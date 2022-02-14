namespace Classeur.Core;

/// <remarks>
/// There is also <see cref="IRepositoryFactory{T,TKey}"/> and if <see cref="IRepository{T,TKey}"/> is registered in
/// DI-container as opened generic then some generic parameters may become invalid because factory should be used for
/// them
/// </remarks>
public interface IRepository<T, TKey>
{
    Task<T> InsertAsync(T entity, CancellationToken token);

    Task<T> GetAsync(TKey key, CancellationToken token);

    Task<List<T>> GetAllAsync(CancellationToken token);

    Task<T> UpdateAsync(T entity, CancellationToken token);

    Task DeleteAsync(TKey key, CancellationToken token);
}