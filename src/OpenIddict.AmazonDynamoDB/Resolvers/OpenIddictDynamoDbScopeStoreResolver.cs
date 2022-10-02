using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;

namespace OpenIddict.AmazonDynamoDB;

public class OpenIddictDynamoDbScopeStoreResolver : IOpenIddictScopeStoreResolver
{
    private readonly ConcurrentDictionary<Type, Type> _cache = new ConcurrentDictionary<Type, Type>();
    private readonly IServiceProvider _provider;

    public OpenIddictDynamoDbScopeStoreResolver(IServiceProvider provider)
        => _provider = provider ?? throw new ArgumentNullException(nameof(provider));

    public IOpenIddictScopeStore<TScope> Get<TScope>() where TScope : class
    {
        var store = _provider.GetService<IOpenIddictScopeStore<TScope>>();
        if (store is not null)
        {
            return store;
        }

        var type = _cache.GetOrAdd(typeof(TScope), key =>
        {
            if (!typeof(OpenIddictDynamoDbScope).IsAssignableFrom(key))
            {
                throw new InvalidOperationException(OpenIddictResources.GetResourceString(OpenIddictResources.ID0259));
            }

            return typeof(IOpenIddictScopeStore<>).MakeGenericType(key);
        });

        return (IOpenIddictScopeStore<TScope>) _provider.GetRequiredService(type);
    }
}