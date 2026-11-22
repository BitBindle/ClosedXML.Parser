using ClosedXML.Parser.Pratt;

namespace ClosedXML.Parser.Tests;

/// <summary>
/// The main parser and the Pratt parser must accept the same chars in names and in unquoted
/// sheet names. The rules are the <c>NAME</c> and <c>SINGLE_SHEET_PREFIX</c> tokens of the
/// main lexer.
/// </summary>
public class IdentCharacterTests
{
    [Theory]
    [Trait("Issue", "16")]
    [InlineData("name0")]
    [InlineData("n.a.m.e")]
    [InlineData("name?")]
    [InlineData("na?me")]
    [InlineData("\\name")]
    [InlineData("name\\")]
    [InlineData("name_")]
    [InlineData("_xlfn.ACOT")]
    [InlineData("\u00A0name")] // NO-BREAK SPACE
    [InlineData("na\u00A0me")]
    [InlineData("na\u2028me")] // LINE SEPARATOR
    [InlineData("\u0080name")] // A control char
    [InlineData("\u20AC")] // EURO SIGN
    [InlineData("\u00BDname")] // VULGAR FRACTION ONE HALF
    [InlineData("\u0660")] // ARABIC-INDIC DIGIT ZERO
    [InlineData("\u0301name")] // COMBINING ACUTE ACCENT
    [InlineData("\uE000")] // A private use char
    [InlineData("\U0001F600")] // GRINNING FACE
    [InlineData("name\U0001F600")]
    [InlineData("A0")] // A text that reads almost as a cell
    [InlineData("A01")]
    [InlineData("XFE1")]
    [InlineData("A1048577")]
    public void Both_parsers_read_name(string formula)
    {
        AssertSameNode<NameNode>(formula);
    }

    [Theory]
    [Trait("Issue", "16")]
    [InlineData("1x!A1")]
    [InlineData("1!A1")]
    [InlineData("1.5!A1")]
    [InlineData("$x!A1")]
    [InlineData(".x!A1")]
    [InlineData("\"x\"!A1")]
    [InlineData("x\"!A1")]
    [InlineData("a;b!A1")]
    [InlineData("a@b!A1")]
    [InlineData("a|b!A1")]
    [InlineData("a~b!A1")]
    [InlineData("a`b!A1")]
    [InlineData("a_b!A1")]
    [InlineData("a\u007Fb!A1")] // DELETE
    [InlineData("a\tb!A1")]
    [InlineData("a\nb!A1")]
    [InlineData("a\rb!A1")]
    [InlineData("\t!A1")]
    [InlineData("\u00A0!A1")]
    [InlineData("\u20AC!A1")]
    [InlineData("\uFFFD!A1")] // The last valid char in the basic multilingual plane
    [InlineData("\U0001F600!A1")]
    [InlineData("TRUE!A1")]
    [InlineData("Sheet! A1")] // The sheet prefix of the main lexer contains the spaces after the `!`
    [InlineData("\nSheet!A1")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa!A1")] // The main parser does not check the length
    public void Both_parsers_read_unquoted_sheet(string formula)
    {
        AssertSameNode<SheetReferenceNode>(formula);
    }

    [Theory]
    [Trait("Issue", "16")]
    [InlineData("1+\nSheet!A1")] // The main lexer adds the LF to the operator, not to the sheet name
    [InlineData("1+\rSheet!A1")]
    [InlineData("1- \r\nSheet!A1")]
    [InlineData("1*\n\n1x!A1")]
    [InlineData("1/\nSheet!A1")]
    [InlineData("2^\nSheet!A1")]
    [InlineData("(\nSheet!A1)")]
    [InlineData("1+\tSheet!A1")] // The main lexer does not add a tab to the operator
    public void Both_parsers_read_sheet_after_operator(string formula)
    {
        var mainNode = ParseMain(formula);
        var prattNode = ParsePratt(formula);

        Assert.Equal(mainNode, prattNode);
    }

    [Theory]
    [Trait("Issue", "16")]
    [InlineData("a;b!x")]
    [InlineData("x!a.b")]
    [InlineData("x!\u00A0a")]
    [InlineData("x!\U0001F600")]
    public void Both_parsers_read_sheet_name(string formula)
    {
        AssertSameNode<SheetNameNode>(formula);
    }

    [Theory]
    [Trait("Issue", "16")]
    [InlineData("a:c;d!A1")] // The last sheet is a sheet prefix
    [InlineData("a:1!A1")]
    [InlineData("a.b:c$d!A1")]
    [InlineData("_a:b!A1")]
    [InlineData("\U0001F600:b!A1")]
    [InlineData("a:\U0001F600!A1")]
    public void Both_parsers_read_3d_reference(string formula)
    {
        AssertSameNode<Reference3DNode>(formula);
    }

    [Theory]
    [Trait("Issue", "16")]
    [InlineData("\"a!b\"")] // The text is longer than the sheet prefix `"a!`
    [InlineData("\"a\"\"!\"")]
    [InlineData("\"a!")] // Not terminated
    public void Lexer_selects_longest_token_for_text_with_bang(string formula)
    {
        // The Pratt parser can not parse a text yet, thus the test checks the lexer.
        var lexer = new Lexer(formula);

        var token = lexer.Consume();

        if (formula.EndsWith('"'))
        {
            Assert.Equal(TokenType.Text, token.Type);
            Assert.Equal(formula, token.GetText(formula).ToString());
        }
        else
        {
            Assert.Equal(TokenType.Ident, token.Type);
            Assert.Equal("\"a", token.GetText(formula).ToString());
        }
    }

    [Fact]
    [Trait("Issue", "16")]
    public void Lexer_reads_tokens_in_sheet_run_without_bang()
    {
        // A run of sheet name chars without `!` gives the usual tokens: ident, semicolon, ident.
        const string formula = "a;b;c";
        var lexer = new Lexer(formula);

        Assert.Equal((TokenType.Ident, "a"), Read(lexer.Consume()));
        Assert.Equal((TokenType.Semicolon, ";"), Read(lexer.Consume()));
        Assert.Equal((TokenType.Ident, "b"), Read(lexer.Consume()));
        Assert.Equal((TokenType.Semicolon, ";"), Read(lexer.Consume()));
        Assert.Equal((TokenType.Ident, "c"), Read(lexer.Consume()));
        Assert.Equal(TokenType.Eof, lexer.Consume().Type);

        (TokenType, string) Read(Pratt.Token token) => (token.Type, token.GetText(formula).ToString());
    }

    [Theory]
    [Trait("Issue", "16")]
    [InlineData("a\\b!A1")] // An unquoted sheet name can not contain a backslash
    [InlineData("a?b!A1")]
    [InlineData("\uFFFE!A1")] // A noncharacter
    [InlineData("a;b:c!A1")] // The first sheet of a 3D reference must be a name
    [InlineData("1a:b!A1")]
    [InlineData("a$b:c!A1")]
    [InlineData("0name")]
    [InlineData("name(")]
    [InlineData("name)")]
    [InlineData("name,")]
    [InlineData("?name")]
    [InlineData(".name")]
    public void Both_parsers_reject_formula(string formula)
    {
        Assert.Throws<ParsingException>(() => ParseMain(formula));
        Assert.Throws<ParsingException>(() => ParsePratt(formula));
    }

    [Theory]
    [Trait("Issue", "16")]
    [InlineData("\\a:b!A1")]
    [InlineData("a?:b!A1")]
    public void Main_parser_reads_name_that_is_not_a_sheet_as_operand_of_range(string formula)
    {
        // A sheet name can not contain these chars, thus the first operand is a name.
        var node = ParseMain(formula);

        var range = Assert.IsType<BinaryNode>(node);
        Assert.Equal(BinaryOperation.Range, range.Operation);
        Assert.IsType<NameNode>(range.Children[0]);
        Assert.IsType<SheetReferenceNode>(range.Children[1]);
    }

    [Theory]
    [Trait("Issue", "16")]
    [InlineData("name ")]
    [InlineData("name \r\n")]
    [InlineData("name\u00A0")]
    [InlineData("name\u2028")]
    public void Both_parsers_trim_whitespace_at_end(string formula)
    {
        AssertSameNode<NameNode>(formula);
        Assert.Equal(new NameNode("name"), ParsePratt(formula));
    }

    [Fact]
    [Trait("Issue", "16")]
    public void Pratt_parser_can_parse_after_failed_formula()
    {
        // The failed formula leaves a token in the queue of the lexer. The next formula must not use it.
        var parser = ParserFactory.Create(new F());
        Assert.Throws<ParsingException>(() => parser.ParseFormula("x)", new Ctx()));

        Assert.Equal(new NameNode("name"), parser.ParseFormula("name", new Ctx()));
    }

    [Theory]
    [Trait("Issue", "16")]
    [InlineData("(", "Unexpected token")]
    [InlineData("1+", "Unexpected token")]
    [InlineData("x)", "was not parsed correctly")]
    [InlineData("(1", "Expected token of type RightParen")]
    public void Pratt_parser_throws_parsing_exception_for_invalid_formula(string formula, string expectedMessage)
    {
        var parser = ParserFactory.Create(new F());

        var ex = Assert.Throws<ParsingException>(() => parser.ParseFormula(formula, new Ctx()));
        Assert.Contains(expectedMessage, ex.Message);
    }

    [Fact]
    [Trait("Issue", "16")]
    public void Unquoted_sheet_name_from_writer_is_valid_for_parsers()
    {
        // The writer can quote more than the parsers need, but each sheet name that the
        // writer does not quote must be valid for the parsers.
        for (var codepoint = 1; codepoint <= 0x10FFFF; ++codepoint)
        {
            if (codepoint is >= 0xD800 and <= 0xDFFF)
                continue;

            // An XML file can not contain a control char, except tab, LF and CR. A quoted sheet
            // name can not contain it either.
            if (codepoint < 0x20 && codepoint is not '\t' and not '\n' and not '\r')
                continue;

            var c = char.ConvertFromUtf32(codepoint);
            foreach (var sheet in new[] { c, "a" + c, c + "a" })
            {
                // Excel does not accept a sheet name that starts with an apostrophe.
                if (sheet[0] == '\'')
                    continue;

                if (NameUtils.IsSheetNameValid(sheet) && !NameUtils.ShouldQuote(sheet))
                    Assert.True(NameUtils.IsUnquotedSheetName(sheet), $"Sheet with U+{codepoint:X4} is not valid without quotes.");
            }
        }
    }

    [Fact]
    [Trait("Issue", "16")]
    public void IsNameValid_checks_name()
    {
        // xUnit changes an unpaired surrogate in InlineData, thus the test has the values in code.
        Assert.False(NameUtils.IsNameValid(""));
        Assert.True(NameUtils.IsNameValid("a"));
        Assert.False(NameUtils.IsNameValid("1"));
        Assert.False(NameUtils.IsNameValid("a b"));
        Assert.False(NameUtils.IsNameValid("a\uD800")); // Unpaired high surrogate
        Assert.False(NameUtils.IsNameValid("a\uD800b"));
        Assert.False(NameUtils.IsNameValid("a\uDC00b")); // Unpaired low surrogate
        Assert.True(NameUtils.IsNameValid("a\uD83D\uDE00"));
    }

    [Fact]
    [Trait("Issue", "16")]
    public void IsUnquotedSheetName_checks_sheet_name()
    {
        // xUnit changes an unpaired surrogate in InlineData, thus the test has the values in code.
        Assert.False(NameUtils.IsUnquotedSheetName(""));
        Assert.True(NameUtils.IsUnquotedSheetName("a"));
        Assert.False(NameUtils.IsUnquotedSheetName("a b"));
        Assert.False(NameUtils.IsUnquotedSheetName("a\uD800")); // Unpaired high surrogate
        Assert.False(NameUtils.IsUnquotedSheetName("a\uD800b"));
        Assert.False(NameUtils.IsUnquotedSheetName("\uDC00")); // Unpaired low surrogate
        Assert.True(NameUtils.IsUnquotedSheetName("\uD83D\uDE00"));
    }

    [Fact]
    [Trait("Issue", "16")]
    public void Lexer_ends_sheet_run_at_unpaired_surrogate()
    {
        // The run ends at the surrogate, thus there is no `!` after it. The lexer then reads an
        // ident and throws at the surrogate.
        var lexer = new Lexer("a\uD800!A1");

        Assert.Throws<ParsingException>(() => lexer.Consume());
    }

    private static void AssertSameNode<TNode>(string formula)
        where TNode : AstNode
    {
        var mainNode = ParseMain(formula);
        var prattNode = ParsePratt(formula);

        Assert.IsType<TNode>(mainNode);
        Assert.Equal(mainNode, prattNode);
    }

    private static AstNode ParseMain(string formula)
    {
        return FormulaParser<ScalarValue, AstNode, Ctx>.CellFormulaA1(formula, new Ctx(), new F());
    }

    private static AstNode ParsePratt(string formula)
    {
        return ParserFactory.Create(new F()).ParseFormula(formula, new Ctx());
    }
}
