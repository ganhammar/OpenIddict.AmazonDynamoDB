using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace OpenIddict.AmazonDynamoDB.Migration;

public static class EnableTimeToLive
{
  public static Task Migrate(
    OpenIddictDynamoDbOptions options,
    IAmazonDynamoDB database,
    CancellationToken cancellationToken)
    => Migrate(options, database, NullLogger.Instance, cancellationToken);

  internal static async Task Migrate(
    OpenIddictDynamoDbOptions options,
    IAmazonDynamoDB database,
    ILogger logger,
    CancellationToken cancellationToken)
  {
    try
    {
      if (await IsEnabled(options, database, cancellationToken))
      {
        return;
      }

      logger.LogInformation("Enabling time to live on table {TableName}", options.DefaultTableName);

      await database.UpdateTimeToLiveAsync(new()
      {
        TableName = options.DefaultTableName,
        TimeToLiveSpecification = new()
        {
          Enabled = true,
          AttributeName = "ttl",
        },
      }, cancellationToken);
    }
    catch (AmazonDynamoDBException exception) when (DynamoDbUtils.IsAccessDenied(exception))
    {
      logger.LogWarning(
        exception,
        "Unable to enable time to live on table {TableName}, expired tokens and authorizations will not be removed",
        options.DefaultTableName);
    }
    catch (AmazonDynamoDBException)
    {
      // Another instance might have enabled time to live at the same time
      if (await IsEnabled(options, database, cancellationToken) == false)
      {
        throw;
      }
    }
  }

  private static async Task<bool> IsEnabled(
    OpenIddictDynamoDbOptions options,
    IAmazonDynamoDB database,
    CancellationToken cancellationToken)
  {
    var ttlSettings = await database.DescribeTimeToLiveAsync(new DescribeTimeToLiveRequest
    {
      TableName = options.DefaultTableName,
    }, cancellationToken);

    return new[] { TimeToLiveStatus.ENABLED, TimeToLiveStatus.ENABLING }
      .Contains(ttlSettings.TimeToLiveDescription?.TimeToLiveStatus);
  }
}
