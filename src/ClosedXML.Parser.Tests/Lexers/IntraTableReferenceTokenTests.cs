using Xunit;

namespace ClosedXML.Parser.Tests.Lexers;

public class IntraTableReferenceTokenTests
{
    [Theory]
    [MemberData(nameof(Data))]
    public void Token_data_are_extracted_and_unescaped(string tokenText, StructuredReferenceArea expectedArea, string expectedFirstColumn, string expectedLastColumn)
    {
        AssertFormula.AssertTokenType(tokenText, FormulaLexer.INTRA_TABLE_REFERENCE);
        TokenParser.ParseIntraTableReference(tokenText, out var area, out var firstColumn, out var lastColumn);

        Assert.Equal(expectedArea, area);
        Assert.Equal(expectedFirstColumn, firstColumn.ToString());
        Assert.Equal(expectedLastColumn, lastColumn.ToString());
    }

    public static IEnumerable<object?[]> Data
    {
        get
        {
            // An empty span is no column, so the expected text of no column is empty.
            // Portions area
            // INTRA_TABLE_REFERENCE : KEYWORD
            yield return new object?[] { "[#All]", StructuredReferenceArea.All, "", "" };
            yield return new object?[] { "[#Data]", StructuredReferenceArea.Data, "", "" };
            yield return new object?[] { "[#Headers]", StructuredReferenceArea.Headers, "", "" };
            yield return new object?[] { "[#Totals]", StructuredReferenceArea.Totals, "", "" };
            yield return new object?[] { "[#This Row]", StructuredReferenceArea.ThisRow, "", "" };

            // Empty simple column, per grammar, the SIMPLE_COLUMN_NAME is optional
            // INTRA_TABLE_REFERENCE : '[' SIMPLE_COLUMN_NAME? ']' 
            yield return new object?[] { "[]", StructuredReferenceArea.None, "", "" };

            // Simple column
            // INTRA_TABLE_REFERENCE : '[' SIMPLE_COLUMN_NAME? ']' 
            yield return new object?[] { "[Col]", StructuredReferenceArea.None, "Col", "" };
            yield return new object?[] { "[Name with space]", StructuredReferenceArea.None, "Name with space", "" };

            // Escaped characters
            // INTRA_TABLE_REFERENCE : '[' SIMPLE_COLUMN_NAME? ']'
            // where column name is a possible value of a ESCAPE_COLUMN_CHARACTER
            yield return new object?[] { "['[']]", StructuredReferenceArea.None, "[]", "" };
            yield return new object?[] { "['''#]", StructuredReferenceArea.None, "'#", "" };
            yield return new object?[] { "['[']'''#]", StructuredReferenceArea.None, "[]'#", "" };

            // INTRA_TABLE_REFERENCE : SPACED_LBRACKET INNER_REFERENCE SPACED_RBRACKET
            // where inner reference is `COLUMN_RANGE : COLUMN(':' COLUMN)?`
            yield return new object?[] { "[[First]]", StructuredReferenceArea.None, "First", "" };
            yield return new object?[] { "[[First]:[Last]]", StructuredReferenceArea.None, "First", "Last" };
            yield return new object?[] { "[[First]:Last]", StructuredReferenceArea.None, "First", "Last" };
            yield return new object?[] { "[First:[Last]]", StructuredReferenceArea.None, "First", "Last" };
            yield return new object?[] { "[First:Last]", StructuredReferenceArea.None, "First", "Last" };

            // fragment INNER_REFERENCE : KEYWORD_LIST SPACED_COMMA COLUMN_RANGE
            // where KEYWORD_LIST is just a KEYWORD
            yield return new object?[] { "[[#All],[First]]", StructuredReferenceArea.All, "First", "" };
            yield return new object?[] { "[[#Data],[First]:[Last]]", StructuredReferenceArea.Data, "First", "Last" };
            yield return new object?[] { "[[#Headers],[First]:Last]", StructuredReferenceArea.Headers, "First", "Last" };
            yield return new object?[] { "[[#Totals],First:[Last]]", StructuredReferenceArea.Totals, "First", "Last" };
            yield return new object?[] { "[[#This Row],First:Last]", StructuredReferenceArea.ThisRow, "First", "Last" };

            // fragment INNER_REFERENCE : KEYWORD_LIST SPACED_COMMA COLUMN_RANGE
            // where KEYWORD_LIST | '[#Headers]' SPACED_COMMA '[#Data]' | '[#Data]' SPACED_COMMA '[#Totals]'
            yield return new object?[] { "[[#Headers],[#Data],[Col]]", StructuredReferenceArea.Headers | StructuredReferenceArea.Data, "Col", "" };
            yield return new object?[] { "[[#Headers],[#Data],[First col]:[Last col]]", StructuredReferenceArea.Headers | StructuredReferenceArea.Data, "First col", "Last col"};
            yield return new object?[] { "[[#Headers],[#Data],First:Last]", StructuredReferenceArea.Headers | StructuredReferenceArea.Data, "First", "Last" };
            yield return new object?[] { "[[#Headers],[#Data],[First]:Last]", StructuredReferenceArea.Headers | StructuredReferenceArea.Data, "First", "Last" };
            yield return new object?[] { "[[#Headers],[#Data],First:[Last]]", StructuredReferenceArea.Headers | StructuredReferenceArea.Data, "First", "Last" };

            // spaces are ignored
            yield return new object?[] { "[  [#Headers]  ,  [#Data]  ,  [First col]:[Last col]  ]", StructuredReferenceArea.Headers | StructuredReferenceArea.Data, "First col", "Last col" };
        }
    }
}