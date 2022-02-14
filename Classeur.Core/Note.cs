using Classeur.Core.CustomizableStructure;

namespace Classeur.Core;

public record Note(IncoherentId Id, StructuredData Data) : IEntity<IncoherentId>,
                                                           IEntity<string>,
                                                           ICustomizableDocument<Note>
{
    string IEntity<string>.Id => Id.ToString();

    object ICustomizableDocument<Note>.Get(FieldKey key, StructureSchemaVersion version)
        => Data.Get<object>(key, version);

    Note ICustomizableDocument<Note>.Set(FieldKey key, object value, StructureSchemaVersion version) => this with
    {
        Data = Data.Set(key, value, version),
    };
}
