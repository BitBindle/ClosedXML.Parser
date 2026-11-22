using ClosedXML.Parser.Rolex;
using System;

namespace ClosedXML.Parser;

/// <summary>
/// A utility class that parses various types of references.
/// </summary>
/// <remarks>
/// Each method has an overload for <see cref="ReadOnlySpan{T}"/>. The output spans of these
/// overloads are slices of the input text, or a new string when the sheet name in the text is
/// escaped (e.g. <c>'Jane''s'!A1</c>). Thus they do not allocate for most texts.
/// </remarks>
public static class ReferenceParser
{
    /// <summary>
    /// The maximum number of tokens that the parser examines. The longest accepted text is
    /// <c>Sheet!A1:B2</c> with 5 tokens (sheet prefix, cell, colon, cell and EOF). The lexer
    /// reads one more token, so that a longer text does not match.
    /// </summary>
    private const int MaxTokens = 6;

    /// <summary>
    /// <para>
    /// Try to parse <paramref name="text"/> as a sheet reference (<c>Sheet!A5</c>) or a local
    /// reference (<c>A1</c>). If the <paramref name="text"/> is a local reference, the output
    /// value of the <paramref name="sheetName"/> is <c>null</c>.
    /// </para>
    /// <para>
    /// Unlike the <see cref="TryParseA1(string,out ReferenceArea)"/> or <see cref="TryParseSheetA1(string, out string, out ReferenceArea)"/>,
    /// this method can parse both sheet reference or local reference.
    /// </para>
    /// </summary>
    /// <param name="text">Text to parse.</param>
    /// <param name="sheetName">The unescaped name of a sheet for sheet reference, <c>null</c> for local reference.</param>
    /// <param name="area">The parsed reference area.</param>
    /// <returns><c>true</c> if parsing was a success, <c>false</c> otherwise.</returns>
    public static bool TryParseA1(string text, out string? sheetName, out ReferenceArea area)
    {
        if (text is null)
            throw new ArgumentNullException();

        if (!TryParseA1(text.AsSpan(), out ReadOnlySpan<char> sheetNameSpan, out area))
        {
            sheetName = string.Empty;
            return false;
        }

        sheetName = sheetNameSpan.IsEmpty ? null : sheetNameSpan.ToString();
        return true;
    }

    /// <summary>
    /// Try to parse <paramref name="text"/> as a sheet reference (<c>Sheet!A5</c>) or a local
    /// reference (<c>A1</c>). If the <paramref name="text"/> is a local reference, the
    /// <paramref name="sheetName"/> is empty.
    /// </summary>
    /// <param name="text">Text to parse.</param>
    /// <param name="sheetName">The unescaped name of a sheet for sheet reference, empty for local reference.</param>
    /// <param name="area">The parsed reference area.</param>
    /// <returns><c>true</c> if parsing was a success, <c>false</c> otherwise.</returns>
    public static bool TryParseA1(ReadOnlySpan<char> text, out ReadOnlySpan<char> sheetName, out ReferenceArea area)
    {
        Span<Token> buffer = stackalloc Token[MaxTokens];
        var tokens = buffer[..RolexLexer.GetTokensA1(text, buffer)];
        sheetName = ReadOnlySpan<char>.Empty;
        if (TryParseA1(tokens, text, out area))
            return true;

        return TryParseSheetA1(tokens, text, out sheetName, out area);
    }

    /// <summary>
    /// Parses area reference in A1 form. The possibilities are
    /// <list type="bullet">
    ///   <item>Cell (e.g. <c>F8</c>).</item>
    ///   <item>Area (e.g. <c>B2:$D7</c>).</item>
    ///   <item>Colspan (e.g. <c>$D:$G</c>).</item>
    ///   <item>Rowspan (e.g. <c>14:$15</c>).</item>
    /// </list>
    /// Doesn't allow any whitespaces or extra values inside.
    /// </summary>
    /// <param name="text">Text to parse.</param>
    /// <param name="area">Parsed area.</param>
    /// <returns><c>true</c> if parsing was a success, <c>false</c> otherwise.</returns>
    public static bool TryParseA1(string text, out ReferenceArea area)
    {
        if (text is null)
            throw new ArgumentNullException();

        return TryParseA1(text.AsSpan(), out area);
    }

    /// <inheritdoc cref="TryParseA1(string, out ReferenceArea)"/>
    public static bool TryParseA1(ReadOnlySpan<char> text, out ReferenceArea area)
    {
        Span<Token> buffer = stackalloc Token[MaxTokens];
        var tokens = buffer[..RolexLexer.GetTokensA1(text, buffer)];
        return TryParseA1(tokens, text, out area);
    }

    /// <summary>
    /// Parses area reference in A1 form. The possibilities are
    /// <list type="bullet">
    ///   <item>Cell (e.g. <c>F8</c>).</item>
    ///   <item>Area (e.g. <c>B2:$D7</c>).</item>
    ///   <item>Colspan (e.g. <c>$D:$G</c>).</item>
    ///   <item>Rowspan (e.g. <c>14:$15</c>).</item>
    /// </list>
    /// Doesn't allow any whitespaces or extra values inside.
    /// </summary>
    /// <exception cref="ParsingException">Invalid input.</exception>
    public static ReferenceArea ParseA1(string text)
    {
        if (!TryParseA1(text, out var area))
            throw new ParsingException($"Unable to parse '{text}'.");

        return area;
    }

    /// <inheritdoc cref="ParseA1(string)"/>
    public static ReferenceArea ParseA1(ReadOnlySpan<char> text)
    {
        if (!TryParseA1(text, out var area))
            throw new ParsingException($"Unable to parse '{text}'.");

        return area;
    }

    /// <summary>
    /// Try to parse a A1 reference that has a sheet (e.g. <c>'Data values'!A$1:F10</c>).
    /// If <paramref name="text"/> contains only reference without a sheet or anything
    /// else (e.g. <c>A1</c>), return <c>false</c>.
    /// </summary>
    /// <remarks>
    /// The method doesn't accept
    /// <list type="bullet">
    ///   <item>Sheet names, e.g. <c>Sheet!name</c>.</item>
    ///   <item>External sheet references, e.g. <c>[1]Sheet!A1</c>.</item>
    ///   <item>Sheet errors, e.g. <c>Sheet5!$REF!</c>.</item>
    /// </list>
    /// </remarks>
    /// <param name="text">Text to parse.</param>
    /// <param name="sheetName">Name of the sheet, unescaped (e.g. the sheetName will contain <c>Jane's</c> for <c>'Jane''s'!A1</c>).</param>
    /// <param name="area">Parsed reference.</param>
    /// <returns><c>true</c> if parsing was a success, <c>false</c> otherwise.</returns>
    public static bool TryParseSheetA1(string text, out string sheetName, out ReferenceArea area)
    {
        if (text is null)
            throw new ArgumentNullException(nameof(text));

        var success = TryParseSheetA1(text.AsSpan(), out ReadOnlySpan<char> sheetNameSpan, out area);
        sheetName = success ? sheetNameSpan.ToString() : string.Empty;
        return success;
    }

    /// <inheritdoc cref="TryParseSheetA1(string, out string, out ReferenceArea)"/>
    public static bool TryParseSheetA1(ReadOnlySpan<char> text, out ReadOnlySpan<char> sheetName, out ReferenceArea area)
    {
        Span<Token> buffer = stackalloc Token[MaxTokens];
        var tokens = buffer[..RolexLexer.GetTokensA1(text, buffer)];
        return TryParseSheetA1(tokens, text, out sheetName, out area);
    }

    /// <summary>
    /// <para>
    /// Try to parse <paramref name="text"/> as a name (e.g. <c>Name</c>) or a sheet name
    /// (<c>Sheet!Name</c>). If the <paramref name="text"/> is only a name, the output value of the
    /// <paramref name="sheetName"/> is <c>null</c>.
    /// </para>
    /// </summary>
    /// <param name="text">Text to parse.</param>
    /// <param name="sheetName">The unescaped name of a sheet for sheet name, <c>null</c> for a name.</param>
    /// <param name="name">The parsed name.</param>
    /// <returns><c>true</c> if parsing was a success, <c>false</c> otherwise.</returns>
    public static bool TryParseName(string text, out string? sheetName, out string name)
    {
        if (text is null)
            throw new ArgumentNullException(nameof(text));

        if (!TryParseName(text.AsSpan(), out ReadOnlySpan<char> sheetNameSpan, out var nameSpan))
        {
            sheetName = string.Empty;
            name = string.Empty;
            return false;
        }

        if (sheetNameSpan.IsEmpty)
        {
            // The whole text is the name.
            sheetName = null;
            name = text;
            return true;
        }

        sheetName = sheetNameSpan.ToString();
        name = nameSpan.ToString();
        return true;
    }

    /// <summary>
    /// Try to parse <paramref name="text"/> as a name (e.g. <c>Name</c>) or a sheet name
    /// (<c>Sheet!Name</c>). If the <paramref name="text"/> is only a name, the
    /// <paramref name="sheetName"/> is empty.
    /// </summary>
    /// <param name="text">Text to parse.</param>
    /// <param name="sheetName">The unescaped name of a sheet for sheet name, empty for a name.</param>
    /// <param name="name">The parsed name.</param>
    /// <returns><c>true</c> if parsing was a success, <c>false</c> otherwise.</returns>
    public static bool TryParseName(ReadOnlySpan<char> text, out ReadOnlySpan<char> sheetName, out ReadOnlySpan<char> name)
    {
        Span<Token> buffer = stackalloc Token[MaxTokens];
        var tokens = buffer[..RolexLexer.GetTokensA1(text, buffer)];
        if (tokens.Length == 2 &&
            tokens[0].SymbolId == Token.NAME &&
            tokens[1].SymbolId == Token.EofSymbolId)
        {
            sheetName = ReadOnlySpan<char>.Empty;
            name = text;
            return true;
        }

        return TryParseSheetName(tokens, text, out sheetName, out name);
    }

    /// <summary>
    /// Try to parse a text as a sheet name (e.g. <c>Sheet!Name</c>). Doesn't accept pure name
    /// without sheet (e.g. <c>name</c>).
    /// </summary>
    /// <param name="text">Text to parse.</param>
    /// <param name="sheetName">Parsed sheet name, unescaped.</param>
    /// <param name="name">Parsed defined name.</param>
    /// <returns><c>true</c> if parsing was a success, <c>false</c> otherwise.</returns>
    public static bool TryParseSheetName(string text, out string sheetName, out string name)
    {
        if (text is null)
            throw new ArgumentNullException(nameof(text));

        if (!TryParseSheetName(text.AsSpan(), out ReadOnlySpan<char> sheetNameSpan, out var nameSpan))
        {
            sheetName = string.Empty;
            name = string.Empty;
            return false;
        }

        sheetName = sheetNameSpan.ToString();
        name = nameSpan.ToString();
        return true;
    }

    /// <inheritdoc cref="TryParseSheetName(string, out string, out string)"/>
    public static bool TryParseSheetName(ReadOnlySpan<char> text, out ReadOnlySpan<char> sheetName, out ReadOnlySpan<char> name)
    {
        Span<Token> buffer = stackalloc Token[MaxTokens];
        var tokens = buffer[..RolexLexer.GetTokensA1(text, buffer)];
        return TryParseSheetName(tokens, text, out sheetName, out name);
    }

    private static bool TryParseA1(ReadOnlySpan<Token> tokens, ReadOnlySpan<char> text, out ReferenceArea area)
    {
        var isValid = IsA1Reference(tokens);
        if (!isValid)
        {
            area = default;
            return false;
        }

        area = TokenParser.ParseReference(text, isA1: true);
        return true;
    }

    private static bool TryParseSheetA1(scoped ReadOnlySpan<Token> tokens, ReadOnlySpan<char> text, out ReadOnlySpan<char> sheetName, out ReferenceArea area)
    {
        if (tokens.Length == 0 ||
            tokens[0].SymbolId != Token.SINGLE_SHEET_PREFIX ||
            !IsA1Reference(tokens[1..]))
        {
            sheetName = ReadOnlySpan<char>.Empty;
            area = default;
            return false;
        }

        var sheetPrefixToken = tokens[0];
        var sheetPrefix = text.Slice(sheetPrefixToken.StartIndex, sheetPrefixToken.Length);
        TokenParser.ParseSingleSheetPrefix(sheetPrefix, out int? workbookIndex, out sheetName);
        if (workbookIndex is not null)
        {
            sheetName = ReadOnlySpan<char>.Empty;
            area = default;
            return false;
        }

        var referenceArea = text.Slice(sheetPrefixToken.Length);
        area = TokenParser.ParseReference(referenceArea, isA1: true);
        return true;
    }

    private static bool IsA1Reference(ReadOnlySpan<Token> tokens)
    {
        // a1_reference : A1_CELL
        //              | A1_CELL COLON A1_CELL
        //              | A1_SPAN_REFERENCE
        var isValid = tokens.Length switch
        {
            2 => tokens[0].SymbolId is Token.A1_CELL or Token.A1_SPAN_REFERENCE &&
                 tokens[1].SymbolId == Token.EofSymbolId,
            4 => tokens[0].SymbolId == Token.A1_CELL &&
                 tokens[1].SymbolId == Token.COLON &&
                 tokens[2].SymbolId == Token.A1_CELL &&
                 tokens[3].SymbolId == Token.EofSymbolId,
            _ => false,
        };
        return isValid;
    }

    private static bool TryParseSheetName(scoped ReadOnlySpan<Token> tokens, ReadOnlySpan<char> text, out ReadOnlySpan<char> sheetName, out ReadOnlySpan<char> name)
    {
        var isValid = tokens.Length switch
        {
            3 => tokens[0].SymbolId == Token.SINGLE_SHEET_PREFIX &&
                 tokens[1].SymbolId == Token.NAME &&
                 tokens[2].SymbolId == Token.EofSymbolId,
            _ => false,
        };
        if (!isValid)
        {
            sheetName = ReadOnlySpan<char>.Empty;
            name = ReadOnlySpan<char>.Empty;
            return false;
        }

        var sheetPrefixToken = tokens[0];
        var sheetPrefix = text.Slice(sheetPrefixToken.StartIndex, sheetPrefixToken.Length);
        TokenParser.ParseSingleSheetPrefix(sheetPrefix, out int? workbookIndex, out sheetName);
        if (workbookIndex is not null)
        {
            sheetName = ReadOnlySpan<char>.Empty;
            name = ReadOnlySpan<char>.Empty;
            return false;
        }

        var nameToken = tokens[1];
        name = text.Slice(nameToken.StartIndex, nameToken.Length);
        return true;
    }
}
