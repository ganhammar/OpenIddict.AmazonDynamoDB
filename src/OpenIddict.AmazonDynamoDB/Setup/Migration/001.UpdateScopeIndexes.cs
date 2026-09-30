using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace OpenIddict.AmazonDynamoDB.Migration;

// Tables created before scopes were stored with lookups have a Resource-index instead of
// the ScopeId-index, DynamoDB only allows one index to be removed or added at a time
public static class UpdateScopeIndexes
{
  private const string ResourceIndexName = "Resource-index";

  public static async Task Migrate(
    OpenIddictDynamoDbOptions options,
    IAmazonDynamoDB database,
    CancellationToken cancellationToken)
  {
    await RemoveResourceIndex(options, database, NullLogger.Instance, cancellationToken);
    await DynamoDbTableSetup.AddMissingGlobalSecondaryIndexes(
      options, database, NullLogger.Instance, cancellationToken);
  }

  internal static async Task RemoveResourceIndex(
    OpenIddictDynamoDbOptions options,
    IAmazonDynamoDB database,
    ILogger logger,
    CancellationToken cancellationToken)
  {
    var table = await DynamoDbTableSetup.DescribeTable(database, options.DefaultTableName, cancellationToken);
    var resourceIndex = table?.GlobalSecondaryIndexes?.Find(x => x.IndexName == ResourceIndexName);

    if (resourceIndex == default)
    {
      return;
    }

    if (Equals(resourceIndex.IndexStatus, IndexStatus.DELETING) == false)
    {
      await DynamoDbUtils.WaitForActiveTableAsync(database, options.DefaultTableName, logger, cancellationToken);

      logger.LogInformation(
        "Removing global secondary index {IndexName} from table {TableName}",
        ResourceIndexName,
        options.DefaultTableName);

      try
      {
        await database.UpdateTableAsync(new()
        {
          TableName = options.DefaultTableName,
          GlobalSecondaryIndexUpdates = new()
          {
            new()
            {
              Delete = new()
              {
                IndexName = ResourceIndexName,
              },
            },
          },
        }, cancellationToken);
      }
      catch (AmazonDynamoDBException exception) when (DynamoDbUtils.IsAccessDenied(exception))
      {
        logger.LogWarning(
          exception,
          "Unable to remove global secondary index {IndexName} from table {TableName}, remove it manually or allow dynamodb:UpdateTable",
          ResourceIndexName,
          options.DefaultTableName);
        return;
      }
      catch (AmazonDynamoDBException)
      {
        // Another instance might be removing the index at the same time
        var current = await DynamoDbTableSetup.DescribeTable(database, options.DefaultTableName, cancellationToken);
        var index = current?.GlobalSecondaryIndexes?.Find(x => x.IndexName == ResourceIndexName);

        if (index != default && Equals(index.IndexStatus, IndexStatus.DELETING) == false)
        {
          throw;
        }
      }
    }

    // The index is listed as deleting until it has been removed
    await DynamoDbUtils.WaitForActiveTableAsync(database, options.DefaultTableName, logger, cancellationToken);
  }
}
