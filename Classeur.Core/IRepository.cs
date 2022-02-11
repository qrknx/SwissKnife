namespace Classeur.Core;

public interface IRepository<T, TKey>
{
    Task<T> InsertAsync(T entity, CancellationToken token);

    Task<T> GetAsync(TKey key, CancellationToken token);

    Task<List<T>> GetAllAsync(CancellationToken token);

    Task<T> UpdateAsync(T entity, CancellationToken token);

    Task DeleteAsync(TKey key, CancellationToken token);
}
