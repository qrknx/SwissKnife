using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Classeur.Core.CustomizableStructure;

namespace Classeur.Core.Json;

public class StructuredDataJsonConverter : JsonConverter<StructuredData>
{
    private const string DataFieldName = "Data";

    private readonly ImmutableDictionary<IncoherentId, StructureSchema> _schemas;

    public StructuredDataJsonConverter(IEnumerable<StructureSchema> schemas)
    {
        _schemas = schemas.ToImmutableDictionary(s => s.Id);
    }

    public override StructuredData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.MoveIfEquals(JsonTokenType.StartObject);
        reader.MoveIfEquals(nameof(StructuredData.SchemaId));

        if (!_schemas.TryGetValue(new IncoherentId(reader.GetInt64()), out StructureSchema? schema))
        {
            throw new JsonException();
        }

        reader.Read();

        reader.MoveIfEquals(nameof(StructuredData.VersionIndex));

        StructureSchemaVersion version = schema.GetVersion(reader.GetInt32());

        StructuredData structuredData = StructuredData.CreateDefault(version);

        reader.Read();

        reader.MoveIfEquals(DataFieldName);
        reader.MoveIfEquals(JsonTokenType.StartObject);

        while (reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }

            FieldKey key = new(reader.GetString() ?? throw new JsonException());

            if (!version.TryGetField(key, out _, out FieldDescription field))
            {
                throw new JsonException();
            }

            reader.Read();

            structuredData = structuredData.Set(key,
                                                JsonSerializer.Deserialize(ref reader,
                                                                           field.Type.UnderlyingType,
                                                                           options) ?? throw new JsonException(),
                                                version);

            reader.Read();
        }

        return reader.Read() && reader.TokenType == JsonTokenType.EndObject
            ? structuredData
            : throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, StructuredData value, JsonSerializerOptions options)
    {
        StructureSchemaVersion version = _schemas[value.SchemaId].GetVersion(value.VersionIndex);

        writer.WriteStartObject();
        writer.WritePropertyName(nameof(StructuredData.SchemaId));
        JsonSerializer.Serialize(writer, value.SchemaId, options);
        writer.WriteNumber(nameof(StructuredData.VersionIndex), value.VersionIndex);
        writer.WriteStartObject(DataFieldName);

        foreach (FieldDescription field in version.UnorderedFields)
        {
            JsonSerializer.Serialize(writer, value.Get<object>(field.Key, version), field.Type.UnderlyingType, options);
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
    }
}
