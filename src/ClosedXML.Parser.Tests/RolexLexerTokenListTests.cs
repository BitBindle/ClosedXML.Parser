using ClosedXML.Parser.Rolex;

namespace ClosedXML.Parser.Tests;

public class RolexLexerTokenListTests
{
    [Fact]
    [Trait("Issue", "21")]
    public void Empty_formula_has_only_end_token()
    {
        var tokens = RolexLexer.GetTokensA1(string.Empty);

        var token = Assert.Single(tokens);
        Assert.Equal(Token.EofSymbolId, token.SymbolId);
        Assert.Equal(0, token.StartIndex);
    }

    [Theory]
    [Trait("Issue", "21")]
    [InlineData(1)]
    [InlineData(200)]
    [InlineData(5000)]
    public void Lexer_gives_all_tokens_when_formula_has_more_tokens_than_initial_capacity(int operandCount)
    {
        // The formula 1+1+...+1 has more tokens than the half of its length. One operand is the shortest case.
        var formula = string.Join("+", Enumerable.Repeat("1", operandCount));

        var tokens = RolexLexer.GetTokensA1(formula);

        Assert.Equal(2 * operandCount, tokens.Count);
        for (var i = 0; i < tokens.Count - 1; ++i)
        {
            Assert.Equal(i, tokens[i].StartIndex);
            Assert.Equal(i % 2 == 0 ? Token.NUMERICAL_CONSTANT : Token.PLUS, tokens[i].SymbolId);
        }

        Assert.Equal(Token.EofSymbolId, tokens[^1].SymbolId);
        Assert.Equal(formula.Length, tokens[^1].StartIndex);
    }

    [Theory]
    [Trait("Issue", "22")]
    [InlineData("", 1, Token.EofSymbolId)]
    [InlineData("1+2", 4, Token.EofSymbolId)]
    [InlineData("1+2+3", 6, Token.EofSymbolId)]
    [InlineData("1+2+3+", 6, Token.PLUS)]
    [InlineData("1+2+3+4", 6, Token.PLUS)]
    [InlineData("1+\"", 3, Token.ErrorSymbolId)]
    public void Lexer_stops_when_token_buffer_is_full(string formula, int expectedCount, int expectedLastSymbolId)
    {
        Span<Token> buffer = stackalloc Token[6];

        var count = RolexLexer.GetTokensA1(formula, buffer);

        // The tokens in the buffer are the same as the first tokens of the full list.
        Assert.Equal(expectedCount, count);
        Assert.Equal(expectedLastSymbolId, buffer[count - 1].SymbolId);
        var allTokens = RolexLexer.GetTokensA1(formula);
        for (var i = 0; i < count; ++i)
        {
            Assert.Equal(allTokens[i].SymbolId, buffer[i].SymbolId);
            Assert.Equal(allTokens[i].StartIndex, buffer[i].StartIndex);
            Assert.Equal(allTokens[i].Length, buffer[i].Length);
        }
    }
}
