using System;
using System.IO;
using System.Text.Json;
using Classeur.Core;
using Classeur.Core.CustomizableStructure;
using Classeur.Core.Json;
using Xunit;

namespace Classeur.Tests;

public class StructureSchemaJsonConverterTests
{
    private static readonly IncoherentId Id = new(123);
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters =
        {
            StructureSchemaJsonConverter.Instance,
            IncoherentIdJsonConverter.Instance,
            FieldKeyJsonConverter.Instance,
            FieldTypeJsonConverter.Instance,
        },
    };

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

        Assert.Equal(expectedJson, actual: reader.ReadToEnd());

        stream.Position = 0;

        StructureSchema deserializedSchema = JsonSerializer.Deserialize<StructureSchema>(stream, Options)!;

        Assert.Equal(expected: Id, deserializedSchema.Id);
        Assert.Equal(expected: @case.Changes, deserializedSchema.Changes);
    }

    private class TestCases : TheoryData<(StructureSchema.Change[] Changes, object NonSerializedJson)>
    {
        public TestCases()
        {
            Add((Array.Empty<StructureSchema.Change>(),
                 new
                 {
                     Id = 123,
                     Changes = Array.Empty<object>(),
                 }));

            Add((new StructureSchema.Change[]
                 {
                     new StructureSchema.FieldAdded(new(new("f1"), FieldType.String("abc", new FieldType.ConstraintsString(10))),
                         Version: 1),
                     new StructureSchema.FieldAdded(new(new("f2"), FieldType.Int64(100, new FieldType.ConstraintsInt64(-100, 200))),
                                                    Version: 1),
                     new StructureSchema.FieldRemoved(new("f1"), Version: 2),
                 },
                 new
                 {
                     Id = 123,
                     Changes = new object[]
                     {
                         new
                         {
                             ChangeType = "FieldAdded",
                             Version = 1,
                             Key = "f1",
                             Type = new
                             {
                                 Id = 1,
                                 Constraints = new
                                 {
                                     MaxLength = 10,
                                 },
                                 Default = "abc",
                             },
                         },
                         new
                         {
                             ChangeType = "FieldAdded",
                             Version = 1,
                             Key = "f2",
                             Type = new
                             {
                                 Id = 2,
                                 Constraints = new
                                 {
                                     Min = -100,
                                     Max = 200,
                                 },
                                 Default = 100,
                             },
                         },
                         new
                         {
                             ChangeType = "FieldRemoved",
                             Version = 2,
                             Key = "f1",
                         },
                     },
                 }));
        }
    }
}
