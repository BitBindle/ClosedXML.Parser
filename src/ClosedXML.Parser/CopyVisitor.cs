using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ClosedXML.Parser;

/// <summary>
/// A visitor that generates the identical formula for the parsed formula based on passed arguments.
/// CopyVisitor doesn't make any judgements if passed arguments have been modified. When the text of
/// a node and of all its children is the same as in the original formula, the node is a copy of the
/// original formula text and no new text is allocated. Otherwise, the node gets a new text.
/// </summary>
public class CopyVisitor : IAstFactory<TransformedSymbol, TransformedSymbol, ModContext>
{
    // 1 quote on left, 1 quote on right size and at most 4 quotes inside.
    private const int QUOTE_RESERVE = 6;
    private const int SHEET_SEPARATOR_LEN = 1;
    private const int BOOK_PREFIX_LEN = 3;
    private const int MAX_R1_C1_LEN = 20;

    /// <summary>
    /// The length of a stack buffer for a node text. A longer text uses a heap buffer.
    /// </summary>
    internal const int StackBufferLength = 256;

    /// <inheritdoc />
    public virtual TransformedSymbol LogicalValue(ModContext ctx, SymbolRange range, bool value)
    {
        return TransformedSymbol.CopyOriginal(ctx.Formula, range);
    }

    /// <inheritdoc />
    public virtual TransformedSymbol NumberValue(ModContext ctx, SymbolRange range, double value)
    {
        return TransformedSymbol.CopyOriginal(ctx.Formula, range);
    }

    /// <inheritdoc />
    public virtual TransformedSymbol TextValue(ModContext ctx, SymbolRange range, ReadOnlySpan<char> text)
    {
        return TransformedSymbol.CopyOriginal(ctx.Formula, range);
    }

    /// <inheritdoc />
    public virtual TransformedSymbol ErrorValue(ModContext ctx, SymbolRange range, ReadOnlySpan<char> error)
    {
        return TransformedSymbol.CopyOriginal(ctx.Formula, range);
    }

    /// <inheritdoc />
    public virtual TransformedSymbol ArrayNode(ModContext ctx, SymbolRange range, int rows, int columns, IReadOnlyList<TransformedSymbol> elements)
    {
        // The elements are in the order of the formula text, row by row.
        return BuildNode(ctx, range, range.Start, ReadOnlySpan<char>.Empty, AsSpan(elements));
    }

    /// <inheritdoc />
    public virtual TransformedSymbol BlankNode(ModContext ctx, SymbolRange range)
    {
        return TransformedSymbol.CopyOriginal(ctx.Formula, range);
    }

    /// <inheritdoc />
    public virtual TransformedSymbol LogicalNode(ModContext ctx, SymbolRange range, bool value)
    {
        return TransformedSymbol.CopyOriginal(ctx.Formula, range);
    }

    /// <inheritdoc />
    public virtual TransformedSymbol ErrorNode(ModContext ctx, SymbolRange range, ReadOnlySpan<char> error)
    {
        return TransformedSymbol.CopyOriginal(ctx.Formula, range);
    }

    /// <inheritdoc />
    public virtual TransformedSymbol NumberNode(ModContext ctx, SymbolRange range, double value)
    {
        return TransformedSymbol.CopyOriginal(ctx.Formula, range);
    }

    /// <inheritdoc />
    public virtual TransformedSymbol TextNode(ModContext ctx, SymbolRange range, ReadOnlySpan<char> text)
    {
        return TransformedSymbol.CopyOriginal(ctx.Formula, range);
    }

    /// <inheritdoc />
    public virtual TransformedSymbol Reference(ModContext ctx, SymbolRange range, ReferenceArea reference)
    {
        Span<char> buffer = stackalloc char[ReferenceArea.MaxDisplayLength];
        return ToSymbol(ctx, range, buffer[..reference.Format(buffer)]);
    }

    /// <inheritdoc />
    public virtual TransformedSymbol SheetReference(ModContext ctx, SymbolRange range, ReadOnlySpan<char> sheet, ReferenceArea reference)
    {
        var maxLength = NameUtils.GetMaxSheetReferenceLength(sheet) + ReferenceArea.MaxDisplayLength;
        Span<char> buffer = maxLength <= StackBufferLength ? stackalloc char[StackBufferLength] : new char[maxLength];
        var length = NameUtils.FormatSheetReference(buffer, sheet);
        length += reference.Format(buffer[length..]);
        return ToSymbol(ctx, range, buffer[..length]);
    }

    /// <inheritdoc />
    public virtual TransformedSymbol BangReference(ModContext ctx, SymbolRange range, ReferenceArea reference)
    {
        Span<char> buffer = stackalloc char[SHEET_SEPARATOR_LEN + ReferenceArea.MaxDisplayLength];
        buffer[0] = '!';
        var length = SHEET_SEPARATOR_LEN + reference.Format(buffer[SHEET_SEPARATOR_LEN..]);
        return ToSymbol(ctx, range, buffer[..length]);
    }

    /// <inheritdoc />
    public virtual TransformedSymbol Reference3D(ModContext ctx, SymbolRange range, ReadOnlySpan<char> firstSheet, ReadOnlySpan<char> lastSheet, ReferenceArea reference)
    {
        var sb = new StringBuilder(firstSheet.Length + QUOTE_RESERVE + lastSheet.Length + QUOTE_RESERVE + SHEET_SEPARATOR_LEN + MAX_R1_C1_LEN);
        if (NameUtils.ShouldQuote(firstSheet) || NameUtils.ShouldQuote(lastSheet))
        {
            sb
                .Append('\'')
                .AppendEscapedSheetName(firstSheet)
                .Append(':')
                .AppendEscapedSheetName(lastSheet)
                .Append('\'');
        }
        else
        {
            sb.Append(firstSheet)
                .Append(':')
                .Append(lastSheet);
        }

        var nodeText = sb
            .AppendReferenceSeparator()
            .AppendRef(reference)
            .ToString();
        return ToSymbol(ctx, range, nodeText);
    }

    /// <inheritdoc />
    public virtual TransformedSymbol ExternalSheetReference(ModContext ctx, SymbolRange range, int workbookIndex, ReadOnlySpan<char> sheet, ReferenceArea reference)
    {
        var sb = new StringBuilder(BOOK_PREFIX_LEN + sheet.Length + QUOTE_RESERVE + SHEET_SEPARATOR_LEN + MAX_R1_C1_LEN);
        var nodeText = sb
            .AppendExternalSheetReference(workbookIndex, sheet)
            .AppendRef(reference)
            .ToString();
        return ToSymbol(ctx, range, nodeText);
    }

    /// <inheritdoc />
    public virtual TransformedSymbol ExternalReference3D(ModContext ctx, SymbolRange range, int workbookIndex, ReadOnlySpan<char> firstSheet, ReadOnlySpan<char> lastSheet, ReferenceArea reference)
    {
        var sb = new StringBuilder(BOOK_PREFIX_LEN + firstSheet.Length + QUOTE_RESERVE + lastSheet.Length + QUOTE_RESERVE + SHEET_SEPARATOR_LEN + MAX_R1_C1_LEN);
        if (NameUtils.ShouldQuote(firstSheet) || NameUtils.ShouldQuote(lastSheet))
        {
            sb
                .Append('\'')
                .AppendBookIndex(workbookIndex)
                .AppendEscapedSheetName(firstSheet)
                .Append(':')
                .AppendEscapedSheetName(lastSheet)
                .Append('\'');
        }
        else
        {
            sb
                .AppendBookIndex(workbookIndex)
                .Append(firstSheet)
                .Append(':')
                .Append(lastSheet);
        }

        var nodeText = sb
            .AppendReferenceSeparator()
            .AppendRef(reference)
            .ToString();
        return ToSymbol(ctx, range, nodeText);
    }

    /// <inheritdoc />
    public virtual TransformedSymbol Function(ModContext ctx, SymbolRange range, ReadOnlySpan<char> functionName, IReadOnlyList<TransformedSymbol> arguments)
    {
        return BuildFunction(ctx, range, functionName, arguments);
    }

    /// <inheritdoc />
    public virtual TransformedSymbol ExternalFunction(ModContext ctx, SymbolRange range, int workbookIndex, ReadOnlySpan<char> functionName, IReadOnlyList<TransformedSymbol> arguments)
    {
        var prefix = new StringBuilder(BOOK_PREFIX_LEN + SHEET_SEPARATOR_LEN + functionName.Length)
            .AppendBookIndex(workbookIndex)
            .AppendReferenceSeparator()
            .Append(functionName)
            .ToString();
        return BuildFunction(ctx, range, prefix.AsSpan(), arguments);
    }

    /// <inheritdoc />
    public virtual TransformedSymbol Function(ModContext ctx, SymbolRange range, ReadOnlySpan<char> sheetName, ReadOnlySpan<char> functionName, IReadOnlyList<TransformedSymbol> arguments)
    {
        var prefix = new StringBuilder(sheetName.Length + QUOTE_RESERVE + SHEET_SEPARATOR_LEN + functionName.Length)
            .AppendSheetReference(sheetName)
            .Append(functionName)
            .ToString();
        return BuildFunction(ctx, range, prefix.AsSpan(), arguments);
    }

    /// <inheritdoc />
    public virtual TransformedSymbol ExternalFunction(ModContext ctx, SymbolRange range, int workbookIndex, ReadOnlySpan<char> sheetName, ReadOnlySpan<char> functionName, IReadOnlyList<TransformedSymbol> arguments)
    {
        var prefix = new StringBuilder(BOOK_PREFIX_LEN + sheetName.Length + QUOTE_RESERVE + SHEET_SEPARATOR_LEN + functionName.Length)
            .AppendExternalSheetReference(workbookIndex, sheetName)
            .Append(functionName)
            .ToString();
        return BuildFunction(ctx, range, prefix.AsSpan(), arguments);
    }

    /// <inheritdoc />
    public virtual TransformedSymbol CellFunction(ModContext ctx, SymbolRange range, RowCol cell, IReadOnlyList<TransformedSymbol> arguments)
    {
        Span<char> prefix = stackalloc char[RowCol.MaxDisplayLength];
        return BuildFunction(ctx, range, prefix[..cell.Format(prefix)], arguments);
    }

    /// <inheritdoc />
    public virtual TransformedSymbol StructureReference(ModContext ctx, SymbolRange range,
        StructuredReferenceArea area, ReadOnlySpan<char> firstColumn, ReadOnlySpan<char> lastColumn)
    {
        var nodeText = GetIntraTableReference(area, firstColumn, lastColumn);
        return ToSymbol(ctx, range, nodeText);
    }

    /// <inheritdoc />
    public virtual TransformedSymbol StructureReference(ModContext ctx, SymbolRange range,
        ReadOnlySpan<char> table, StructuredReferenceArea area, ReadOnlySpan<char> firstColumn, ReadOnlySpan<char> lastColumn)
    {
        var nodeText = string.Concat(table, GetIntraTableReference(area, firstColumn, lastColumn).AsSpan());
        return ToSymbol(ctx, range, nodeText);
    }

    /// <inheritdoc />
    public virtual TransformedSymbol ExternalStructureReference(ModContext ctx,
        SymbolRange range, int workbookIndex, ReadOnlySpan<char> table, StructuredReferenceArea area, ReadOnlySpan<char> firstColumn,
        ReadOnlySpan<char> lastColumn)
    {
        var nodeText = new StringBuilder()
            .AppendBookIndex(workbookIndex)
            .AppendReferenceSeparator()
            .Append(table)
            .Append(GetIntraTableReference(area, firstColumn, lastColumn))
            .ToString();
        return ToSymbol(ctx, range, nodeText);
    }

    /// <inheritdoc />
    public virtual TransformedSymbol Name(ModContext ctx, SymbolRange range, ReadOnlySpan<char> name)
    {
        return TransformedSymbol.CopyOriginal(ctx.Formula, range);
    }

    /// <inheritdoc />
    public virtual TransformedSymbol SheetName(ModContext ctx, SymbolRange range, ReadOnlySpan<char> sheet, ReadOnlySpan<char> name)
    {
        var sb = new StringBuilder(sheet.Length + QUOTE_RESERVE + SHEET_SEPARATOR_LEN + name.Length);
        var nodeText = sb
            .AppendSheetReference(sheet)
            .Append(name)
            .ToString();
        return ToSymbol(ctx, range, nodeText);
    }

    /// <inheritdoc />
    public virtual TransformedSymbol BangName(ModContext ctx, SymbolRange range, ReadOnlySpan<char> name)
    {
        var sb = new StringBuilder(SHEET_SEPARATOR_LEN + name.Length);
        var nodeText = sb
            .AppendReferenceSeparator()
            .Append(name)
            .ToString();
        return ToSymbol(ctx, range, nodeText);
    }

    /// <inheritdoc />
    public virtual TransformedSymbol ExternalName(ModContext ctx, SymbolRange range, int workbookIndex, ReadOnlySpan<char> name)
    {
        var sb = new StringBuilder(BOOK_PREFIX_LEN + SHEET_SEPARATOR_LEN + name.Length);
        var nodeText = sb
            .AppendBookIndex(workbookIndex)
            .AppendReferenceSeparator()
            .Append(name)
            .ToString();
        return ToSymbol(ctx, range, nodeText);
    }

    /// <inheritdoc />
    public virtual TransformedSymbol ExternalSheetName(ModContext ctx, SymbolRange range, int workbookIndex, ReadOnlySpan<char> sheet, ReadOnlySpan<char> name)
    {
        var sb = new StringBuilder(BOOK_PREFIX_LEN + sheet.Length + QUOTE_RESERVE + SHEET_SEPARATOR_LEN + name.Length);
        var nodeText = sb
            .AppendExternalSheetReference(workbookIndex, sheet)
            .Append(name)
            .ToString();
        return ToSymbol(ctx, range, nodeText);
    }

    /// <inheritdoc />
    public virtual TransformedSymbol BinaryNode(ModContext ctx, SymbolRange range, BinaryOperation operation, TransformedSymbol leftNode, TransformedSymbol rightNode)
    {
        return BuildNode(ctx, range, range.Start, ReadOnlySpan<char>.Empty, [leftNode, rightNode]);
    }

    /// <inheritdoc />
    public virtual TransformedSymbol Unary(ModContext ctx, SymbolRange range, UnaryOperation operation, TransformedSymbol node)
    {
        return BuildNode(ctx, range, range.Start, ReadOnlySpan<char>.Empty, [node]);
    }

    /// <inheritdoc />
    public virtual TransformedSymbol Nested(ModContext ctx, SymbolRange range, TransformedSymbol node)
    {
        return BuildNode(ctx, range, range.Start, ReadOnlySpan<char>.Empty, [node]);
    }

    /// <summary>
    /// Create a symbol for a node text. A text that is the same as the original text of the range
    /// becomes a copy of the original, so that the parent nodes can also keep the original text.
    /// </summary>
    private static TransformedSymbol ToSymbol(ModContext ctx, SymbolRange range, string nodeText)
    {
        var original = ctx.Formula.AsSpan(range.Start, range.Length);
        return original.SequenceEqual(nodeText.AsSpan())
            ? TransformedSymbol.CopyOriginal(ctx.Formula, range)
            : TransformedSymbol.ToText(ctx.Formula, range, nodeText);
    }

    /// <inheritdoc cref="ToSymbol(ModContext, SymbolRange, string)"/>
    /// <remarks>The method allocates a string only when the text is not the same as the original text.</remarks>
    private static TransformedSymbol ToSymbol(ModContext ctx, SymbolRange range, ReadOnlySpan<char> nodeText)
    {
        var original = ctx.Formula.AsSpan(range.Start, range.Length);
        return original.SequenceEqual(nodeText)
            ? TransformedSymbol.CopyOriginal(ctx.Formula, range)
            : TransformedSymbol.ToText(ctx.Formula, range, nodeText.ToString());
    }

    /// <summary>
    /// Create a symbol for a function: the <paramref name="prefix"/> (for example a sheet and a
    /// function name), then the braces and the arguments.
    /// </summary>
    private static TransformedSymbol BuildFunction(ModContext ctx, SymbolRange range, ReadOnlySpan<char> prefix, IReadOnlyList<TransformedSymbol> arguments)
    {
        var nodeStart = arguments.Count == 0 ? range.End : arguments[0].OriginalRange.Start;
        var braceIdx = StringBuilderExtensions.GetStartBraceIndex(ctx, range, nodeStart);
        return BuildNode(ctx, range, braceIdx, prefix, AsSpan(arguments));
    }

    /// <summary>
    /// Create a symbol for an inner node. The text of the node is the <paramref name="prefix"/>,
    /// then the original formula text from <paramref name="start"/> to the end of the
    /// <paramref name="range"/>, where the text of each child replaces the original range of the
    /// child. When the prefix and all children are the same as the original, the node is a copy
    /// of the original and nothing is allocated.
    /// </summary>
    /// <param name="ctx">The context with the original formula.</param>
    /// <param name="range">The range of the node in the original formula.</param>
    /// <param name="start">The index in the original formula where the copied text starts. The text from the start of the range to this index is replaced by the prefix.</param>
    /// <param name="prefix">The text before the copied text.</param>
    /// <param name="children">The child symbols, in the order of the formula text.</param>
    private static TransformedSymbol BuildNode(ModContext ctx, SymbolRange range, int start, ReadOnlySpan<char> prefix, ReadOnlySpan<TransformedSymbol> children)
    {
        var formula = ctx.Formula;
        var isOriginal = prefix.SequenceEqual(formula.AsSpan(range.Start, start - range.Start));

        // The text between the children comes from the original formula. A gap is copied only
        // when it has a positive length.
        var length = prefix.Length;
        var position = start;
        foreach (var child in children)
        {
            var childRange = child.OriginalRange;
            if (childRange.Start >= position)
                length += childRange.Start - position;
            else
                isOriginal = false;

            length += child.Length;
            isOriginal &= child.IsOriginalIn(formula);
            position = childRange.End;
        }

        if (range.End >= position)
            length += range.End - position;
        else
            isOriginal = false;

        if (isOriginal)
            return TransformedSymbol.CopyOriginal(formula, range);

        var parts = new NodeParts(formula, start, range.End, prefix, children);
        var nodeText = string.Create(length, parts, static (text, parts) =>
        {
            parts.Prefix.CopyTo(text);
            text = text.Slice(parts.Prefix.Length);
            var position = parts.Start;
            foreach (var child in parts.Children)
            {
                var childRange = child.OriginalRange;
                if (childRange.Start > position)
                {
                    var gap = parts.Formula.AsSpan(position, childRange.Start - position);
                    gap.CopyTo(text);
                    text = text.Slice(gap.Length);
                }

                var childText = child.AsSpan();
                childText.CopyTo(text);
                text = text.Slice(childText.Length);
                position = childRange.End;
            }

            if (parts.End > position)
                parts.Formula.AsSpan(position, parts.End - position).CopyTo(text);
        });
        return TransformedSymbol.ToText(formula, range, nodeText);
    }

    private static ReadOnlySpan<TransformedSymbol> AsSpan(IReadOnlyList<TransformedSymbol> symbols)
    {
        return symbols switch
        {
            TransformedSymbol[] array => array,
            List<TransformedSymbol> list => CollectionsMarshal.AsSpan(list),
            _ => ToArray(symbols),
        };

        static TransformedSymbol[] ToArray(IReadOnlyList<TransformedSymbol> symbols)
        {
            var array = new TransformedSymbol[symbols.Count];
            for (var i = 0; i < array.Length; ++i)
                array[i] = symbols[i];

            return array;
        }
    }

    private readonly ref struct NodeParts(string formula, int start, int end, ReadOnlySpan<char> prefix, ReadOnlySpan<TransformedSymbol> children)
    {
        public string Formula { get; } = formula;

        public int Start { get; } = start;

        public int End { get; } = end;

        public ReadOnlySpan<char> Prefix { get; } = prefix;

        public ReadOnlySpan<TransformedSymbol> Children { get; } = children;
    }

    private static string GetIntraTableReference(StructuredReferenceArea area, ReadOnlySpan<char> firstColumn, ReadOnlySpan<char> lastColumn)
    {
        if (firstColumn.IsEmpty || lastColumn.IsEmpty)
        {
            // No column

            // Shorthand for full table inside the table.
            if (area == StructuredReferenceArea.None)
                return "[]";

            if (area == (StructuredReferenceArea.Headers | StructuredReferenceArea.Data))
                return "[[#Headers],[#Data]]";

            if (area == (StructuredReferenceArea.Data | StructuredReferenceArea.Totals))
                return "[[#Data],[#Totals]]";

            return Keyword(area);
        }

        if (firstColumn.SequenceEqual(lastColumn))
        {
            // One column
            if (area == StructuredReferenceArea.None)
            {
                // One column, no keyword
                return new StringBuilder(firstColumn.Length + 2)
                    .Append('[').Append(firstColumn).Append(']')
                    .ToString();
            }

            // One column, keyword
            var keywordList = KeywordList(area);
            return new StringBuilder(keywordList.Length + firstColumn.Length + 5)
                .Append('[')
                .Append(keywordList).Append(',')
                .Append('[').Append(firstColumn).Append(']')
                .Append(']')
                .ToString();
        }
        else
        {
            // Two columns
            var keywordList = KeywordList(area);
            var sb = new StringBuilder(firstColumn.Length + lastColumn.Length + keywordList.Length + 8);
            sb.Append('[');
            if (keywordList.Length > 0)
                sb.Append(keywordList).Append(',');

            return sb
                .Append('[').Append(firstColumn).Append(']')
                .Append(':')
                .Append('[').Append(lastColumn).Append(']')
                .Append(']')
                .ToString();
        }

        static string KeywordList(StructuredReferenceArea area)
        {
            return area switch
            {
                StructuredReferenceArea.Headers | StructuredReferenceArea.Data => "[#Headers],[#Data]",
                StructuredReferenceArea.Data | StructuredReferenceArea.Totals => "[#Data],[#Totals]",
                _ => Keyword(area),
            };
        }

        static string Keyword(StructuredReferenceArea area)
        {
            return area switch
            {
                StructuredReferenceArea.None => string.Empty,
                StructuredReferenceArea.Headers => "[#Headers]",
                StructuredReferenceArea.Data => "[#Data]",
                StructuredReferenceArea.Totals => "[#Totals]",
                StructuredReferenceArea.All => "[#All]",
                StructuredReferenceArea.ThisRow => "[#This Row]",
                _ => throw new NotSupportedException(),
            };
        }
    }
}