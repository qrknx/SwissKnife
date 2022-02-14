namespace Classeur.Core.CustomizableStructure;

public interface IStructuredDataView
{
    public object Get(FieldKey key, StructureSchemaVersion version);
}
