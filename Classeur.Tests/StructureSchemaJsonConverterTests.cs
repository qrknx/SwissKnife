using System;
using System.Collections.Immutable;
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
            new StructureSchemaJsonConverter(ImmutableDictionary<string, Type>.Empty
                                                                              .Add("String", typeof(StringFieldType))
                                                                              .Add("Int64", typeof(Int64FieldType))),
            IncoherentIdJsonConverter.Instance,
            FieldKeyJsonConverter.Instance,
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

        string json = reader.ReadToEnd();

        Assert.Equal(expectedJson, actual: json);

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
                     Id = Id.UnderlyingValue,
                     Changes = Array.Empty<object>(),
                 }));

            Add((new StructureSchema.Change[]
                 {
                     new StructureSchema.FieldAdded(new(new("f1"), "F1", new StringFieldType(10, "abc")),
                         Version: 1),
                     new StructureSchema.FieldAdded(new(new("f2"), "F2", new Int64FieldType(-100, 200, 100)),
                                                    Version: 1),
                     new StructureSchema.FieldRemoved(new("f1"), Version: 2),
                 },
                 new
                 {
                     Id = Id.UnderlyingValue,
                     Changes = new object[]
                     {
                         new
                         {
                             ChangeType = "FieldAdded",
                             Version = 1,
                             Key = "f1",
                             Label = "F1",
                             TypeId = "String",
                             Type = new
                             {
                                 MaxLength = 10,
                                 Default = "abc",
                             },
                         },
                         new
                         {
                             ChangeType = "FieldAdded",
                             Version = 1,
                             Key = "f2",
                             Label = "F2",
                             TypeId = "Int64",
                             Type = new
                             {
                                 Min = -100,
                                 Max = 200,
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
