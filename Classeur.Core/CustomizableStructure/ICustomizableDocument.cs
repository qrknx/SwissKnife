namespace Classeur.Core.CustomizableStructure;

public interface ICustomizableDocument<T> : IStructuredDataView
{
    public T Set(FieldKey key, object value, StructureSchemaVersion version);
}
