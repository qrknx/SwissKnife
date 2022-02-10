namespace Classeur.Core.CustomizableStructure;

public interface ICustomizableDocument
{
    public T GetValue<T>(FieldKey key, StructureSchema schema);

    public IEnumerable<FieldKey> GetKeys(StructureSchema schema);
}
