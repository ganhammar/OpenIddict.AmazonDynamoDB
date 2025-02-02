using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;

namespace OpenIddict.AmazonDynamoDB;

public class OpenIddictDynamoDbAuthorizationStoreResolver(IServiceProvider provider) : IOpenIddictAuthorizationStoreResolver
{
  private readonly ConcurrentDictionary<Type, Type> _cache = new();
  private readonly IServiceProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));

  public IOpenIddictAuthorizationStore<TAuthorization> Get<TAuthorization>() where TAuthorization : class
  {
    var store = _provider.GetService<IOpenIddictAuthorizationStore<TAuthorization>>();
    if (store is not null)
    {
      return store;
    }

    var type = _cache.GetOrAdd(typeof(TAuthorization), key =>
    {
      if (!typeof(OpenIddictDynamoDbAuthorization).IsAssignableFrom(key))
      {
        throw new InvalidOperationException(OpenIddictResources.GetResourceString(OpenIddictResources.ID0258));
      }

      return typeof(OpenIddictDynamoDbAuthorizationStore<>).MakeGenericType(key);
    });

    return (IOpenIddictAuthorizationStore<TAuthorization>)_provider.GetRequiredService(type);
  }
}
