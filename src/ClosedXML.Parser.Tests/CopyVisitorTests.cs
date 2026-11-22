using System.Collections.ObjectModel;

namespace ClosedXML.Parser.Tests;

public class CopyVisitorTests
{
    [Theory]
    [Trait("Issue", "21")]
    [InlineData("SUM( A1 , 'Sheet 1'!B2 )")]
    [InlineData("{1,2;3,4}")]
    [InlineData("-( A1 )%")]
    [InlineData("Sheet1!F( 7 )")]
    [InlineData("[1]Sheet1!A1+[1]!Name")]
    [InlineData("A1:B2 C1")]
    [InlineData("IF(A1,SUM( B1 ),)")]
    [InlineData("RAND( )")]
    public void Formula_without_changes_keeps_original_text(string formula)
    {
        AssertChangesA1(formula, new RefModVisitor(), formula);
    }

    [Theory]
    [Trait("Issue", "21")]
    [InlineData("a1+sum(b$2)", "A1+sum(B$2)")]
    [InlineData("sheet1!a1:b2", "sheet1!A1:B2")]
    public void Reference_is_written_in_upper_case(string formula, string expected)
    {
        // The visitor writes each reference again. Only the other text comes from the original.
        AssertChangesA1(formula, new RefModVisitor(), expected);
    }

    [Theory]
    [Trait("Issue", "21")]
    [InlineData("SUM(A1, B2) + C3", "SUM(D4, B2) + C3")]
    [InlineData("{1,2;3,4}+-(A1)", "{1,2;3,4}+-(D4)")]
    [InlineData("Sheet1!F( A1 )", "Sheet1!F( D4 )")]
    public void Changed_reference_changes_text_of_parent_nodes(string formula, string expected)
    {
        var visitor = new MoveA1Visitor();
        AssertChangesA1(formula, visitor, expected);
    }

    [Theory]
    [Trait("Issue", "21")]
    [InlineData("sum(A1, 2)", "TOTAL(A1, 2)")]
    [InlineData("sum()", "TOTAL()")]
    [InlineData("1+sum( 5 )", "1+TOTAL( 5 )")]
    public void New_function_name_is_kept_when_arguments_do_not_change(string formula, string expected)
    {
        AssertChangesA1(formula, new RenameFunctionVisitor(), expected);
    }

    [Theory]
    [Trait("Issue", "32")]
    [InlineData("Table1[Col']]")]
    [InlineData("Table1[Col'[]")]
    [InlineData("Table1[It''s]")]
    [InlineData("Table1['#Total]")]
    [InlineData("Table1[[#Data],[a'#b]]")]
    [InlineData("Table1[[A']]:[B'[]]")]
    [InlineData("Table1[[#Headers],[#Data],[A']]:[B'#]]")]
    [InlineData("[Col']]")]
    [InlineData("[4]!Table1[Col']]")]
    public void Structure_reference_column_keeps_escape_ticks(string formula)
    {
        // The parser gives the visitor the column name without the escape ticks.
        AssertChangesA1(formula, new RefModVisitor(), formula);
    }

    [Theory]
    [Trait("Issue", "21")]
    [InlineData("[4]!SomeTable[Column]")]
    [InlineData("[4]!SomeTable[[#Data],[Col]]")]
    public void External_structure_reference_keeps_separator_after_book(string formula)
    {
        // The book prefix of a structure reference is [N]! and the text without the ! can't be parsed.
        AssertChangesA1(formula, new RefModVisitor(), formula);
    }

    [Fact]
    [Trait("Issue", "21")]
    public void Bang_name_without_changes_is_copy_of_original()
    {
        // The parser doesn't create a bang name, so call the visitor directly.
        const string formula = "!Name";
        var ctx = new ModContext(formula, "Sheet", 1, 1, isA1: true);

        var node = new CopyVisitor().BangName(ctx, new SymbolRange(0, 5), "Name");

        Assert.Equal(formula, node.ToString());
    }

    [Fact]
    [Trait("Issue", "21")]
    public void Text_between_child_ranges_is_copied_only_when_child_ranges_do_not_overlap()
    {
        // The parser never makes such ranges, but the visitor must not fail for them.
        const string formula = "A1+B1";
        var ctx = new ModContext(formula, "Sheet", 1, 1, isA1: true);
        var left = TransformedSymbol.CopyOriginal(formula, new SymbolRange(0, 4));
        var right = TransformedSymbol.CopyOriginal(formula, new SymbolRange(3, 5));

        var node = new CopyVisitor().BinaryNode(ctx, new SymbolRange(0, 4), BinaryOperation.Addition, left, right);

        Assert.Equal("A1+BB1", node.ToString());
    }

    [Fact]
    [Trait("Issue", "21")]
    public void Function_accepts_argument_list_of_any_type()
    {
        const string formula = "F(A1,B1)";
        var ctx = new ModContext(formula, "Sheet", 1, 1, isA1: true);
        var arguments = new ReadOnlyCollection<TransformedSymbol>(
        [
            TransformedSymbol.ToText(formula, new SymbolRange(2, 4), "C3"),
            TransformedSymbol.CopyOriginal(formula, new SymbolRange(5, 7)),
        ]);

        var node = new CopyVisitor().Function(ctx, new SymbolRange(0, 8), "F".AsSpan(), arguments);

        Assert.Equal("F(C3,B1)", node.ToString());
    }

    [Fact]
    [Trait("Issue", "21")]
    public void Long_formula_without_changes_allocates_memory_linear_to_its_length()
    {
        // Before, each node made a new text of its full subtree, so a chain of N references
        // allocated memory proportional to N * N. A formula 4 times longer must allocate about
        // 4 times more memory, not 16 times.
        var shortAllocated = AllocatedBytesForChain(500);
        var longAllocated = AllocatedBytesForChain(2000);

        Assert.True(longAllocated < 8 * shortAllocated, $"Allocated {shortAllocated} bytes for 500 references and {longAllocated} bytes for 2000 references.");
    }

    [Fact]
    [Trait("Issue", "21")]
    public void Child_symbol_of_other_formula_text_is_not_copied_from_parent_formula()
    {
        var visitor = new OtherFormulaReferenceVisitor("B1+B2");

        AssertChangesA1("A1+A2", visitor, "B1+B2");
    }

    [Fact]
    [Trait("Issue", "22")]
    public void Sheet_reference_with_long_sheet_name_is_written()
    {
        // A long sheet name does not fit in the stack buffer, so the visitor uses a heap buffer.
        var sheet = "'" + new string('x', 150) + " y''s'";
        AssertChangesA1(sheet + "!A1", new CopyVisitor(), sheet + "!A1");
        AssertChangesA1(sheet + "!A1+1", new MoveA1Visitor(), sheet + "!D4+1");
        AssertChangesA1(sheet + "!#REF!", new RefModVisitor(), sheet + "!#REF!");
    }

    [Fact]
    [Trait("Issue", "22")]
    public void Changed_bang_reference_and_cell_function_are_written()
    {
        AssertChangesA1("!A1+A1(1)", new MoveA1Visitor(), "!D4+A1(1)");
    }

    private static long AllocatedBytesForChain(int referenceCount)
    {
        var formula = string.Join("+", Enumerable.Range(1, referenceCount).Select(i => "A" + i));
        var visitor = new RefModVisitor();
        AssertChangesA1(formula, visitor, formula);

        var before = GC.GetAllocatedBytesForCurrentThread();
        AssertChangesA1(formula, visitor, formula);
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private static void AssertChangesA1(string formula, IAstFactory<TransformedSymbol, TransformedSymbol, ModContext> visitor, string expected)
    {
        var ctx = new ModContext(formula, "Sheet", 1, 1, isA1: true);
        var modifiedFormula = FormulaParser<TransformedSymbol, TransformedSymbol, ModContext>.CellFormulaA1(formula, ctx, visitor);
        Assert.Equal(expected, modifiedFormula.ToString());
    }

    private sealed class MoveA1Visitor : RefModVisitor
    {
        internal override ReferenceArea? ModifyRef(ModContext ctx, ReferenceArea reference)
        {
            return reference.GetDisplayStringA1() == "A1" ? ReferenceParser.ParseA1("D4") : reference;
        }
    }

    private sealed class OtherFormulaReferenceVisitor(string otherFormula) : CopyVisitor
    {
        public override TransformedSymbol Reference(ModContext ctx, SymbolRange range, ReferenceArea reference)
        {
            return TransformedSymbol.CopyOriginal(otherFormula, range);
        }
    }

    private sealed class RenameFunctionVisitor : RefModVisitor
    {
        protected override ReadOnlySpan<char> ModifyFunction(ModContext ctx, ReadOnlySpan<char> functionName)
        {
            return functionName.Equals("sum", StringComparison.Ordinal) ? "TOTAL" : functionName;
        }
    }
}
