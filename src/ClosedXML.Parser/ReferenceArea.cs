using System;
using System.Text;

namespace ClosedXML.Parser;

/// <summary>
/// Due to frequency of an area in formulas, the grammar has a token that represents
/// an area in a sheet. This is the DTO from parser to engine. Two corners make an area
/// for A1 notation, but not for R1C1 (has several edge cases).
/// </summary>
public readonly record struct ReferenceArea : ISpanFormattable
{
    /// <summary>
    /// The maximum length of a display string, two <see cref="RowCol"/> and a colon.
    /// </summary>
    internal const int MaxDisplayLength = 2 * RowCol.MaxDisplayLength + 1;

    /// <summary>
    /// First reference. First in terms of position in formula, not position
    /// in sheet.
    /// </summary>
    public RowCol First { get; }

    /// <summary>
    /// Second reference. Second in terms of position in formula, not position
    /// in sheet. If area was specified using only one cell, the value is
    /// same as <see cref="First"/>.
    /// </summary>
    public RowCol Second { get; }

    /// <summary>
    /// Semantic style of reference.
    /// </summary>
    public ReferenceStyle Style => First.Style;

    /// <summary>
    /// Is area a row span (e.g. <c>$5:7</c> in A1 or <c>R[7]</c>, <c>R7:R[9]</c>)?
    /// </summary>
    internal bool IsRowSpan => First.IsRow && Second.IsRow;

    /// <summary>
    /// Is area a col span (e.g. <c>$C:Z</c> in A1 or <c>C[7]</c>, <c>C7:C[9]</c>)?
    /// </summary>
    internal bool IsColSpan => First.IsColumn && Second.IsColumn;

    /// <summary>
    /// Create a reference symbol using the two <see cref="RowCol"/> (e.g.
    /// <c>A1:B2</c>) or two columns (e.g. <c>A:D</c>) or two rows (e.g.
    /// <c>7:8</c>).
    /// </summary>
    public ReferenceArea(RowCol first, RowCol second)
    {
        if (first.IsA1 ^ second.IsA1)
            throw new ArgumentException("Both RowCol must use same semantic.");

        First = first;
        Second = second;
    }

    /// <summary>
    /// Create an area for a single reference.
    /// </summary>
    public ReferenceArea(RowCol rowCol)
        : this(rowCol, rowCol)
    {
    }

    /// <summary>
    /// Create a new area from a single <see cref="RowCol"/>.
    /// </summary>
    /// <param name="rowType">Row axis type of a reference.</param>
    /// <param name="rowPosition">Row position.</param>
    /// <param name="columnType">Column axis type of a reference.</param>
    /// <param name="columnPosition">Column position.</param>
    /// <param name="style">Semantic of the reference.</param>
    public ReferenceArea(ReferenceAxisType rowType, int rowPosition, ReferenceAxisType columnType,
        int columnPosition, ReferenceStyle style)
        : this(new RowCol(rowType, rowPosition, columnType, columnPosition, style))
    {
    }

    /// <summary>
    /// Create a new area from a single row/column intersection.
    /// </summary>
    /// <param name="rowPosition"><see cref="ReferenceAxisType.Relative"/> row.</param>
    /// <param name="columnPosition"><see cref="ReferenceAxisType.Relative"/> column.</param>
    /// <param name="style">Semantic of the reference.</param>
    internal ReferenceArea(int rowPosition, int columnPosition, ReferenceStyle style)
        : this(new RowCol(ReferenceAxisType.Relative, rowPosition, ReferenceAxisType.Relative, columnPosition, style))
    {
    }

    /// <summary>
    /// Render area in A1 notation. The content must be a valid content
    /// from A1 token. If both references are same, only one is converted
    /// to display string.
    /// </summary>
    public string GetDisplayStringA1()
    {
        Span<char> buffer = stackalloc char[MaxDisplayLength];
        return new string(buffer[..FormatA1(buffer)]);
    }

    /// <summary>
    /// Render area in R1C1 notation. The content must be a valid content
    /// from R1C1 token. If both references are same, only one is converted
    /// to display string.
    /// </summary>
    public string GetDisplayStringR1C1()
    {
        Span<char> buffer = stackalloc char[MaxDisplayLength];
        return new string(buffer[..FormatR1C1(buffer)]);
    }

    /// <summary>
    /// Get the area in its own notation, <em>A1</em> or <em>R1C1</em> (see <see cref="Style"/>).
    /// </summary>
    public override string ToString()
    {
        Span<char> buffer = stackalloc char[MaxDisplayLength];
        return new string(buffer[..Format(buffer)]);
    }

    /// <summary>
    /// Get the area in its own notation, <em>A1</em> or <em>R1C1</em> (see <see cref="Style"/>).
    /// </summary>
    /// <param name="format">Must be <c>null</c> or empty. There are no other formats.</param>
    /// <param name="formatProvider">Not used. A reference does not depend on a culture.</param>
    /// <exception cref="FormatException">The <paramref name="format"/> is not empty.</exception>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        RowCol.CheckFormat(format);
        return ToString();
    }

    /// <summary>
    /// Write the area in its own notation, <em>A1</em> or <em>R1C1</em> (see <see cref="Style"/>),
    /// into the <paramref name="destination"/>.
    /// </summary>
    /// <param name="destination">The span where to write the area.</param>
    /// <param name="charsWritten">The number of written chars, or 0 if the destination is too short.</param>
    /// <param name="format">Must be empty. There are no other formats.</param>
    /// <param name="provider">Not used. A reference does not depend on a culture.</param>
    /// <returns><c>true</c> if the area was written, <c>false</c> if the destination is too short.</returns>
    /// <exception cref="FormatException">The <paramref name="format"/> is not empty.</exception>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
    {
        RowCol.CheckFormat(format);
        Span<char> buffer = stackalloc char[MaxDisplayLength];
        var length = Format(buffer);
        if (!buffer[..length].TryCopyTo(destination))
        {
            charsWritten = 0;
            return false;
        }

        charsWritten = length;
        return true;
    }

    /// <summary>
    /// Convert A1 reference to R1C1.
    /// </summary>
    /// <remarks>Assumes reference is in A1.</remarks>
    internal ReferenceArea ToR1C1(int anchorRow, int anchorCol)
    {
        var first = First.ToR1C1(anchorRow, anchorCol);
        if (First != Second)
        {
            var second = Second.ToR1C1(anchorRow, anchorCol);
            return new ReferenceArea(first, second);
        }

        return new ReferenceArea(first, first);
    }

    /// <summary>
    /// Convert R1C1 reference to A1.
    /// </summary>
    /// <remarks>Assumes reference is in R1C1.</remarks>
    internal ReferenceArea? ToA1OrError(int anchorRow, int anchorCol)
    {
        var first = First.ToA1OrError(anchorRow, anchorCol);
        if (first is null)
            return null;

        var second = First != Second ? Second.ToA1OrError(anchorRow, anchorCol) : first;
        if (second is null)
            return null;

        return new ReferenceArea(first.Value, second.Value);
    }

    internal StringBuilder Append(StringBuilder sb)
    {
        Span<char> buffer = stackalloc char[MaxDisplayLength];
        return sb.Append(buffer[..Format(buffer)]);
    }

    /// <summary>
    /// Write the area in its own notation into the <paramref name="buffer"/>.
    /// </summary>
    /// <param name="buffer">A buffer with at least <see cref="MaxDisplayLength"/> chars.</param>
    /// <returns>The number of written chars.</returns>
    internal int Format(Span<char> buffer)
    {
        return Style == A1 ? FormatA1(buffer) : FormatR1C1(buffer);
    }

    /// <summary>
    /// Write the area in A1 notation into the <paramref name="buffer"/>.
    /// </summary>
    /// <remarks>Assumes reference is in A1.</remarks>
    private int FormatA1(Span<char> buffer)
    {
        // In A1, column and row span must have both parts specified.
        if (First == Second && !IsColSpan && !IsRowSpan)
            return First.FormatA1(buffer);

        var length = First.FormatA1(buffer);
        buffer[length++] = ':';
        return length + Second.FormatA1(buffer[length..]);
    }

    /// <summary>
    /// Write the area in R1C1 notation into the <paramref name="buffer"/>.
    /// </summary>
    /// <remarks>Assumes reference is in R1C1.</remarks>
    private int FormatR1C1(Span<char> buffer)
    {
        var length = First.FormatR1C1(buffer);
        if (First != Second)
        {
            buffer[length++] = ':';
            length += Second.FormatR1C1(buffer[length..]);
        }

        return length;
    }
}
