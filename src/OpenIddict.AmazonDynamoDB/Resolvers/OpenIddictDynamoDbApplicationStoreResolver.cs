using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;

namespace OpenIddict.AmazonDynamoDB;

public class OpenIddictDynamoDbApplicationStoreResolver : IOpenIddictApplicationStoreResolver
{
  private readonly ConcurrentDictionary<Type, Type> _cache = new ConcurrentDictionary<Type, Type>();
  private readonly IServiceProvider _provider;

  public OpenIddictDynamoDbApplicationStoreResolver(IServiceProvider provider)
    => _provider = provider ?? throw new ArgumentNullException(nameof(provider));

  public IOpenIddictApplicationStore<TApplication> Get<TApplication>() where TApplication : class
  {
    var store = _provider.GetService<IOpenIddictApplicationStore<TApplication>>();
    if (store is not null)
    {
      return store;
    }

    var type = _cache.GetOrAdd(typeof(TApplication), key =>
    {
      if (!typeof(OpenIddictDynamoDbApplication).IsAssignableFrom(key))
      {
        throw new InvalidOperationException(OpenIddictResources.GetResourceString(OpenIddictResources.ID0257));
      }

      return typeof(OpenIddictDynamoDbApplicationStore<>).MakeGenericType(key);
    });

    return (IOpenIddictApplicationStore<TApplication>)_provider.GetRequiredService(type);
  }
}
