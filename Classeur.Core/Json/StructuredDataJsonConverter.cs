using System.Text.Json;
using System.Text.Json.Serialization;
using Classeur.Core.CustomizableStructure;

namespace Classeur.Core.Json;

public class StructuredDataJsonConverter : JsonConverter<StructuredData>
{
    private const string SchemaIdFieldName = "SchemaId";
    private const string DataFieldName = "Data";

    private readonly StructureSchema _schema;

    public StructuredDataJsonConverter(StructureSchema schema) => _schema = schema;

    public override StructuredData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.MoveIfEquals(JsonTokenType.StartObject);
        reader.MoveIfEquals(SchemaIdFieldName);

        IncoherentId id = new(reader.GetInt64());

        if (_schema.Id != id)
        {
            throw new JsonException();
        }

        reader.Read();

        reader.MoveIfEquals(nameof(StructuredData.Version));

        StructuredData structuredData = StructuredData.CreateDefault(_schema, reader.GetInt32());

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

            if (!structuredData.Version.TryGetField(key, out FieldDescription field))
            {
                throw new JsonException();
            }

            reader.Read();

            structuredData = field.Type.Id switch
            {
                FieldTypeId.String => structuredData.Set(key, reader.GetString() ?? throw new JsonException()),
                FieldTypeId.Int64 => structuredData.Set(key,
                                                        reader.TryGetInt64(out long int64)
                                                            ? int64
                                                            : throw new JsonException()),
                _ => throw new NotImplementedException(),
            };

            reader.Read();
        }

        return reader.Read() && reader.TokenType == JsonTokenType.EndObject
            ? structuredData
            : throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, StructuredData value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName(SchemaIdFieldName);
        JsonSerializer.Serialize(writer, value.Schema.Id, options);
        writer.WriteNumber(nameof(StructuredData.Version), value.Version.Version);
        writer.WriteStartObject(DataFieldName);

        foreach (FieldDescription field in value.Version.Fields)
        {
            FieldKey key = field.Key;

            switch (field.Type.Id)
            {
                case FieldTypeId.String:
                    writer.WriteString(key.Name, value.Get<string>(key));
                    break;

                case FieldTypeId.Int64:
                    writer.WriteNumber(key.Name, value.Get<long>(key));
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
    }
}
