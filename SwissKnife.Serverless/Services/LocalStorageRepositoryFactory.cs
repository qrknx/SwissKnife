using System.Text.Json;
using Classeur.Core;
using Classeur.Core.CustomizableStructure;
using Classeur.Core.Json;
using Microsoft.JSInterop;

namespace SwissKnife.Serverless.Services;

public class LocalStorageRepositoryFactory<T, TKey> : IRepositoryFactory<T, TKey>
    where T : IEntity<TKey>
    where TKey : IEquatable<TKey>
{
    private readonly IJSRuntime _js;
    private readonly string _collectionId;
    private readonly JsonSerializerOptions _defaultOptions;

    public LocalStorageRepositoryFactory(IJSRuntime js, string collectionId, JsonSerializerOptions defaultOptions)
    {
        _js = js;
        _collectionId = collectionId;
        _defaultOptions = defaultOptions;
    }

    public IRepository<T, TKey> GetRepository(StructureSchema schema)
    {
        return new LocalStorageRepository<T, TKey>(_js, _collectionId, new JsonSerializerOptions(_defaultOptions)
        {
            Converters =
            {
                new StructuredDataJsonConverter(new[] { schema }),
            },
        });
    }
}
