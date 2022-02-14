using System;
using System.IO;
using System.Text;
using System.Text.Json;
using Classeur.Core.CustomizableStructure;
using Xunit;

namespace Classeur.Tests;

public partial class StructureSchemaJsonConverterTests
{
    [Theory]
    [ClassData(typeof(TestCases))]
    public void SerializeAndDeserialize((StructureSchema.Change[] Changes, object NonSerializedJson) @case)
    {
        string expectedJson = JsonSerializer.Serialize(@case.NonSerializedJson);

        StructureSchema schema = new(Id, @case.Changes);

        using MemoryStream stream = new();

        JsonSerializer.Serialize(stream, schema, Options);

        stream.Position = 0;

        using StreamReader reader = new(stream);

        string json = reader.ReadToEnd();

        Assert.Equal(expectedJson, actual: json);

        stream.Position = 0;

        StructureSchema deserializedSchema = JsonSerializer.Deserialize<StructureSchema>(stream, Options)!;

        Assert.Equal(expected: Id, deserializedSchema.Id);
        Assert.Equal(expected: @case.Changes, deserializedSchema.InternalChangesForSerialization);
    }

    [Fact(Skip = "External code")]
    public void Deserialize_BuiltIn()
    {
        string validIntString = int.MaxValue.ToString();
        string invalidIntString = $"1{int.MaxValue}";

        byte[] validBytes = GetBytes(validIntString);

        Utf8JsonReader reader = new(validBytes);

        reader.Read();
        
        Assert.Equal(expected: int.MaxValue, reader.GetInt32());
        Assert.Equal(expected: int.MaxValue, JsonSerializer.Deserialize<int>(validIntString));

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int>(invalidIntString));
        Assert.Throws<FormatException>(() =>
        {
            byte[] bytes = GetBytes(invalidIntString);

            Utf8JsonReader reader = new(bytes);

            reader.Read();
            reader.GetInt32();
        });

        static byte[] GetBytes(string s) => Encoding.UTF8.GetBytes(s);
    }
}
