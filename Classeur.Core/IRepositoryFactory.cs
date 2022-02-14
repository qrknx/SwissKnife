using Classeur.Core.CustomizableStructure;

namespace Classeur.Core;

/// <remarks>
/// See docs for <see cref="IRepository{T,TKey}"/>
/// </remarks>
public interface IRepositoryFactory<T, TKey>
{
    IRepository<T, TKey> GetRepository(StructureSchema schema);

    IRepository<T, TKey> GetRepository(IEnumerable<StructureSchema> schemas);
}
