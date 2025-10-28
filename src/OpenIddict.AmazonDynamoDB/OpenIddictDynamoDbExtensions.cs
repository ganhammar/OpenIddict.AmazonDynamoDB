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

    builder.ReplaceApplicationStore<OpenIddictDynamoDbApplication, OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>>(ServiceLifetime.Singleton)
      .ReplaceAuthorizationStore<OpenIddictDynamoDbAuthorization, OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>>(ServiceLifetime.Singleton)
      .ReplaceScopeStore<OpenIddictDynamoDbScope, OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>>(ServiceLifetime.Singleton)
      .ReplaceTokenStore<OpenIddictDynamoDbToken, OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>>(ServiceLifetime.Singleton);

    builder.Services.TryAddSingleton(typeof(OpenIddictDynamoDbApplicationStore<>));
    builder.Services.TryAddSingleton(typeof(OpenIddictDynamoDbAuthorizationStore<>));
    builder.Services.TryAddSingleton(typeof(OpenIddictDynamoDbScopeStore<>));
    builder.Services.TryAddSingleton(typeof(OpenIddictDynamoDbTokenStore<>));

    return new OpenIddictDynamoDbBuilder(builder.Services);
  }
}
