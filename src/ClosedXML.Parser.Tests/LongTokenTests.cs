using System.Runtime.ExceptionServices;

namespace ClosedXML.Parser.Tests;

/// <summary>
/// A formula has no length limit, thus a token can be very long. The parser must not put a
/// buffer of the token length on the stack, because the stack can overflow and stop the process.
/// </summary>
public class LongTokenTests
{
    // Each test parses on a thread with a small stack. A buffer of the token length on the
    // stack (2 bytes for each char) is larger than the stack.
    private const int StackSize = 256 * 1024;
    private const int Repeats = 500_000;

    [Fact]
    [Trait("Issue", "18")]
    public void Can_parse_long_text_literal()
    {
        var text = new string('a', 2 * Repeats);

        var node = ParseOnSmallStack("\"" + text + "\"");

        Assert.Equal(new ValueNode("Text", text), node);
    }

    [Fact]
    [Trait("Issue", "18")]
    public void Can_parse_long_text_literal_with_escaped_quotes()
    {
        var text = Repeat("a\"");

        var node = ParseOnSmallStack("\"" + text.Replace("\"", "\"\"") + "\"");

        Assert.Equal(new ValueNode("Text", text), node);
    }

    [Fact]
    [Trait("Issue", "18")]
    public void Can_parse_long_text_literal_in_array()
    {
        var text = Repeat("a\"");

        var node = (ArrayNode)ParseOnSmallStack("{\"" + text.Replace("\"", "\"\"") + "\",1}");

        Assert.Equal(new ScalarValue("Text", text), node.Elements[0]);
        Assert.Equal(new ScalarValue("Number", 1d), node.Elements[1]);
    }

    [Fact]
    [Trait("Issue", "18")]
    public void Can_parse_long_quoted_sheet_name()
    {
        var sheet = Repeat("b'");

        var node = ParseOnSmallStack("'" + sheet.Replace("'", "''") + "'!A1");

        Assert.Equal(new SheetReferenceNode(sheet, new ReferenceArea(1, 1, A1)), node);
    }

    [Fact]
    [Trait("Issue", "18")]
    public void Can_parse_long_quoted_sheet_names_in_3d_reference()
    {
        var firstSheet = Repeat("c'");
        var lastSheet = Repeat("d'");

        var node = ParseOnSmallStack("'" + firstSheet.Replace("'", "''") + ":" + lastSheet.Replace("'", "''") + "'!A1");

        Assert.Equal(new Reference3DNode(firstSheet, lastSheet, new ReferenceArea(1, 1, A1)), node);
    }

    [Fact]
    [Trait("Issue", "18")]
    public void Can_parse_long_escaped_column_names_in_structured_reference()
    {
        var firstColumn = Repeat("e[");
        var lastColumn = Repeat("f]");

        var node = ParseOnSmallStack("Table[[" + firstColumn.Replace("[", "'[") + "]:[" + lastColumn.Replace("]", "']") + "]]");

        Assert.Equal(new StructureReferenceNode("Table", StructuredReferenceArea.None, firstColumn, lastColumn), node);
    }

    private static string Repeat(string part) => string.Concat(Enumerable.Repeat(part, Repeats));

    private static AstNode ParseOnSmallStack(string formula)
    {
        AstNode? node = null;
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                node = FormulaParser<ScalarValue, AstNode, Ctx>.CellFormulaA1(formula, new Ctx(), new F());
            }
            catch (Exception e)
            {
                error = e;
            }
        }, StackSize);
        thread.Start();
        thread.Join();

        if (error is not null)
            ExceptionDispatchInfo.Throw(error);

        return node!;
    }
}
