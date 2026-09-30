using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Logging;

namespace OpenIddict.AmazonDynamoDB;

internal class DynamoDbUtils
{
  private const int MaxBatchWriteSize = 25;
  private const int MaxBatchWriteRetries = 8;

  public static async Task WaitForActiveTableAsync(
    IAmazonDynamoDB client, string tableName, ILogger logger, CancellationToken cancellationToken = default)
  {
    bool active;
    do
    {
      var response = await client.DescribeTableAsync(new DescribeTableRequest
      {
        TableName = tableName,
      }, cancellationToken);

      active = Equals(response.Table.TableStatus, TableStatus.ACTIVE)
        && (response.Table.GlobalSecondaryIndexes ?? new())
          .TrueForAll(g => Equals(g.IndexStatus, IndexStatus.ACTIVE));

      if (!active)
      {
        logger.LogInformation("Waiting for table {TableName} to become active", tableName);

        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
      }
    } while (!active);
  }

  // BatchWriteItem accepts at most 25 requests and can leave some of them unprocessed
  public static async Task BatchWriteAsync(
    IAmazonDynamoDB client,
    string tableName,
    IEnumerable<WriteRequest> requests,
    CancellationToken cancellationToken = default)
  {
    foreach (var chunk in requests.Chunk(MaxBatchWriteSize))
    {
      var unprocessed = chunk.ToList();

      for (var attempt = 0; unprocessed.Count > 0; attempt++)
      {
        if (attempt > MaxBatchWriteRetries)
        {
          throw new Exception($"Couldn't write {unprocessed.Count} items to table {tableName}");
        }

        if (attempt > 0)
        {
          await Task.Delay(TimeSpan.FromMilliseconds(50 * Math.Pow(2, attempt - 1)), cancellationToken);
        }

        var response = await client.BatchWriteItemAsync(new BatchWriteItemRequest
        {
          RequestItems = new()
          {
            { tableName, unprocessed },
          },
        }, cancellationToken);

        unprocessed = response.UnprocessedItems?.GetValueOrDefault(tableName) ?? new();
      }
    }
  }

  // Counts are changed atomically, reading and saving them loses concurrent changes
  public static Task UpdateCountAsync(
    IAmazonDynamoDB client,
    string tableName,
    CountType type,
    long change,
    CancellationToken cancellationToken)
  {
    var count = new CountModel(type);

    return client.UpdateItemAsync(new UpdateItemRequest
    {
      TableName = tableName,
      Key = new()
      {
        { "PartitionKey", new AttributeValue { S = count.PartitionKey } },
        { "SortKey", new AttributeValue { S = count.SortKey } },
      },
      UpdateExpression = "SET #type = :type ADD #count :change",
      ExpressionAttributeNames = new()
      {
        { "#type", "Type" },
        { "#count", "Count" },
      },
      ExpressionAttributeValues = new()
      {
        { ":type", new AttributeValue { N = ((int)type).ToString(CultureInfo.InvariantCulture) } },
        { ":change", new AttributeValue { N = change.ToString(CultureInfo.InvariantCulture) } },
      },
    }, cancellationToken);
  }

  public static async Task<List<WriteRequest>> GetDeleteRequestsForPartitionAsync(
    IAmazonDynamoDB client,
    string tableName,
    string partitionKey,
    CancellationToken cancellationToken)
  {
    var requests = new List<WriteRequest>();
    Dictionary<string, AttributeValue>? exclusiveStartKey = default;

    do
    {
      var response = await client.QueryAsync(new QueryRequest
      {
        TableName = tableName,
        ProjectionExpression = "PartitionKey, SortKey",
        KeyConditionExpression = "PartitionKey = :partitionKey",
        ExpressionAttributeValues = new()
        {
          { ":partitionKey", new AttributeValue { S = partitionKey } },
        },
        ExclusiveStartKey = exclusiveStartKey,
        ConsistentRead = true,
      }, cancellationToken);

      requests.AddRange((response.Items ?? new()).Select(x => new WriteRequest
      {
        DeleteRequest = new DeleteRequest { Key = x },
      }));
      exclusiveStartKey = response.LastEvaluatedKey;
    } while (exclusiveStartKey?.Count > 0);

    return requests;
  }

  public static WriteRequest ToDeleteRequest(string partitionKey, string? sortKey) => new()
  {
    DeleteRequest = new DeleteRequest
    {
      Key = new Dictionary<string, AttributeValue>
      {
        { "PartitionKey", new AttributeValue { S = partitionKey } },
        { "SortKey", new AttributeValue { S = sortKey } },
      },
    },
  };

  public static bool IsAccessDenied(AmazonDynamoDBException exception)
    => exception.ErrorCode == "AccessDeniedException";

  public static async Task<(string?, List<T>)> Paginate<T>(
    IAmazonDynamoDB client,
    int? size = default,
    string? token = default,
    CancellationToken cancellationToken = default)
  {
    var page = new List<Document>();
    var context = new DynamoDBContextBuilder()
      .WithDynamoDBClient(() => client)
      .Build();
    var table = context.GetTargetTable<T>();

    var tableDefinition = await client.DescribeTableAsync(table.TableName, cancellationToken);
    if (size.HasValue == false)
    {
      size = unchecked((int)(tableDefinition.Table.ItemCount ?? 0));
    }

    var isDone = false;
    do
    {
      var scan = table.Scan(new ScanOperationConfig
      {
        PaginationToken = token,
        Limit = Math.Min(100, size.Value),
      });

      var batch = await scan.GetNextSetAsync(cancellationToken);

      var take = batch.Take(size.Value - page.Count);
      page.AddRange(take);

      isDone = page.Count == size || scan.IsDone;
      token = scan.PaginationToken;
    }
    while (!isDone);

    var items = page.Select(x => context.FromDocument<T>(x)).ToList();

    return (token, items);
  }
}
