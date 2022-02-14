using System.Text.Json;
using Classeur.Core;
using Microsoft.JSInterop;

namespace SwissKnife.Serverless.Services;

public class LocalStorageRepository<T, TKey> : IRepository<T, TKey>
    where T : IEntity<TKey>
    where TKey : IEquatable<TKey>
{
    private readonly IJSRuntime _js;
    private readonly string _collectionId;
    private readonly JsonSerializerOptions _options;

    public LocalStorageRepository(IJSRuntime js, string collectionId, JsonSerializerOptions options)
    {
        _js = js;
        _collectionId = collectionId;
        _options = options;
    }

    public async Task<T> InsertAsync(T entity, CancellationToken token)
    {
        List<T> entities = await GetAllAsync(token);

        if (entities.Any(e => e.Id.Equals(entity.Id)))
        {
            throw new Exception();
        }

        entities.Add(entity);

        await SaveAsync(entities, token);

        return entity;
    }

    public async Task<T> GetAsync(TKey key, CancellationToken token)
    {
        List<T> entities = await GetAllAsync(token);

        return entities.First(e => e.Id.Equals(key));
    }

    public async Task<List<T>> GetAllAsync(CancellationToken token)
    {
        string rawCollection = await _js.LocalStoragegetItemAsync(_collectionId, token) ?? "[]";

        return JsonSerializer.Deserialize<List<T>>(rawCollection, _options)!;
    }

    public async Task<T> UpdateAsync(T entity, CancellationToken token)
    {
        List<T> entities = await GetAllAsync(token);

        entities[entities.FindIndex(e => e.Id.Equals(entity.Id))] = entity;

        await SaveAsync(entities, token);

        return entity;
    }

    public async Task DeleteAsync(TKey key, CancellationToken token)
    {
        List<T> entities = await GetAllAsync(token);

        entities.RemoveAt(entities.FindIndex(e => e.Id.Equals(key)));

        await SaveAsync(entities, token);
    }

    private async Task SaveAsync(List<T> entities, CancellationToken token)
    {
        string json = JsonSerializer.Serialize(entities, _options);

        await _js.LocalStoragesetItemAsync(_collectionId, json, token);
    }
}