using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClosedXML.Parser.Function;

internal class AstNodeConverter : JsonConverter<AstNode>
{
    private readonly ReferenceStyle _style;

    internal AstNodeConverter(ReferenceStyle style)
    {
        _style = style;
    }

    public override AstNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException("Deserialization of AST is not supported.");
    }

    public override void Write(Utf8JsonWriter writer, AstNode value, JsonSerializerOptions options)
    {
        WriteNode(writer, value);
    }

    private void WriteNode(Utf8JsonWriter writer, AstNode value)
    {
        writer.WriteStartObject();
        writer.WriteString("type", value.GetTypeString());
        writer.WriteString("content", value.GetDisplayString(_style));

        writer.WriteStartArray("children");
        foreach (var child in value.Children)
            WriteNode(writer, child);

        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}
