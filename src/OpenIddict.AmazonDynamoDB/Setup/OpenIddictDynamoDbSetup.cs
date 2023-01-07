using Amazon.DynamoDBv2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace OpenIddict.AmazonDynamoDB;

public static class OpenIddictDynamoDbSetup
{
  public static void EnsureInitialized(IServiceProvider services)
  {
    var database = services.GetService<IAmazonDynamoDB>();

    EnsureInitialized(
      services.GetRequiredService<IOptionsMonitor<OpenIddictDynamoDbOptions>>(),
      database);
  }

  public static async Task EnsureInitializedAsync(
      IServiceProvider services,
      CancellationToken cancellationToken = default)
  {
    var database = services.GetService<IAmazonDynamoDB>();

    await EnsureInitializedAsync(
      services.GetRequiredService<IOptionsMonitor<OpenIddictDynamoDbOptions>>(),
      database,
      cancellationToken);
  }

  public static async Task EnsureInitializedAsync(
    IOptionsMonitor<OpenIddictDynamoDbOptions> openIddictDynamoDbOptions,
    IAmazonDynamoDB? database = default,
    CancellationToken cancellationToken = default)
  {
    var promises = new[]
    {
      OpenIddictDynamoDbApplicationSetup.EnsureInitializedAsync(
        openIddictDynamoDbOptions.CurrentValue, database),
      OpenIddictDynamoDbAuthorizationSetup.EnsureInitializedAsync(
        openIddictDynamoDbOptions.CurrentValue, database),
      OpenIddictDynamoDbTokenSetup.EnsureInitializedAsync(
        openIddictDynamoDbOptions.CurrentValue, database),
      OpenIddictDynamoDbScopeSetup.EnsureInitializedAsync(
        openIddictDynamoDbOptions.CurrentValue, database)
    };

    await Task.WhenAll(promises);
  }

  public static void EnsureInitialized(
    IOptionsMonitor<OpenIddictDynamoDbOptions> openIddictDynamoDbOptions,
    IAmazonDynamoDB? database = default)
  {
    EnsureInitializedAsync(openIddictDynamoDbOptions, database).GetAwaiter().GetResult();
  }
}
