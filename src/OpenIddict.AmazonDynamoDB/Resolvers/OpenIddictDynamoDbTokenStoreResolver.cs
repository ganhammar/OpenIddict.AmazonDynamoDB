using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;

namespace OpenIddict.AmazonDynamoDB;

public class OpenIddictDynamoDbTokenStoreResolver(IServiceProvider provider) : IOpenIddictTokenStoreResolver
{
  private readonly ConcurrentDictionary<Type, Type> _cache = new();
  private readonly IServiceProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));

  public IOpenIddictTokenStore<TToken> Get<TToken>() where TToken : class
  {
    var store = _provider.GetService<IOpenIddictTokenStore<TToken>>();
    if (store is not null)
    {
      return store;
    }

    var type = _cache.GetOrAdd(typeof(TToken), key =>
    {
      if (!typeof(OpenIddictDynamoDbToken).IsAssignableFrom(key))
      {
        throw new InvalidOperationException(OpenIddictResources.GetResourceString(OpenIddictResources.ID0260));
      }

      return typeof(OpenIddictDynamoDbTokenStore<>).MakeGenericType(key);
    });

    return (IOpenIddictTokenStore<TToken>)_provider.GetRequiredService(type);
  }
}
