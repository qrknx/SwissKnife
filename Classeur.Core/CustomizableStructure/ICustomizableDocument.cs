namespace Classeur.Core.CustomizableStructure;

public interface ICustomizableDocument
{
    public object Get(FieldKey key, StructureSchema schema);

    public ICustomizableDocument Set(FieldKey key, object value, StructureSchema schema);
}
