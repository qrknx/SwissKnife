using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Classeur.Core.CustomizableStructure;

namespace Classeur.Core.Json;

using static StructureSchema.Change;

public class StructureSchemaJsonConverter : JsonConverter<StructureSchema>
{
    private const string ChangeTypeFieldName = "ChangeType";
    private const string FieldTypeIdFieldName = "TypeId";
    private const string FieldTypeFieldName = "Type";

    private readonly ImmutableDictionary<string, Type> _typeById;
    private readonly ImmutableDictionary<Type, string> _idByType;

    public StructureSchemaJsonConverter(ImmutableDictionary<string, Type> typeById)
    {
        _typeById = typeById;
        _idByType = _typeById.ToImmutableDictionary(x => x.Value, x => x.Key);
    }

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

            StructureSchema.Change change;
            int version;

            switch (reader.GetString())
            {
                case nameof(FieldAdded):
                    version = ReadVersion(ref reader);
                    FieldKey keyAdded = reader.DeserializeProperty<FieldKey>(nameof(FieldDescription.Key), options);
                    reader.MoveIfEquals(nameof(FieldDescription.Label));
                    string label = reader.GetString() ?? throw new JsonException();
                    reader.Read();
                    reader.MoveIfEquals(FieldTypeIdFieldName);
                    Type type = _typeById[reader.GetString() ?? throw new JsonException()];
                    reader.Read();
                    var typeAdded = (AbstractFieldType?)reader.DeserializeProperty(FieldTypeFieldName, type, options)
                                    ?? throw new JsonException();

                    change = FieldAdded(new FieldDescription(keyAdded, label, typeAdded), version);
                    break;

                case nameof(FieldRemoved):
                    version = ReadVersion(ref reader);
                    FieldKey keyRemoved
                        = reader.DeserializeProperty<FieldKey>(nameof(StructureSchema.Change.Key), options);

                    change = FieldRemoved(keyRemoved, version);
                    break;

                case nameof(FieldMoved):
                    version = ReadVersion(ref reader);
                    FieldKey keyMoved
                        = reader.DeserializeProperty<FieldKey>(nameof(StructureSchema.Change.Key), options);
                    reader.MoveIfEquals(nameof(StructureSchema.Change.Position));
                    int position = reader.GetInt32();
                    reader.Read();

                    change = FieldMoved(keyMoved, position, version);
                    break;

                default:
                    throw new JsonException();
            }

            changes.Add(change);

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

        foreach (StructureSchema.Change change in value.Changes.Where(c => !c.IsNop))
        {
            writer.WriteStartObject();
            
            switch (change)
            {
                case {IsAdded: true, Field: {Key: var key, Label: var label, Type: var type}}:
                    writer.WriteString(ChangeTypeFieldName, nameof(FieldAdded));
                    writer.WriteNumber(nameof(StructureSchema.Change.Version), change.Version);
                    writer.WritePropertyName(nameof(FieldDescription.Key));
                    JsonSerializer.Serialize(writer, key, options);
                    writer.WriteString(nameof(FieldDescription.Label), label);
                    Type runtimeType = type.GetType();
                    writer.WriteString(FieldTypeIdFieldName, _idByType[runtimeType]);
                    writer.WritePropertyName(FieldTypeFieldName);
                    JsonSerializer.Serialize(writer, type, runtimeType, options);
                    break;

                case {IsRemoved: true, Key: var key}:
                    writer.WriteString(ChangeTypeFieldName, nameof(FieldRemoved));
                    writer.WriteNumber(nameof(StructureSchema.Change.Version), change.Version);
                    writer.WritePropertyName(nameof(StructureSchema.Change.Key));
                    JsonSerializer.Serialize(writer, key, options);
                    break;

                case { IsMoved: true, Key: var key, Position: var position }:
                    writer.WriteString(ChangeTypeFieldName, nameof(FieldMoved));
                    writer.WriteNumber(nameof(StructureSchema.Change.Version), change.Version);
                    writer.WritePropertyName(nameof(StructureSchema.Change.Key));
                    JsonSerializer.Serialize(writer, key, options);
                    writer.WriteNumber(nameof(StructureSchema.Change.Position), position);
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

        int version = reader.GetInt32();

        reader.Read();

        return version;
    }
}
