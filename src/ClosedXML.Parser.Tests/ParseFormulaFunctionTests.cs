using System.Text.Json;
using ClosedXML.Parser.Function;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClosedXML.Parser.Tests;

/// <summary>
/// The AST visualizer calls the <c>parse-formula</c> Azure function and reads its JSON.
/// </summary>
public class ParseFormulaFunctionTests
{
    [Fact]
    [Trait("Issue", "4")]
    public void A1_formula_gives_ast_as_json()
    {
        var result = Run("?formula=SUM(A1,1)");

        Assert.Null(result.StatusCode);
        Assert.Equal(
            """{"formula":"SUM(A1,1)","style":"A1","ast":{"type":"Function","content":"SUM","children":[{"type":"Reference","content":"A1","children":[]},{"type":"Number","content":"1","children":[]}]}}""",
            ToJson(result, writeIndented: false));
    }

    [Fact]
    [Trait("Issue", "4")]
    public void R1C1_style_parses_formula_in_R1C1_notation()
    {
        var result = Run("?formula=R1C2&style=R1C1");

        Assert.Equal(
            """{"formula":"R1C2","style":"R1C1","ast":{"type":"Reference","content":"R1C2","children":[]}}""",
            ToJson(result, writeIndented: false));
    }

    [Fact]
    [Trait("Issue", "4")]
    public void Formula_that_is_not_valid_gives_error_with_status_422()
    {
        var result = Run("?formula=SUM(");

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, result.StatusCode);
        using var json = JsonDocument.Parse(ToJson(result, writeIndented: false));
        Assert.Equal("SUM(", json.RootElement.GetProperty("formula").GetString());
        Assert.False(string.IsNullOrEmpty(json.RootElement.GetProperty("error").GetString()));
    }

    [Fact]
    [Trait("Issue", "4")]
    public void Json_is_indented_for_the_visualizer()
    {
        var result = Run("?formula=A1");

        Assert.Contains(Environment.NewLine + "  \"formula\": \"A1\"", ToJson(result));
    }

    [Fact]
    [Trait("Issue", "4")]
    public void Json_of_ast_cannot_be_read_back()
    {
        var result = Run("?formula=A1");
        var options = (JsonSerializerOptions)result.SerializerSettings!;

        Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<AstNode>("{}", options));
    }

    [Fact]
    [Trait("Issue", "4")]
    public void Long_chain_of_binary_operators_gives_ast_deeper_than_default_json_depth()
    {
        var formula = string.Join("+", Enumerable.Repeat("A1", 100));

        var result = Run("?formula=" + Uri.EscapeDataString(formula));

        Assert.Null(result.StatusCode);
        using var json = JsonDocument.Parse(ToJson(result), new JsonDocumentOptions { MaxDepth = 256 });
        Assert.Equal(99, CountNodes(json.RootElement.GetProperty("ast"), "+"));
    }

    private static int CountNodes(JsonElement node, string content)
    {
        var count = node.GetProperty("content").GetString() == content ? 1 : 0;
        foreach (var child in node.GetProperty("children").EnumerateArray())
            count += CountNodes(child, content);

        return count;
    }

    private static JsonResult Run(string queryString)
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString(queryString);
        return Assert.IsType<JsonResult>(ParseFormula.Run(context.Request, CancellationToken.None));
    }

    private static string ToJson(JsonResult result, bool writeIndented = true)
    {
        var options = new JsonSerializerOptions((JsonSerializerOptions)result.SerializerSettings!)
        {
            WriteIndented = writeIndented
        };
        return JsonSerializer.Serialize(result.Value, options);
    }
}
