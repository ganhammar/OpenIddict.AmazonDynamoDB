using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace OpenIddict.AmazonDynamoDB.Tests;

// Creates a table as version 3 did, with the eight global secondary indexes its stores queried
public static class LegacyTable
{
  public static async Task<string> CreateAsync(IAmazonDynamoDB client)
  {
    var tableName = $"legacy-{Guid.NewGuid()}";

    await client.CreateTableAsync(new CreateTableRequest
    {
      TableName = tableName,
      BillingMode = BillingMode.PAY_PER_REQUEST,
      KeySchema = [new("PartitionKey", KeyType.HASH), new("SortKey", KeyType.RANGE)],
      AttributeDefinitions =
      [
        new("PartitionKey", ScalarAttributeType.S),
        new("SortKey", ScalarAttributeType.S),
        new("ClientId", ScalarAttributeType.S),
        new("ApplicationId", ScalarAttributeType.S),
        new("RedirectUri", ScalarAttributeType.S),
        new("RedirectType", ScalarAttributeType.N),
        new("Subject", ScalarAttributeType.S),
        new("SearchKey", ScalarAttributeType.S),
        new("ScopeName", ScalarAttributeType.S),
        new("ScopeId", ScalarAttributeType.S),
        new("AuthorizationId", ScalarAttributeType.S),
        new("ReferenceId", ScalarAttributeType.S),
      ],
      GlobalSecondaryIndexes =
      [
        Index("ApplicationId-index", "ApplicationId", "SortKey"),
        Index("Subject-index", "Subject", "SearchKey"),
        Index("ClientId-index", "ClientId"),
        Index("RedirectUri-RedirectType-index", "RedirectUri", "RedirectType"),
        Index("Name-index", "ScopeName"),
        Index("ScopeId-index", "ScopeId", "PartitionKey"),
        Index("AuthorizationId-index", "AuthorizationId"),
        Index("ReferenceId-index", "ReferenceId"),
      ],
    });
    await client.UpdateTimeToLiveAsync(new UpdateTimeToLiveRequest
    {
      TableName = tableName,
      TimeToLiveSpecification = new() { Enabled = true, AttributeName = "ttl" },
    });

    for (var attempt = 0; attempt < 60; attempt++)
    {
      var table = await client.DescribeTableAsync(tableName);
      if (Equals(table.Table.TableStatus, TableStatus.ACTIVE)
        && table.Table.GlobalSecondaryIndexes.TrueForAll(x => Equals(x.IndexStatus, IndexStatus.ACTIVE)))
      {
        return tableName;
      }

      await Task.Delay(500);
    }

    throw new TimeoutException($"Table {tableName} did not become active");
  }

  public static async Task<int> CountItemsAsync(IAmazonDynamoDB client, string tableName)
  {
    var count = 0;
    Dictionary<string, AttributeValue>? exclusiveStartKey = default;

    do
    {
      var response = await client.ScanAsync(new ScanRequest
      {
        TableName = tableName,
        ExclusiveStartKey = exclusiveStartKey,
        ConsistentRead = true,
        Select = Select.COUNT,
      });
      count += response.Count ?? 0;
      exclusiveStartKey = response.LastEvaluatedKey;
    } while (exclusiveStartKey?.Count > 0);

    return count;
  }

  private static GlobalSecondaryIndex Index(string name, string hashKey, string? rangeKey = default) => new()
  {
    IndexName = name,
    KeySchema = rangeKey is null
      ? [new(hashKey, KeyType.HASH)]
      : [new(hashKey, KeyType.HASH), new(rangeKey, KeyType.RANGE)],
    Projection = new Projection { ProjectionType = ProjectionType.ALL },
  };
}
