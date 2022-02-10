using System.Text.Json;
using System.Text.Json.Serialization;
using Classeur.Core.CustomizableStructure;

namespace Classeur.Core.Json;

public class StructureSchemaJsonConverter : JsonConverter<StructureSchema>
{
    private const string ChangeTypeFieldName = "ChangeType";

    public static readonly StructureSchemaJsonConverter Instance = new();

    protected StructureSchemaJsonConverter() {}

    public override StructureSchema Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.MoveIfEquals(JsonTokenType.StartObject);

        IncoherentId id = reader.DeserializeProperty<IncoherentId>(nameof(StructureSchema.Id), options);

        reader.MoveIfEquals(nameof(StructureSchema.Changes));
        reader.MoveIfEquals(JsonTokenType.StartArray);

        List<StructureSchema.Change> changes = new();

        while (reader.TokenType != JsonTokenType.EndArray)
        {
            reader.MoveIfEquals(JsonTokenType.StartObject);
            reader.MoveIfEquals(ChangeTypeFieldName);

            int version;

            switch (reader.GetString())
            {
                case nameof(StructureSchema.FieldAdded):
                    version = ReadVersion(ref reader);
                    reader.MoveIfEquals(nameof(FieldDescription.Key));
                    FieldKey keyAdded = JsonSerializer.Deserialize<FieldKey>(ref reader, options);
                    reader.Read();
                    reader.MoveIfEquals(nameof(FieldDescription.Type));
                    FieldType typeAdded = JsonSerializer.Deserialize<FieldType>(ref reader, options);
                    reader.Read();
                    changes.Add(new StructureSchema.FieldAdded(new FieldDescription(keyAdded, typeAdded), version));
                    break;

                case nameof(StructureSchema.FieldRemoved):
                    version = ReadVersion(ref reader);
                    reader.MoveIfEquals(nameof(StructureSchema.FieldRemoved.Key));
                    FieldKey keyRemoved = JsonSerializer.Deserialize<FieldKey>(ref reader, options);
                    reader.Read();
                    changes.Add(new StructureSchema.FieldRemoved(keyRemoved, version));
                    break;

                default:
                    throw new JsonException();
            }

            reader.MoveIfEquals(JsonTokenType.EndObject);
        }

        reader.Read();

        return reader.TokenType == JsonTokenType.EndObject
            ? new StructureSchema(id, changes)
            : throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, StructureSchema value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(nameof(StructureSchema.Id));
        JsonSerializer.Serialize(writer, value.Id, options);

        writer.WritePropertyName(nameof(StructureSchema.Changes));
        writer.WriteStartArray();

        foreach (StructureSchema.Change change in value.Changes)
        {
            writer.WriteStartObject();
            
            switch (change)
            {
                case StructureSchema.FieldAdded {Field: {Key: var key, Type: var type}}:
                    writer.WriteString(ChangeTypeFieldName, nameof(StructureSchema.FieldAdded));
                    writer.WriteNumber(nameof(StructureSchema.Change.Version), change.Version);
                    writer.WritePropertyName(nameof(FieldDescription.Key));
                    JsonSerializer.Serialize(writer, key, options);
                    writer.WritePropertyName(nameof(FieldDescription.Type));
                    JsonSerializer.Serialize(writer, type, options);
                    break;

                case StructureSchema.FieldRemoved {Key: var key}:
                    writer.WriteString(ChangeTypeFieldName, nameof(StructureSchema.FieldRemoved));
                    writer.WriteNumber(nameof(StructureSchema.Change.Version), change.Version);
                    writer.WritePropertyName(nameof(StructureSchema.FieldRemoved.Key));
                    JsonSerializer.Serialize(writer, key, options);
                    break;

                default:
                    throw new NotImplementedException();
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        writer.WriteEndObject();
    }

    private static int ReadVersion(ref Utf8JsonReader reader)
    {
        reader.Read();

        reader.MoveIfEquals(nameof(StructureSchema.Change.Version));

        if (!reader.TryGetInt32(out int version))
        {
            throw new JsonException();
        }

        reader.Read();

        return version;
    }
}
