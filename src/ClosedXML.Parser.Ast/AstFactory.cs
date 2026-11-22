namespace ClosedXML.Parser;

public record Ctx;

public class F : IAstFactory<ScalarValue, AstNode, Ctx>
{
    public ScalarValue LogicalValue(Ctx _, SymbolRange range, bool value)
    {
        return new ScalarValue("Logical", value);
    }

    public ScalarValue NumberValue(Ctx _, SymbolRange range, double value)
    {
        return new ScalarValue("Number", value);
    }

    public ScalarValue TextValue(Ctx _, SymbolRange range, ReadOnlySpan<char> input)
    {
        return new ScalarValue("Text", input.ToString());
    }

    public ScalarValue ErrorValue(Ctx _, SymbolRange range, ReadOnlySpan<char> error)
    {
        return new ScalarValue("Error", error.ToString());
    }

    public AstNode BlankNode(Ctx _, SymbolRange range)
    {
        return new ValueNode("Blank", string.Empty);
    }

    public AstNode LogicalNode(Ctx _, SymbolRange range, bool value)
    {
        return new ValueNode("Logical", value);
    }

    public AstNode ErrorNode(Ctx _, SymbolRange range, ReadOnlySpan<char> error)
    {
        return new ValueNode("Error", error.ToString());
    }

    public AstNode NumberNode(Ctx _, SymbolRange range, double value)
    {
        return new ValueNode("Number", value);
    }

    public AstNode TextNode(Ctx _, SymbolRange range, ReadOnlySpan<char> text)
    {
        return new ValueNode("Text", text.ToString());
    }

    public AstNode ArrayNode(Ctx _, SymbolRange range, int rows, int columns, IReadOnlyList<ScalarValue> array)
    {
        return new ArrayNode(rows, columns, array);
    }

    public AstNode Reference(Ctx _, SymbolRange range, ReferenceArea reference)
    {
        return new ReferenceNode(reference);
    }

    public AstNode SheetReference(Ctx _, SymbolRange range, ReadOnlySpan<char> sheet, ReferenceArea reference)
    {
        return new SheetReferenceNode(sheet.ToString(), reference);
    }

    public AstNode BangReference(Ctx _, SymbolRange range, ReferenceArea reference)
    {
        return new BangReferenceNode(reference);
    }

    public AstNode Reference3D(Ctx _, SymbolRange range, ReadOnlySpan<char> firstSheet, ReadOnlySpan<char> lastSheet, ReferenceArea reference)
    {
        return new Reference3DNode(firstSheet.ToString(), lastSheet.ToString(), reference);
    }

    public AstNode ExternalSheetReference(Ctx _, SymbolRange range, int workbookIndex, ReadOnlySpan<char> sheet, ReferenceArea reference)
    {
        return new ExternalSheetReferenceNode(workbookIndex, sheet.ToString(), reference);
    }

    public AstNode ExternalReference3D(Ctx _, SymbolRange range, int workbookIndex, ReadOnlySpan<char> firstSheet, ReadOnlySpan<char> lastSheet, ReferenceArea reference)
    {
        return new ExternalReference3DNode(workbookIndex, firstSheet.ToString(), lastSheet.ToString(), reference);
    }

    public AstNode Function(Ctx _, SymbolRange range, ReadOnlySpan<char> name, IReadOnlyList<AstNode> args)
    {
        return new FunctionNode(null, name.ToString()) { Children = args.ToArray() };
    }

    public AstNode Function(Ctx _, SymbolRange range, ReadOnlySpan<char> sheet, ReadOnlySpan<char> name, IReadOnlyList<AstNode> args)
    {
        return new FunctionNode(sheet.ToString(), name.ToString()) { Children = args.ToArray() };
    }

    public AstNode ExternalFunction(Ctx _, SymbolRange range, int workbookIndex, ReadOnlySpan<char> sheetName, ReadOnlySpan<char> name, IReadOnlyList<AstNode> args)
    {
        return new ExternalFunctionNode(workbookIndex, sheetName.ToString(), name.ToString())
        {
            Children = args.ToArray()
        };
    }

    public AstNode ExternalFunction(Ctx _, SymbolRange range, int workbookIndex, ReadOnlySpan<char> name, IReadOnlyList<AstNode> args)
    {
        return new ExternalFunctionNode(workbookIndex, null, name.ToString())
        {
            Children = args.ToArray()
        };
    }

    public AstNode StructureReference(Ctx _, SymbolRange range, StructuredReferenceArea area, ReadOnlySpan<char> firstColumn, ReadOnlySpan<char> lastColumn)
    {
        return new StructureReferenceNode(null, area, ToColumn(firstColumn), ToColumn(lastColumn));
    }

    public AstNode StructureReference(Ctx _, SymbolRange range, ReadOnlySpan<char> table, StructuredReferenceArea area, ReadOnlySpan<char> firstColumn, ReadOnlySpan<char> lastColumn)
    {
        return new StructureReferenceNode(table.ToString(), area, ToColumn(firstColumn), ToColumn(lastColumn));
    }

    public AstNode ExternalStructureReference(Ctx _, SymbolRange range, int workbookIndex, ReadOnlySpan<char> table, StructuredReferenceArea area, ReadOnlySpan<char> firstColumn, ReadOnlySpan<char> lastColumn)
    {
        return new ExternalStructureReferenceNode(workbookIndex, table.ToString(), area, ToColumn(firstColumn), ToColumn(lastColumn));
    }

    public AstNode CellFunction(Ctx _, SymbolRange range, RowCol cell, IReadOnlyList<AstNode> args)
    {
        return new CellFunctionNode(cell)
        {
            Children = args.ToArray()
        };
    }

    public AstNode Name(Ctx _, SymbolRange range, ReadOnlySpan<char> name)
    {
        return new NameNode(name.ToString());
    }

    public AstNode SheetName(Ctx _, SymbolRange range, ReadOnlySpan<char> sheet, ReadOnlySpan<char> name)
    {
        return new SheetNameNode(sheet.ToString(), name.ToString());
    }

    public AstNode BangName(Ctx context, SymbolRange range, ReadOnlySpan<char> name)
    {
        return new BangNameNode(name.ToString());
    }

    public AstNode ExternalName(Ctx _, SymbolRange range, int workbookIndex, ReadOnlySpan<char> name)
    {
        return new ExternalNameNode(workbookIndex, name.ToString());
    }

    public AstNode ExternalSheetName(Ctx _, SymbolRange range, int workbookIndex, ReadOnlySpan<char> sheet, ReadOnlySpan<char> name)
    {
        return new ExternalSheetNameNode(workbookIndex, sheet.ToString(), name.ToString());
    }

    public AstNode BinaryNode(Ctx _, SymbolRange range, BinaryOperation operation, AstNode leftNode, AstNode rightNode)
    {
        return new BinaryNode(operation, leftNode, rightNode);
    }

    public AstNode Unary(Ctx _, SymbolRange range, UnaryOperation operation, AstNode node)
    {
        return new UnaryNode(operation) { Children = new[] { node } };
    }

    public AstNode Nested(Ctx _, SymbolRange range, AstNode node)
    {
        // The tree shape gives the precedence, so a node for the parentheses adds no data.
        return node;
    }

    /// <summary>
    /// The AST nodes use null for a reference without a column.
    /// </summary>
    private static string? ToColumn(ReadOnlySpan<char> column)
    {
        return column.IsEmpty ? null : column.ToString();
    }
}