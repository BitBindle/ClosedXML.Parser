using ClosedXML.Parser.Rolex;

namespace ClosedXML.Parser.Tests;

public class UnpairedSurrogateTests
{
    // The test data has the surrogate as a number, because xunit cannot serialize a string
    // with an unpaired surrogate.
    public static TheoryData<string, int, string> UnpairedSurrogates => new()
    {
        { "", 0xD800, "" }, // High surrogate is the only char
        { "A1+", 0xD800, "" }, // High surrogate at the end of the formula
        { "", 0xDBFF, "A" }, // High surrogate before a char that is not a low surrogate
        { "", 0xDC00, "" }, // Low surrogate with no high surrogate
        { "name", 0xDFFF, "" }, // Low surrogate in a name
        { "\"text", 0xD800, "\"" }, // High surrogate in a text literal
        { "'Sheet", 0xDC00, "'!A1" }, // Low surrogate in a quoted sheet name
    };

    [Theory]
    [Trait("Issue", "19")]
    [MemberData(nameof(UnpairedSurrogates))]
    public void Rolex_lexer_gives_error_token_for_unpaired_surrogate(string prefix, int surrogate, string suffix)
    {
        var formula = prefix + (char)surrogate + suffix;

        Assert.Equal(Token.ErrorSymbolId, RolexLexer.GetTokensA1(formula).Last().SymbolId);
        Assert.Equal(Token.ErrorSymbolId, RolexLexer.GetTokensR1C1(formula).Last().SymbolId);
    }

    [Theory]
    [Trait("Issue", "19")]
    [MemberData(nameof(UnpairedSurrogates))]
    public void Parsers_throw_parsing_exception_for_unpaired_surrogate(string prefix, int surrogate, string suffix)
    {
        var formula = prefix + (char)surrogate + suffix;

        Assert.Throws<ParsingException>(() => FormulaParser<ScalarValue, AstNode, Ctx>.CellFormulaA1(formula, new Ctx(), new F()));
        Assert.Throws<ParsingException>(() => FormulaParser<ScalarValue, AstNode, Ctx>.CellFormulaR1C1(formula, new Ctx(), new F()));
    }

    [Theory]
    [Trait("Issue", "19")]
    [MemberData(nameof(UnpairedSurrogates))]
    public void Reference_parser_returns_false_for_unpaired_surrogate(string prefix, int surrogate, string suffix)
    {
        var text = prefix + (char)surrogate + suffix;

        Assert.False(ReferenceParser.TryParseA1(text, out _));
        Assert.False(ReferenceParser.TryParseA1(text, out _, out _));
        Assert.False(ReferenceParser.TryParseSheetA1(text, out _, out _));
        Assert.False(ReferenceParser.TryParseName(text, out _, out _));
        Assert.False(ReferenceParser.TryParseSheetName(text, out _, out _));
    }

    [Fact]
    [Trait("Issue", "19")]
    public void Rolex_lexer_error_token_starts_at_token_that_contains_unpaired_surrogate()
    {
        // The tokens before the surrogate are valid. A text literal with a surrogate is not
        // a token, thus the error starts at the opening quote.
        var afterTokens = RolexLexer.GetTokensA1("A1+" + (char)0xD800);
        var inTextLiteral = RolexLexer.GetTokensA1("1+\"text" + (char)0xD800 + "\"");

        Assert.Equal(new[] { (Token.A1_CELL, 0), (Token.PLUS, 2), (Token.ErrorSymbolId, 3) }, afterTokens.Select(x => (x.SymbolId, x.StartIndex)));
        Assert.Equal(new[] { (Token.NUMERICAL_CONSTANT, 0), (Token.PLUS, 1), (Token.ErrorSymbolId, 2) }, inTextLiteral.Select(x => (x.SymbolId, x.StartIndex)));
    }

    [Fact]
    [Trait("Issue", "19")]
    public void Rolex_lexer_reads_surrogate_pair_as_one_code_point()
    {
        // MATHEMATICAL BOLD CAPITAL A, U+1D400
        var tokens = RolexLexer.GetTokensA1("\U0001D400name+1");

        Assert.Equal(new[] { Token.NAME, Token.PLUS, Token.NUMERICAL_CONSTANT, Token.EofSymbolId }, tokens.Select(x => x.SymbolId));
        Assert.Equal(new[] { 0, 6, 7, 8 }, tokens.Select(x => x.StartIndex));
    }
}
