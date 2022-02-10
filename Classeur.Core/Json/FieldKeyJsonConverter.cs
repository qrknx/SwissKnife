using System.Text.Json;
using System.Text.Json.Serialization;
using Classeur.Core.CustomizableStructure;

namespace Classeur.Core.Json;

public class FieldKeyJsonConverter : JsonConverter<FieldKey>
{
    public static readonly FieldKeyJsonConverter Instance = new();

    protected FieldKeyJsonConverter() {}

    public override FieldKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new FieldKey(reader.GetString() ?? throw new JsonException());
    }

    public override void Write(Utf8JsonWriter writer, FieldKey value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Name);
    }
}
