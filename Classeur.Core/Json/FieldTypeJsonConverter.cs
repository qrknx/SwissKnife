using System.Text.Json;
using System.Text.Json.Serialization;
using Classeur.Core.CustomizableStructure;

namespace Classeur.Core.Json;

public class FieldTypeJsonConverter : JsonConverter<FieldType>
{
    private const string ConstraintsFieldName = "Constraints";
    private const string DefaultFieldName = "Default";

    public static readonly FieldTypeJsonConverter Instance = new();

    protected FieldTypeJsonConverter() {}

    public override FieldType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.MoveIfEquals(JsonTokenType.StartObject);

        FieldTypeId id = reader.DeserializeProperty<FieldTypeId>(nameof(FieldType.Id), options);

        reader.MoveIfEquals(ConstraintsFieldName);

        FieldType type;

        switch (id)
        {
            case FieldTypeId.String:
                var constraintsString = JsonSerializer.Deserialize<FieldType.ConstraintsString>(ref reader, options);

                reader.Read();

                reader.MoveIfEquals(DefaultFieldName);

                type = FieldType.String(reader.GetString() ?? throw new JsonException(),
                                        constraintsString);
                break;

            case FieldTypeId.Int64:
                var constraintsInt64 = JsonSerializer.Deserialize<FieldType.ConstraintsInt64>(ref reader, options);

                reader.Read();

                reader.MoveIfEquals(DefaultFieldName);

                type = FieldType.Int64(reader.TryGetInt64(out long int64)
                                           ? int64
                                           : throw new JsonException(),
                                       constraintsInt64);
                break;

            default:
                throw new JsonException();
        }

        reader.Read();

        return reader.TokenType == JsonTokenType.EndObject
            ? type
            : throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, FieldType value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(nameof(FieldType.Id));

        JsonSerializer.Serialize(writer, value.Id, options);

        writer.WritePropertyName(ConstraintsFieldName);

        switch (value.Id)
        {
            case FieldTypeId.String:
                JsonSerializer.Serialize(writer, value.StringConstraints, options);
                writer.WriteString(DefaultFieldName, value.DefaultString);
                break;

            case FieldTypeId.Int64:
                JsonSerializer.Serialize(writer, value.Int64Constraints, options);
                writer.WriteNumber(DefaultFieldName, value.DefaultInt64);
                break;

            default:
                throw new NotImplementedException();
        }

        writer.WriteEndObject();
    }
}
