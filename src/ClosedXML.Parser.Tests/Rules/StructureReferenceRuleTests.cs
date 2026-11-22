namespace ClosedXML.Parser.Tests.Rules;

public class StructureReferenceRuleTests
{
    [Theory]
    [MemberData(nameof(TestCases))]
    public void Structure_reference_is_parsed_to_a_node(string formula, AstNode expectedNode)
    {
        AssertFormula.SingleNodeParsed(formula, expectedNode);
    }

    [Theory]
    [Trait("Issue", "32")]
    [InlineData("[[#Data],[#Totals]]", StructuredReferenceArea.Data | StructuredReferenceArea.Totals)]
    [InlineData("[ [#Headers] , [#Data] ]", StructuredReferenceArea.Headers | StructuredReferenceArea.Data)]
    [InlineData("[[#All]]", StructuredReferenceArea.All)]
    [InlineData("Table1[[#Headers],[#Data]]", StructuredReferenceArea.Headers | StructuredReferenceArea.Data)]
    public void Keyword_list_without_column_is_parsed(string formula, StructuredReferenceArea area)
    {
        // After the keywords, the end bracket can follow without a comma and a column.
        var table = formula.StartsWith("Table1") ? "Table1" : null;
        AssertFormula.SingleNodeParsed(formula, new StructureReferenceNode(table, area, null, null));
    }

    [Theory]
    [Trait("Issue", "32")]
    [InlineData("[Column]")]
    [InlineData("[]")]
    [InlineData("[#Totals]")]
    [InlineData("SomeTable[#This Row]")]
    [InlineData("SomeTable[[#Data],[#Totals]]")]
    [InlineData("SomeTable[[#Headers],[#Data],[Col]]")]
    [InlineData("SomeTable[Column]")]
    [InlineData("SomeTable[[#Data],[First]:[Last]]")]
    [InlineData("SomeTable[[First]:[Last]]")]
    [InlineData("SomeTable[[#This Row],[Col]]")]
    [InlineData("SomeTable[Col']]")]
    [InlineData("SomeTable[[A'[]:[B'#]]")]
    [InlineData("SomeTable[It''s]")]
    [InlineData("[4]!SomeTable[Column]")]
    [InlineData("[4]!SomeTable[]")]
    [InlineData("[4]!SomeTable[[#All],[Col']]]")]
    public void Display_string_of_structure_reference_is_the_formula(string formula)
    {
        var node = FormulaParser<ScalarValue, AstNode, Ctx>.CellFormulaA1(formula, new Ctx(), new F());

        Assert.Equal(formula, node.GetDisplayString(ReferenceStyle.A1));
    }

    public static IEnumerable<object[]> TestCases
    {
        get
        {
            // structure_reference : INTRA_TABLE_REFERENCE
            yield return new object[]
            {
                "[Column]",
                new StructureReferenceNode(null, StructuredReferenceArea.None, "Column", "Column")
            };

            yield return new object[]
            {
                "[#Totals]",
                new StructureReferenceNode(null, StructuredReferenceArea.Totals, null, null)
            };

            yield return new object[]
            {
                "[]",
                new StructureReferenceNode(null, StructuredReferenceArea.None, null, null)
            };

            yield return new object[]
            {
                "[[#Data],[First Column]:[Last Column]]",
                new StructureReferenceNode(null, StructuredReferenceArea.Data, "First Column", "Last Column")
            };

            // structure_reference : NAME INTRA_TABLE_REFERENCE
            yield return new object[]
            {
                "SomeTable[Column]",
                new StructureReferenceNode("SomeTable", StructuredReferenceArea.None, "Column", "Column")
            };

            // structure_reference: BOOK_PREFIX NAME INTRA_TABLE_REFERENCE
            yield return new object[]
            {
                "[4]!SomeTable[Column]",
                new ExternalStructureReferenceNode(4, "SomeTable", StructuredReferenceArea.None, "Column", "Column")
            };
        }
    }
}