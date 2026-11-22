using System.Text.Json;
using ClosedXML.Parser.Function;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

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

    [Fact]
    [Trait("Issue", "4")]
    public async Task Result_executor_writes_ast_as_json_to_response()
    {
        // The isolated worker host writes the JsonResult with the result executor of ASP.NET Core MVC.
        var services = new ServiceCollection().AddLogging().AddMvcCore().Services.BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.QueryString = new QueryString("?formula=" + Uri.EscapeDataString(string.Join("+", Enumerable.Repeat("A1", 100))));
        context.Response.Body = new MemoryStream();

        var result = ParseFormula.Run(context.Request, CancellationToken.None);
        await result.ExecuteResultAsync(new ActionContext(context, new RouteData(), new ActionDescriptor()));

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.StartsWith("application/json", context.Response.ContentType);
        context.Response.Body.Position = 0;
        using var json = await JsonDocument.ParseAsync(context.Response.Body, new JsonDocumentOptions { MaxDepth = 256 }, CancellationToken.None);
        Assert.Equal(99, CountNodes(json.RootElement.GetProperty("ast"), "+"));
    }

    [Fact]
    [Trait("Issue", "23")]
    public void Requests_with_same_style_and_similar_depth_use_same_serializer_options()
    {
        // Serializer options cache the type metadata, thus a new instance for each request is slow.
        var a1 = Run("?formula=A1").SerializerSettings;
        var sum = Run("?formula=SUM(A1,1)").SerializerSettings;
        var error = Run("?formula=SUM(").SerializerSettings;
        var r1c1 = Run("?formula=R1C1&style=R1C1").SerializerSettings;
        var deep = (JsonSerializerOptions)Run("?formula=" + Uri.EscapeDataString(string.Join("+", Enumerable.Repeat("A1", 100)))).SerializerSettings!;

        Assert.Same(a1, sum);
        Assert.Same(a1, error);
        Assert.NotSame(a1, r1c1);
        Assert.NotSame(a1, deep);
        Assert.Equal(64, ((JsonSerializerOptions)a1!).MaxDepth);
        Assert.Equal(256, deep.MaxDepth);
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
