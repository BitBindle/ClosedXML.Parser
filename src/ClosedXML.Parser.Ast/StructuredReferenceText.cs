using System.Text;

namespace ClosedXML.Parser;

/// <summary>
/// Write the part of a structured reference in the brackets after the table name.
/// </summary>
internal static class StructuredReferenceText
{
    /// <summary>
    /// Append the area and the columns, for example <c>[[#Data],[First]:[Last]]</c>.
    /// </summary>
    /// <param name="sb">The builder to append to.</param>
    /// <param name="area">The area of the table.</param>
    /// <param name="firstColumn">The first column, unescaped, or <c>null</c> for no column.</param>
    /// <param name="lastColumn">The last column, unescaped, or <c>null</c> for no column.</param>
    public static StringBuilder AppendIntraTableReference(StringBuilder sb, StructuredReferenceArea area, string? firstColumn, string? lastColumn)
    {
        var isOneColumn = firstColumn is not null && (lastColumn is null || lastColumn == firstColumn);
        if (area == StructuredReferenceArea.None && isOneColumn)
            return sb.Append('[').Append(EscapeColumn(firstColumn!)).Append(']');

        // One keyword without a column has no outer brackets, for example Table[#Totals].
        var isOneKeyword = area is StructuredReferenceArea.All or StructuredReferenceArea.Data or
            StructuredReferenceArea.Headers or StructuredReferenceArea.Totals or StructuredReferenceArea.ThisRow;
        if (firstColumn is null && isOneKeyword)
            return sb.Append(area.GetDisplayString());

        sb.Append('[');

        var list = new List<string>(2);
        if (area != StructuredReferenceArea.None)
            list.Add(area.GetDisplayString());

        if (firstColumn is not null)
        {
            var columns = $"[{EscapeColumn(firstColumn)}]";
            if (!isOneColumn)
                columns += $":[{EscapeColumn(lastColumn!)}]";

            list.Add(columns);
        }

        sb.Append(string.Join(",", list));
        return sb.Append(']');
    }

    /// <summary>
    /// Put a tick before each <c>'</c>, <c>[</c>, <c>]</c> and <c>#</c> of a column name.
    /// </summary>
    private static string EscapeColumn(string column)
    {
        var sb = new StringBuilder(column.Length);
        foreach (var c in column)
        {
            if (c is '\'' or '[' or ']' or '#')
                sb.Append('\'');

            sb.Append(c);
        }

        return sb.ToString();
    }
}
