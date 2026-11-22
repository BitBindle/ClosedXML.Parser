using System.Collections.Generic;
using System;

namespace ClosedXML.Parser.Rolex;

internal class RolexLexer
{
    /// <summary>
    /// A value for an unpaired surrogate. No DFA transition accepts a negative value, thus the
    /// lexer gives an error token. The DFA tables accept the surrogate range U+D800..U+DFFF as a
    /// part of a name, so the lexer must not give the surrogate itself to the DFA.
    /// </summary>
    private const int UnpairedSurrogate = -1;

    private const int MaxInitialCapacity = 256;

    /// <summary>
    /// Get all tokens for a formula. Use A1 semantic. If there is an error, add token with an error symbol at the end or EOF token at the end.
    /// </summary>
    /// <param name="formula">Formula to parse.</param>
    public static List<Token> GetTokensA1(ReadOnlySpan<char> formula)
    {
        return GetTokens(formula, RolexA1Dfa.DfaTable);
    }

    /// <summary>
    /// Get all tokens for a formula. Use R1C1 semantic. If there is an error, add token with an error symbol at the end or EOF token at the end.
    /// </summary>
    /// <param name="formula">Formula to parse.</param>
    public static List<Token> GetTokensR1C1(ReadOnlySpan<char> formula)
    {
        return GetTokens(formula, RolexR1C1Dfa.DfaTable);
    }

    /// <summary>
    /// Get the first tokens of a formula into <paramref name="tokens"/>. Use A1 semantic. The
    /// lexer stops when the buffer is full. If the buffer becomes full before the end of the
    /// formula, the last token is not EOF.
    /// </summary>
    /// <param name="formula">Formula to parse.</param>
    /// <param name="tokens">The buffer for the tokens.</param>
    /// <returns>The number of tokens in the buffer.</returns>
    public static int GetTokensA1(ReadOnlySpan<char> formula, Span<Token> tokens)
    {
        var count = 0;
        for (var i = 0; i < formula.Length;)
        {
            if (count == tokens.Length)
                return count;

            var (symbolId, length) = GetToken(formula, i, RolexA1Dfa.DfaTable);
            tokens[count++] = new Token(symbolId, i, length);
            if (symbolId < 0)
                return count;

            i += length;
        }

        if (count == tokens.Length)
            return count;

        tokens[count++] = Token.EofSymbol(formula.Length);
        return count;
    }

    private static List<Token> GetTokens(ReadOnlySpan<char> formula, DfaEntry[] lexerDfa)
    {
        var tokens = new List<Token>(GetInitialCapacity(formula.Length));
        for (var i = 0; i < formula.Length;)
        {
            var (symbolId, length) = GetToken(formula, i, lexerDfa);
            tokens.Add(new Token(symbolId, i, length));
            if (symbolId < 0)
            {
                return tokens;
            }

            i += length;
        }

        tokens.Add(Token.EofSymbol(formula.Length));
        return tokens;
    }

    /// <summary>
    /// Estimate the number of tokens, so that the list does not grow for most formulas. In the
    /// Enron and EUSES formulas, a token has 2.9 chars on average and the half of the formula
    /// length is sufficient for 92 % of formulas. The cap limits the memory for long formulas.
    /// </summary>
    private static int GetInitialCapacity(int formulaLength)
    {
        return Math.Min(formulaLength / 2 + 1, MaxInitialCapacity);
    }

    /// <summary>
    /// Read the code point at <paramref name="index"/> and move the index after it.
    /// </summary>
    /// <returns>The code point, or <see cref="UnpairedSurrogate"/> for a surrogate that is not part of a pair.</returns>
    private static int Next(ReadOnlySpan<char> input, ref int index)
    {
        var c = input[index++];
        if (char.IsHighSurrogate(c))
        {
            if (index < input.Length && char.IsLowSurrogate(input[index]))
                return char.ConvertToUtf32(c, input[index++]);

            return UnpairedSurrogate;
        }

        if (char.IsLowSurrogate(c))
            return UnpairedSurrogate;

        return c;
    }

    private static (int SymbolId, int Length) GetToken(ReadOnlySpan<char> input, int startIdx, DfaEntry[] dfaTable)
    {
        int dfaState = 0;

        // Best token and its length found so far
        var symbolId = Token.ErrorSymbolId;
        var symbolEndIndex = 0;

        for (var idx = startIdx; idx < input.Length;)
        {
            var ch = Next(input, ref idx);

            // We are at some state and are looking for another state
            // That is indicated by a `found` flag.
            int nextDfaState = -1;
            for (var i = 0; i < dfaTable[dfaState].Transitions.Length; ++i)
            {
                DfaTransitionEntry entry = dfaTable[dfaState].Transitions[i];
                bool found = false;
                for (var j = 0; j < entry.PackedRanges.Length; ++j)
                {
                    int first = entry.PackedRanges[j];
                    j++;
                    int last = entry.PackedRanges[j];
                    if (ch <= last)
                    {
                        if (first <= ch)
                        {
                            found = true;
                        }
                        j = int.MaxValue - 1; // Equivalent of a break.
                    }
                }
                if (found)
                {
                    nextDfaState = entry.Destination;
                    i = int.MaxValue - 1; // Equivalent of a break.
                }
            }

            // No valid transition was found from dfaState
            if (nextDfaState == -1)
            {
                // Return best symbolId. If nothing was yet found, it returns -2 as an error.
                if (symbolId < 0)
                    return (Token.ErrorSymbolId, 0);

                // Adjust symbol because ANTLR and Rolex are 1 off
                return (symbolId + 1, symbolEndIndex - startIdx);
            }

            // There is a valid transition.
            dfaState = nextDfaState;
            var currentSymbolId = dfaTable[dfaState].AcceptSymbolId;

            // The substring from startIdx to the current idx are a valid token
            if (currentSymbolId != -1)
            {
                symbolId = currentSymbolId;
                symbolEndIndex = idx;
            }
        }

        // We are at the end of the input. If there is a valid symbol, return it.
        if (symbolId >= 0)
        {
            // Adjust symbol because ANTLR and Rolex are 1 off
            return (symbolId + 1, symbolEndIndex - startIdx);
        }

        // Otherwise, end of stream, but the only way we could got here is if there has been a half-inserted
        // symbol, e.g. `"Text ` without the ending. This method is not called when lexer is at the end of
        // a stream, thus it has to be an error.
        return (Token.ErrorSymbolId, 0);
    }
}