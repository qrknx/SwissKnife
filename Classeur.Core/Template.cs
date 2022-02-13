using Classeur.Core.CustomizableStructure;

namespace Classeur.Core;

public record Template(string Name, StructureSchema Schema) : IEntity<IncoherentId>,
                                                              IEntity<string>
{
    public IncoherentId Id => Schema.Id;

    string IEntity<string>.Id => Schema.Id.ToString();
}
