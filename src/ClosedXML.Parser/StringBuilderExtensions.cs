using System;
using System.Text;

namespace ClosedXML.Parser;

/// <summary>
/// Extension methods for building formulas.
/// </summary>
internal static class StringBuilderExtensions
{
    /// <summary>
    /// Append a sheet name and the reference separator. An empty sheet name is a deleted sheet,
    /// and it is written as <c>#REF!</c>.
    /// </summary>
    public static StringBuilder AppendSheetReference(this StringBuilder sb, ReadOnlySpan<char> sheetName)
    {
        if (sheetName.IsEmpty)
            return sb.Append("#REF!");

        return NameUtils.EscapeName(sb, sheetName).AppendReferenceSeparator();
    }

    public static StringBuilder AppendExternalSheetReference(this StringBuilder sb, int workbookIndex, ReadOnlySpan<char> sheetName)
    {
        if (NameUtils.ShouldQuote(sheetName))
        {
            return sb
                .Append('\'')
                .AppendBookIndex(workbookIndex)
                .AppendEscapedSheetName(sheetName)
                .Append('\'')
                .AppendReferenceSeparator();
        }

        return sb
            .AppendBookIndex(workbookIndex)
            .AppendSheetReference(sheetName);
    }
    public static StringBuilder AppendEscapedSheetName(this StringBuilder sb, ReadOnlySpan<char> sheetName)
    {
        var startIndex = sb.Length;
        return sb.Append(sheetName).Replace("'", "''", startIndex, sheetName.Length);
    }

    public static StringBuilder AppendReferenceSeparator(this StringBuilder sb)
    {
        return sb.Append('!');
    }

    public static StringBuilder AppendBookIndex(this StringBuilder sb, int bookIndex)
    {
        return sb.Append('[').Append(bookIndex).Append(']');
    }

    public static StringBuilder AppendRef(this StringBuilder sb, ReferenceArea? reference)
    {
        return reference is null ? sb.Append("#REF!") : reference.Value.Append(sb);
    }

    /// <summary>
    /// Get the index of the opening brace of a function. The brace is the last <c>(</c> before
    /// <paramref name="nodeStart"/>, the start of the first argument (or the end of the function
    /// without arguments).
    /// </summary>
    internal static int GetStartBraceIndex(ModContext ctx, SymbolRange range, int nodeStart)
    {
        var formula = ctx.Formula;
        var braceIdx = nodeStart - 1;
        for (; braceIdx > range.Start; --braceIdx)
        {
            if (formula[braceIdx] == '(')
                return braceIdx;
        }

        throw new InvalidOperationException("No opening brace found.");
    }
}
