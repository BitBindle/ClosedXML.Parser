using System.Text;

namespace ClosedXML.Parser;

public record StructureReferenceNode(
    string? Table, 
    StructuredReferenceArea Area,
    string? FirstColumn,
    string? LastColumn) : AstNode
{
    public override string GetDisplayString(ReferenceStyle style)
    {
        var sb = new StringBuilder();
        if (Table is not null)
            sb.Append(Table);

        return StructuredReferenceText.AppendIntraTableReference(sb, Area, FirstColumn, LastColumn).ToString();
    }
};