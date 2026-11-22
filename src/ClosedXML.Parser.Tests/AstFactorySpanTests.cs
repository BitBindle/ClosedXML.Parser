namespace ClosedXML.Parser.Tests;

/// <summary>
/// The factory gets the texts as spans. A text without an escaped char is a part of the formula,
/// so the parser allocates no string for it.
/// </summary>
public class AstFactorySpanTests
{
    [Theory]
    [Trait("Issue", "20")]
    [InlineData("\"text\"", "text")]
    [InlineData("{1,\"text\"}", "text")]
    [InlineData("Sheet1!A1", "Sheet1")]
    [InlineData("'Sheet 1'!A1", "Sheet 1")]
    [InlineData("Sheet1:Sheet2!A1", "Sheet1", "Sheet2")]
    [InlineData("'Sheet 1:Sheet 2'!A1", "Sheet 1", "Sheet 2")]
    [InlineData("[1]Sheet1!A1", "Sheet1")]
    [InlineData("'[1]Sheet 1:Sheet 2'!A1", "Sheet 1", "Sheet 2")]
    [InlineData("Sheet1!F(1)", "Sheet1")]
    [InlineData("[1]Sheet1!F(1)", "Sheet1")]
    [InlineData("Name1", "Name1")]
    [InlineData("Sheet1!Name1", "Sheet1", "Name1")]
    [InlineData("[1]!Name1", "Name1")]
    [InlineData("[1]Sheet1!Name1", "Sheet1", "Name1")]
    [InlineData("Table1[Col]", "Table1", "Col", "Col")]
    [InlineData("Table1[[#Data],[First]:[Last]]", "Table1", "First", "Last")]
    [InlineData("[1]!Table1[Col]", "Table1", "Col", "Col")]
    [InlineData("[Col]", "Col", "Col")]
    public void Text_without_escaped_chars_is_part_of_formula(string formula, params string[] expectedTexts)
    {
        var texts = Parse(formula);

        Assert.Equal(expectedTexts, texts.Select(x => x.Text));
        Assert.All(texts, x => Assert.True(x.IsPartOfFormula, $"'{x.Text}' is not a part of the formula."));
    }

    [Theory]
    [Trait("Issue", "20")]
    [InlineData("\"a\"\"b\"", "a\"b")]
    [InlineData("{\"a\"\"b\"}", "a\"b")]
    [InlineData("'It''s'!A1", "It's")]
    [InlineData("'A''s:B''s'!A1", "A's", "B's")]
    [InlineData("Table1[Col']]", "Col]", "Col]")]
    [InlineData("Table1[[#Data],[a'#b]]", "a#b", "a#b")]
    public void Text_with_escaped_chars_is_unescaped(string formula, params string[] expectedTexts)
    {
        var texts = Parse(formula).Where(x => !x.IsPartOfFormula);

        Assert.Equal(expectedTexts, texts.Select(x => x.Text));
    }

    [Fact]
    [Trait("Issue", "20")]
    public void Structure_reference_without_column_gives_empty_columns()
    {
        var texts = Parse("Table1[#Data]");

        Assert.Equal(["Table1"], texts.Select(x => x.Text));
    }

    private static List<(string Text, bool IsPartOfFormula)> Parse(string formula)
    {
        var visitor = new RecordingVisitor();
        var ctx = new ModContext(formula, "Sheet", 1, 1, isA1: true);
        FormulaParser<TransformedSymbol, TransformedSymbol, ModContext>.CellFormulaA1(formula, ctx, visitor);
        return visitor.Texts;
    }

    /// <summary>
    /// Record each text that the parser gives to the factory. An empty column is not recorded.
    /// </summary>
    private sealed class RecordingVisitor : CopyVisitor
    {
        public List<(string Text, bool IsPartOfFormula)> Texts { get; } = [];

        public override TransformedSymbol TextValue(ModContext ctx, SymbolRange range, ReadOnlySpan<char> text)
        {
            Record(ctx, text);
            return base.TextValue(ctx, range, text);
        }

        public override TransformedSymbol TextNode(ModContext ctx, SymbolRange range, ReadOnlySpan<char> text)
        {
            Record(ctx, text);
            return base.TextNode(ctx, range, text);
        }

        public override TransformedSymbol SheetReference(ModContext ctx, SymbolRange range, ReadOnlySpan<char> sheet, ReferenceArea reference)
        {
            Record(ctx, sheet);
            return base.SheetReference(ctx, range, sheet, reference);
        }

        public override TransformedSymbol Reference3D(ModContext ctx, SymbolRange range, ReadOnlySpan<char> firstSheet, ReadOnlySpan<char> lastSheet, ReferenceArea reference)
        {
            Record(ctx, firstSheet, lastSheet);
            return base.Reference3D(ctx, range, firstSheet, lastSheet, reference);
        }

        public override TransformedSymbol ExternalSheetReference(ModContext ctx, SymbolRange range, int workbookIndex, ReadOnlySpan<char> sheet, ReferenceArea reference)
        {
            Record(ctx, sheet);
            return base.ExternalSheetReference(ctx, range, workbookIndex, sheet, reference);
        }

        public override TransformedSymbol ExternalReference3D(ModContext ctx, SymbolRange range, int workbookIndex, ReadOnlySpan<char> firstSheet, ReadOnlySpan<char> lastSheet, ReferenceArea reference)
        {
            Record(ctx, firstSheet, lastSheet);
            return base.ExternalReference3D(ctx, range, workbookIndex, firstSheet, lastSheet, reference);
        }

        public override TransformedSymbol Function(ModContext ctx, SymbolRange range, ReadOnlySpan<char> sheetName, ReadOnlySpan<char> functionName, IReadOnlyList<TransformedSymbol> arguments)
        {
            Record(ctx, sheetName);
            return base.Function(ctx, range, sheetName, functionName, arguments);
        }

        public override TransformedSymbol ExternalFunction(ModContext ctx, SymbolRange range, int workbookIndex, ReadOnlySpan<char> sheetName, ReadOnlySpan<char> functionName, IReadOnlyList<TransformedSymbol> arguments)
        {
            Record(ctx, sheetName);
            return base.ExternalFunction(ctx, range, workbookIndex, sheetName, functionName, arguments);
        }

        public override TransformedSymbol StructureReference(ModContext ctx, SymbolRange range, StructuredReferenceArea area, ReadOnlySpan<char> firstColumn, ReadOnlySpan<char> lastColumn)
        {
            Record(ctx, firstColumn, lastColumn);
            return base.StructureReference(ctx, range, area, firstColumn, lastColumn);
        }

        public override TransformedSymbol StructureReference(ModContext ctx, SymbolRange range, ReadOnlySpan<char> table, StructuredReferenceArea area, ReadOnlySpan<char> firstColumn, ReadOnlySpan<char> lastColumn)
        {
            Record(ctx, table);
            Record(ctx, firstColumn, lastColumn);
            return base.StructureReference(ctx, range, table, area, firstColumn, lastColumn);
        }

        public override TransformedSymbol ExternalStructureReference(ModContext ctx, SymbolRange range, int workbookIndex, ReadOnlySpan<char> table, StructuredReferenceArea area, ReadOnlySpan<char> firstColumn, ReadOnlySpan<char> lastColumn)
        {
            Record(ctx, table);
            Record(ctx, firstColumn, lastColumn);
            return base.ExternalStructureReference(ctx, range, workbookIndex, table, area, firstColumn, lastColumn);
        }

        public override TransformedSymbol Name(ModContext ctx, SymbolRange range, ReadOnlySpan<char> name)
        {
            Record(ctx, name);
            return base.Name(ctx, range, name);
        }

        public override TransformedSymbol SheetName(ModContext ctx, SymbolRange range, ReadOnlySpan<char> sheet, ReadOnlySpan<char> name)
        {
            Record(ctx, sheet, name);
            return base.SheetName(ctx, range, sheet, name);
        }

        public override TransformedSymbol ExternalName(ModContext ctx, SymbolRange range, int workbookIndex, ReadOnlySpan<char> name)
        {
            Record(ctx, name);
            return base.ExternalName(ctx, range, workbookIndex, name);
        }

        public override TransformedSymbol ExternalSheetName(ModContext ctx, SymbolRange range, int workbookIndex, ReadOnlySpan<char> sheet, ReadOnlySpan<char> name)
        {
            Record(ctx, sheet, name);
            return base.ExternalSheetName(ctx, range, workbookIndex, sheet, name);
        }

        private void Record(ModContext ctx, ReadOnlySpan<char> text)
        {
            if (!text.IsEmpty)
                Texts.Add((text.ToString(), ctx.Formula.AsSpan().Overlaps(text)));
        }

        private void Record(ModContext ctx, ReadOnlySpan<char> first, ReadOnlySpan<char> second)
        {
            Record(ctx, first);
            Record(ctx, second);
        }
    }
}
