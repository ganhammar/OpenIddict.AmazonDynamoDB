using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;
using Moq;

namespace OpenIddict.AmazonDynamoDB.Tests;

public static class TestUtils
{
  public static IOptionsMonitor<OpenIddictDynamoDbOptions> GetOptions(OpenIddictDynamoDbOptions options)
  {
    options.DefaultTableName = options.DefaultTableName == "openiddict"
      ? DatabaseFixture.TableName : options.DefaultTableName;
    var mock = new Mock<IOptionsMonitor<OpenIddictDynamoDbOptions>>();
    mock.Setup(x => x.CurrentValue).Returns(options);
    return mock.Object;
  }

  public static async Task<List<T>> ToListAsync<T>(IAsyncEnumerable<T> items)
  {
    var result = new List<T>();
    await foreach (var item in items)
    {
      result.Add(item);
    }
    return result;
  }

  // Counts the items of an entity type by scanning the table directly
  public static async Task<int> CountItemsAsync(IAmazonDynamoDB client, string partitionKeyPrefix, string sortKeyPrefix)
  {
    var count = 0;
    Dictionary<string, AttributeValue>? exclusiveStartKey = default;

    do
    {
      var response = await client.ScanAsync(new ScanRequest
      {
        TableName = DatabaseFixture.TableName,
        FilterExpression = "begins_with(PartitionKey, :partitionKey) and begins_with(SortKey, :sortKey)",
        ExpressionAttributeValues = new()
        {
          { ":partitionKey", new(partitionKeyPrefix) },
          { ":sortKey", new(sortKeyPrefix) },
        },
        ExclusiveStartKey = exclusiveStartKey,
        ConsistentRead = true,
      });
      count += response.Count ?? 0;
      exclusiveStartKey = response.LastEvaluatedKey;
    } while (exclusiveStartKey?.Count > 0);

    return count;
  }
}
