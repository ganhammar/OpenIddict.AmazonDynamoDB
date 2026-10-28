using Amazon.DynamoDBv2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace OpenIddict.AmazonDynamoDB;

public static class OpenIddictDynamoDbSetup
{
  public static void EnsureInitialized(IServiceProvider services)
  {
    EnsureInitializedAsync(services).GetAwaiter().GetResult();
  }

  public static async Task EnsureInitializedAsync(
      IServiceProvider services,
      CancellationToken cancellationToken = default)
  {
    var database = services.GetService<IAmazonDynamoDB>();
    var logger = services.GetService<ILoggerFactory>()?.CreateLogger(typeof(OpenIddictDynamoDbSetup))
      ?? NullLogger.Instance;

    await DynamoDbTableSetup.EnsureInitializedAsync(
      services.GetRequiredService<IOptionsMonitor<OpenIddictDynamoDbOptions>>().CurrentValue,
      database,
      logger,
      cancellationToken);
  }

  public static async Task EnsureInitializedAsync(
    IOptionsMonitor<OpenIddictDynamoDbOptions> openIddictDynamoDbOptions,
    IAmazonDynamoDB? database = default,
    CancellationToken cancellationToken = default)
  {
    await DynamoDbTableSetup.EnsureInitializedAsync(
      openIddictDynamoDbOptions.CurrentValue, database, cancellationToken);
  }

  public static void EnsureInitialized(
    IOptionsMonitor<OpenIddictDynamoDbOptions> openIddictDynamoDbOptions,
    IAmazonDynamoDB? database = default)
  {
    EnsureInitializedAsync(openIddictDynamoDbOptions, database).GetAwaiter().GetResult();
  }

  /// <summary>Writes the lookup items a table created by version 3 lacks; see <see cref="DynamoDbTableSetup.MigrateAsync(OpenIddictDynamoDbOptions, IAmazonDynamoDB?, CancellationToken)"/>.</summary>
  public static Task<MigrationResult> MigrateAsync(
    IServiceProvider services,
    CancellationToken cancellationToken = default)
    => DynamoDbTableSetup.MigrateAsync(
      services.GetRequiredService<IOptionsMonitor<OpenIddictDynamoDbOptions>>().CurrentValue,
      services.GetService<IAmazonDynamoDB>(),
      services.GetService<ILoggerFactory>()?.CreateLogger(typeof(OpenIddictDynamoDbSetup)) ?? NullLogger.Instance,
      cancellationToken);

  public static Task<MigrationResult> MigrateAsync(
    IOptionsMonitor<OpenIddictDynamoDbOptions> openIddictDynamoDbOptions,
    IAmazonDynamoDB? database = default,
    CancellationToken cancellationToken = default)
    => DynamoDbTableSetup.MigrateAsync(openIddictDynamoDbOptions.CurrentValue, database, cancellationToken);

  /// <summary>Removes the global secondary indexes of version 3; see <see cref="DynamoDbTableSetup.RemoveLegacyIndexesAsync(OpenIddictDynamoDbOptions, IAmazonDynamoDB?, CancellationToken)"/>.</summary>
  public static Task RemoveLegacyIndexesAsync(
    IServiceProvider services,
    CancellationToken cancellationToken = default)
    => DynamoDbTableSetup.RemoveLegacyIndexesAsync(
      services.GetRequiredService<IOptionsMonitor<OpenIddictDynamoDbOptions>>().CurrentValue,
      services.GetService<IAmazonDynamoDB>(),
      services.GetService<ILoggerFactory>()?.CreateLogger(typeof(OpenIddictDynamoDbSetup)) ?? NullLogger.Instance,
      cancellationToken);

  public static Task RemoveLegacyIndexesAsync(
    IOptionsMonitor<OpenIddictDynamoDbOptions> openIddictDynamoDbOptions,
    IAmazonDynamoDB? database = default,
    CancellationToken cancellationToken = default)
    => DynamoDbTableSetup.RemoveLegacyIndexesAsync(openIddictDynamoDbOptions.CurrentValue, database, cancellationToken);
}
