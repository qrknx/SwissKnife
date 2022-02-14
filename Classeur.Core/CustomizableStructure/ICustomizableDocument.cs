namespace Classeur.Core.CustomizableStructure;

public interface ICustomizableDocument<T>
{
    public object Get(FieldKey key, StructureSchemaVersion version);

    public T Set(FieldKey key, object value, StructureSchemaVersion version);
}
