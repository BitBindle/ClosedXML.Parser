using System.Text;

namespace ClosedXML.Parser;

public record ExternalStructureReferenceNode(
    int WorkbookIndex,
    string Table,
    StructuredReferenceArea Area,
    string? FirstColumn,
    string? LastColumn) : AstNode
{
    public override string GetDisplayString(ReferenceStyle style)
    {
        var sb = new StringBuilder();
        sb.Append('[').Append(WorkbookIndex).Append("]!");
        sb.Append(Table);
        return StructuredReferenceText.AppendIntraTableReference(sb, Area, FirstColumn, LastColumn).ToString();
    }
};