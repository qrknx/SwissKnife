using System;
using System.Collections.Immutable;
using System.Text.Json;
using Classeur.Core;
using Classeur.Core.CustomizableStructure;
using Classeur.Core.Json;
using Xunit;

namespace Classeur.Tests;

public partial class StructureSchemaJsonConverterTests
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

            Add((new[]
                 {
                     StructureSchema.Change.FieldAdded(new(new("f1"), "F1", new StringFieldType(10, "abc")), 1),
                     StructureSchema.Change.FieldAdded(new(new("f2"), "F2", new Int64FieldType(-100, 200, 100)), 1),
                     StructureSchema.Change.FieldMoved(new("f2"), 0, 2),
                     StructureSchema.Change.FieldRemoved(new("f1"), 3),
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
                             ChangeType = "FieldMoved",
                             Version = 2,
                             Key = "f2",
                             Position = 0,
                         },
                         new
                         {
                             ChangeType = "FieldRemoved",
                             Version = 3,
                             Key = "f1",
                         },
                     },
                 }));
        }
    }
}
