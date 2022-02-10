using System.Text.Json;
using System.Text.Json.Serialization;

namespace Classeur.Core.Json;

public class IncoherentIdJsonConverter : JsonConverter<IncoherentId>
{
    public static readonly IncoherentIdJsonConverter Instance = new();

    protected IncoherentIdJsonConverter() {}

    public override IncoherentId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new IncoherentId(reader.TryGetInt64(out long i)
                                    ? i
                                    : throw new JsonException());
    }

    public override void Write(Utf8JsonWriter writer, IncoherentId value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.UnderlyingValue);
    }
}
