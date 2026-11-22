namespace ClosedXML.Parser.Tests;

public class RefModVisitorTests
{
    #region ModifySheet

    [Theory]
    [InlineData("Old!B7:$D$10", "Old", "New", "New!B7:$D$10")]
    [InlineData("Old!B7:$D$10", "Old", "New sheet", "'New sheet'!B7:$D$10")]
    [InlineData("'Old Mike''s sheet'!B7:$D$10", "Old Mike's sheet", "New Mike's sheet", "'New Mike''s sheet'!B7:$D$10")]
    public void ModifySheet_can_rename_sheet_name(string formula, string oldSheetName, string newSheetName, string modifiedFormula)
    {
        var factory = new FormulaVisitor { SheetMap = { { oldSheetName, newSheetName } } };
        AssertChangesA1(formula, factory, modifiedFormula);
    }

    [Theory]
    [Trait("Issue", "6")]
    [InlineData("Old!B7", "Old", "Q3", "'Q3'!B7")]
    [InlineData("Old!B7", "Old", "RC", "'RC'!B7")]
    [InlineData("Old!B7:Old!C8", "Old", "R1C1", "'R1C1'!B7:'R1C1'!C8")]
    [InlineData("SUM(Old!B7)", "Old", "2023", "SUM('2023'!B7)")]
    public void ModifySheet_quotes_new_sheet_name_that_reads_as_reference(string formula, string oldSheetName, string newSheetName, string modifiedFormula)
    {
        // Excel writes such a name in apostrophes, for example 'Q3'!A1.
        var factory = new FormulaVisitor { SheetMap = { { oldSheetName, newSheetName } } };
        AssertChangesA1(formula, factory, modifiedFormula);
    }

    [Theory]
    [InlineData("Old!#REF!", "Old", null, "#REF!#REF!")]
    [InlineData("Old!#REF!", "Old", "New", "New!#REF!")]
    [InlineData("'Old sheet'!#REF!", "Old sheet", "New sheet", "'New sheet'!#REF!")]
    [InlineData("'Old''s'!#REF!", "Old's", "New", "New!#REF!")]
    public void ErrorNode_can_modify_sheet(string formula, string oldSheetName, string? newSheetName, string modifiedFormula)
    {
        var factory = new FormulaVisitor { SheetMap = { { oldSheetName, newSheetName } } };
        AssertChangesA1(formula, factory, modifiedFormula);
    }

    [Theory]
    [InlineData("'Old sheet'!#REF!")]
    [InlineData("'It''s'!#REF!")]
    public void ErrorNode_with_sheet_keeps_formula_when_sheet_does_not_change(string formula)
    {
        // The sheet name in the formula is in quotes, but the visitor gets the unescaped name.
        var factory = new FormulaVisitor { SheetMap = { { "Old", "New" } } };
        AssertChangesA1(formula, factory, formula);
    }

    [Theory]
    [InlineData("[1]Old!#REF!")]
    [InlineData("'[1]Old'!#REF!")]
    public void ErrorNode_keeps_sheet_of_other_workbook(string formula)
    {
        // The map renames the local sheet Old, not the sheet Old of another workbook.
        var factory = new FormulaVisitor { SheetMap = { { "Old", "New" } } };
        AssertChangesA1(formula, factory, formula);
    }

    [Theory]
    [InlineData("Old!B$5", "Old", null, "#REF!")]
    [InlineData("Old!B:D", "Old", "Shiny", "Shiny!B:D")]
    public void SheetReference_can_modify_sheet(string formula, string oldSheetName, string? newSheetName, string modifiedFormula)
    {
        var factory = new FormulaVisitor { SheetMap = { { oldSheetName, newSheetName } } };
        AssertChangesA1(formula, factory, modifiedFormula);
    }

    [Theory]
    [InlineData("Old!F(5)", "Old", null, "#REF!")]
    [InlineData("Old!F(7)", "Old", "Shiny", "Shiny!F(7)")]
    public void SheetFunction_can_modify_sheet(string formula, string oldSheetName, string? newSheetName, string modifiedFormula)
    {
        var factory = new FormulaVisitor { SheetMap = { { oldSheetName, newSheetName } } };
        AssertChangesA1(formula, factory, modifiedFormula);
    }

    [Theory]
    [InlineData("Sheet1:Sheet5!A1", "Sheet1", null, "#REF!")]
    [InlineData("Sheet1:Sheet5!A1", "Sheet1", "New sheet", "'New sheet:Sheet5'!A1")]
    [InlineData("Sheet1:Sheet5!A1", "Sheet5", "Sheet9", "Sheet1:Sheet9!A1")]
    public void Reference3D_can_modify_sheet(string formula, string oldSheetName, string? newSheetName, string modifiedFormula)
    {
        var factory = new FormulaVisitor { SheetMap = { { oldSheetName, newSheetName } } };
        AssertChangesA1(formula, factory, modifiedFormula);
    }

    [Theory]
    [InlineData("Sheet!Name", "Sheet", null, "#REF!")]
    [InlineData("Sheet!Name", "Sheet", "New Sheet", "'New Sheet'!Name")]
    public void SheetName_can_modify_sheet(string formula, string oldSheetName, string? newSheetName, string modifiedFormula)
    {
        var factory = new FormulaVisitor { SheetMap = { { oldSheetName, newSheetName } } };
        AssertChangesA1(formula, factory, modifiedFormula);
    }

    #endregion

    #region ModifyTable

    [Theory]
    [Trait("Issue", "20")]
    [InlineData("Old[Col]", "Old", "New", "New[Col]")]
    [InlineData("SUM(Old[[#Data],[A]:[B]])", "Old", "New", "SUM(New[[#Data],[A]:[B]])")]
    [InlineData("Old[Col]", "Old", null, "#REF!")]
    [InlineData("Other[Col]", "Old", null, "Other[Col]")]
    [InlineData("Old[Col']]", "Old", "New", "New[Col']]")]
    [InlineData("Old[[#Data],[A'[]:[B'#]]", "Old", "New", "New[[#Data],[A'[]:[B'#]]")]
    public void ModifyTable_can_rename_or_delete_table(string formula, string oldTableName, string? newTableName, string modifiedFormula)
    {
        var factory = new TableVisitor { TableMap = { { oldTableName, newTableName } } };
        AssertChangesA1(formula, factory, modifiedFormula);
    }

    #endregion

    [Theory]
    [InlineData("5 + !$B1", "$B1", "$7:$9", "5 + !$7:$9")]
    [InlineData("5 + !$B1", "$B1", null, "5 + !#REF!")]
    public void Bang_references_is_modified(string formula, string reference, string? replacement, string modifiedFormula)
    {
        var factory = new ShiftReferenceVisitor { ReferenceMap = { { reference, replacement } } };
        AssertChangesA1(formula, factory, modifiedFormula);
    }

    [Fact]
    public void Log10_is_not_interpreted_as_cell_function()
    {
        var factory = new ShiftReferenceVisitor { ReferenceMap = { { "LOG10", "A1"} } };
        AssertChangesA1("LOG10(LOG10)", factory, "LOG10(A1)");
    }

    private static void AssertChangesA1(string formula, RefModVisitor visitor, string expected)
    {
        var ctx = new ModContext(formula, "Sheet", 1, 1, isA1: true);
        var modifiedFormula = FormulaParser<TransformedSymbol, TransformedSymbol, ModContext>.CellFormulaA1(formula, ctx, visitor);
        Assert.Equal(expected, modifiedFormula.ToString(string.Empty.AsSpan()));
    }

    private class FormulaVisitor : RefModVisitor
    {
        public Dictionary<string, string?> SheetMap { get; } = new();

        protected override ReadOnlySpan<char> ModifySheet(ModContext ctx, ReadOnlySpan<char> sheetName)
        {
            // A null value is a deleted sheet, and it becomes an empty span.
            var lookup = SheetMap.GetAlternateLookup<ReadOnlySpan<char>>();
            return lookup.TryGetValue(sheetName, out var newName) ? newName : sheetName;
        }
    }

    private class TableVisitor : RefModVisitor
    {
        public Dictionary<string, string?> TableMap { get; } = new();

        protected override ReadOnlySpan<char> ModifyTable(ModContext ctx, ReadOnlySpan<char> table)
        {
            // A null value is a deleted table, and it becomes an empty span.
            var lookup = TableMap.GetAlternateLookup<ReadOnlySpan<char>>();
            return lookup.TryGetValue(table, out var newName) ? newName : table;
        }
    }

    private class ShiftReferenceVisitor : RefModVisitor
    {
        public Dictionary<string, string?> ReferenceMap { get; } = new();

        internal override ReferenceArea? ModifyRef(ModContext ctx, ReferenceArea reference)
        {
            if (ReferenceMap.TryGetValue(reference.GetDisplayStringA1(), out var replacement))
                return replacement is not null ? ReferenceParser.ParseA1(replacement) : null;

            return reference;
        }

        internal override RowCol? ModifyCellFunction(ModContext ctx, RowCol cell)
        {
            if (ReferenceMap.TryGetValue(cell.GetDisplayStringA1(), out var replacement))
                return replacement is not null ? ReferenceParser.ParseA1(replacement).First : null;

            return cell;
        }
    }
}
