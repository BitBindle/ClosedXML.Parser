using System;
using System.Collections.Generic;

namespace ClosedXML.Parser.Pratt;

/// <summary>
/// Pratt parser.
/// </summary>
internal class Parser<T, TContext>
{
    private readonly Lexer _lexer = new();
    private readonly Dictionary<TokenType, IPrefixParselet<T, TContext>> _prefixParselets = new();
    private readonly Dictionary<TokenType, IParselet<T, TContext>> _parselets = new();

    internal string Input { get; private set; } = string.Empty;

    public T ParseFormula(string formula, TContext ctx)
    {
        Input = formula;

        // The main parser trims whitespace at the end, thus the Pratt parser does too.
        var trimmedLength = formula.AsSpan().TrimEnd().Length;
        _lexer.Reset(trimmedLength == formula.Length ? formula : formula[..trimmedLength]);
        var node = ParseExpression(ctx, 0);

        // Each token must be a part of the expression.
        var nextToken = _lexer.Peek();
        if (nextToken.Type != TokenType.Eof)
            throw new ParsingException($"The formula `{formula}` was not parsed correctly. The expression `{formula.AsSpan(node.Range.Start, node.Range.Length)}` was parsed, but the rest `{formula.AsSpan(nextToken.Range.Start, trimmedLength - nextToken.Range.Start)}` was not.");

        return node.Value;
    }

    internal Node<T> ParseExpression(TContext ctx, int minBp)
    {
        var node = Prefix(ctx);

        while (true)
        {
            var maybeOp = _lexer.Peek();
            if (maybeOp.Type == TokenType.Eof)
                break;

            var isOp = _parselets.TryGetValue(maybeOp.Type, out var parselet);
            if (!isOp)
                break;

            var bp = parselet!.GetBindingPower();
            if (bp <= minBp)
                break;

            var op = _lexer.Consume();
            node = parselet.Parse(ctx, node, op);
        }

        return node;
    }

    private Node<T> Prefix(TContext ctx)
    {
        var token = _lexer.Consume();

        if (!_prefixParselets.TryGetValue(token.Type, out var parselet))
            throw new ParsingException($"Unexpected token {token.Type} at position {token.Range.Start}.");

        return parselet.Parse(ctx, token);
    }

    public Token LookAhead(int distance)
    {
        return _lexer.Peek(distance);
    }

    internal Token Consume(TokenType expectedType)
    {
        var token = _lexer.Consume();
        if (token.Type != expectedType)
            throw new InvalidOperationException($"Expected token of type {expectedType}, but received {token.Type}.");

        return token;
    }

    internal Token Consume()
    {
        return _lexer.Consume();
    }

    internal void Register(TokenType type, IPrefixParselet<T, TContext> parselet)
    {
        _prefixParselets.Add(type, parselet);
    }

    internal void Register(TokenType type, IParselet<T, TContext> parselet)
    {
        _parselets.Add(type, parselet);
    }
}
