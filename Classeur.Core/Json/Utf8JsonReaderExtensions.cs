using System.Text.Json;

namespace Classeur.Core.Json;

internal static class Utf8JsonReaderExtensions
{
    public static T? DeserializeProperty<T>(this ref Utf8JsonReader reader, string name, JsonSerializerOptions options)
    {
        reader.MoveIfEquals(name);

        T? value = JsonSerializer.Deserialize<T>(ref reader, options);

        reader.Read();

        return value;
    }

    public static bool MoveIfEquals(this ref Utf8JsonReader reader, string s) => reader.GetString() == s
        ? reader.Read()
        : throw new JsonException();

    public static bool MoveIfEquals(this ref Utf8JsonReader reader, JsonTokenType type) => reader.TokenType == type
        ? reader.Read()
        : throw new JsonException();
}
