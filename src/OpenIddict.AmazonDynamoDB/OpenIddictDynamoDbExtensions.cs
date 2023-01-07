using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenIddict.AmazonDynamoDB;

namespace Microsoft.Extensions.DependencyInjection;

public static class OpenIddictDynamoDbExtensions
{
  public static OpenIddictDynamoDbBuilder UseDynamoDb(this OpenIddictCoreBuilder builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.SetDefaultApplicationEntity<OpenIddictDynamoDbApplication>()
      .SetDefaultAuthorizationEntity<OpenIddictDynamoDbAuthorization>()
      .SetDefaultTokenEntity<OpenIddictDynamoDbToken>()
      .SetDefaultScopeEntity<OpenIddictDynamoDbScope>();

    builder.ReplaceApplicationStoreResolver<OpenIddictDynamoDbApplicationStoreResolver>(ServiceLifetime.Singleton)
      .ReplaceAuthorizationStoreResolver<OpenIddictDynamoDbAuthorizationStoreResolver>(ServiceLifetime.Singleton)
      .ReplaceScopeStoreResolver<OpenIddictDynamoDbScopeStoreResolver>(ServiceLifetime.Singleton)
      .ReplaceTokenStoreResolver<OpenIddictDynamoDbTokenStoreResolver>(ServiceLifetime.Singleton);

    builder.Services.TryAddSingleton(typeof(OpenIddictDynamoDbApplicationStore<>));
    builder.Services.TryAddSingleton(typeof(OpenIddictDynamoDbAuthorizationStore<>));
    builder.Services.TryAddSingleton(typeof(OpenIddictDynamoDbScopeStore<>));
    builder.Services.TryAddSingleton(typeof(OpenIddictDynamoDbTokenStore<>));

    return new OpenIddictDynamoDbBuilder(builder.Services);
  }
}
