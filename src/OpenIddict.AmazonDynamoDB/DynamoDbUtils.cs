using System.Globalization;
using System.Text;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Logging;

namespace OpenIddict.AmazonDynamoDB;

internal class DynamoDbUtils
{
  private const int MaxBatchWriteSize = 25;
  private const int MaxBatchWriteRetries = 8;

  /// <summary>
  /// A filter on the partition key's prefix, for the indexes several entity types share; the
  /// query puts the prefix in <see cref="PartitionKeyPrefixValue"/>.
  /// </summary>
  public const string PartitionKeyPrefixFilter = "begins_with(PartitionKey, :partitionKeyPrefix)";
  public const string PartitionKeyPrefixValue = ":partitionKeyPrefix";

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

  // BatchWriteItem accepts at most 25 requests, none of them for the same key, and can leave some
  // of them unprocessed. Of several requests for one key the last is written.
  public static async Task BatchWriteAsync(
    IAmazonDynamoDB client,
    string tableName,
    IEnumerable<WriteRequest> requests,
    CancellationToken cancellationToken = default)
  {
    var distinct = new Dictionary<(string, string?), WriteRequest>();

    foreach (var request in requests)
    {
      var key = request.PutRequest?.Item ?? request.DeleteRequest?.Key ?? throw new ArgumentException("A write request puts or deletes an item", nameof(requests));
      distinct[(key["PartitionKey"].S, key.GetValueOrDefault("SortKey")?.S)] = request;
    }

    foreach (var chunk in distinct.Values.Chunk(MaxBatchWriteSize))
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
      Key = DynamoDbItems.Key(count),
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

  public static async Task<long> GetCountAsync(
    IAmazonDynamoDB client,
    string tableName,
    CountType type,
    CancellationToken cancellationToken)
  {
    var count = new CountModel(type);
    var stored = await DynamoDbItems.GetAsync<CountModel>(client, tableName, count.PartitionKey, count.SortKey, cancellationToken);

    return stored?.Count ?? 0;
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
      Key = DynamoDbItems.Key(partitionKey, sortKey),
    },
  };

  public static bool IsAccessDenied(AmazonDynamoDBException exception)
    => exception.ErrorCode == "AccessDeniedException";

  // Scans the items of one entity type, the table also holds the other entity types. The
  // returned token continues after the last returned item, so that no item is skipped
  public static async Task<(string?, List<T>)> Paginate<T>(
    IAmazonDynamoDB client,
    string tableName,
    string partitionKeyPrefix,
    string sortKeyPrefix,
    int? size = default,
    string? token = default,
    CancellationToken cancellationToken = default)
    where T : IDynamoDbItem, new()
  {
    var items = new List<T>();
    var exclusiveStartKey = token == default ? default : ParseToken(token);

    if (size <= 0)
    {
      return (token, items);
    }

    do
    {
      var response = await client.ScanAsync(new ScanRequest
      {
        TableName = tableName,
        FilterExpression = "begins_with(PartitionKey, :partitionKey) and begins_with(SortKey, :sortKey)",
        ExpressionAttributeValues = new()
        {
          { ":partitionKey", new AttributeValue { S = partitionKeyPrefix } },
          { ":sortKey", new AttributeValue { S = sortKeyPrefix } },
        },
        ExclusiveStartKey = exclusiveStartKey,
      }, cancellationToken);

      foreach (var item in response.Items ?? new())
      {
        items.Add(DynamoDbItems.FromItem<T>(item));

        if (items.Count == size)
        {
          return (CreateToken(item["PartitionKey"].S, item["SortKey"].S), items);
        }
      }

      exclusiveStartKey = response.LastEvaluatedKey;
    } while (exclusiveStartKey?.Count > 0);

    return (default, items);
  }

  // The pagination token is the key of the last returned item as a JSON object
  private static string CreateToken(string partitionKey, string sortKey)
  {
    using var stream = new MemoryStream();
    using (var writer = new Utf8JsonWriter(stream))
    {
      writer.WriteStartObject();
      writer.WriteString("PartitionKey", partitionKey);
      writer.WriteString("SortKey", sortKey);
      writer.WriteEndObject();
    }

    return Encoding.UTF8.GetString(stream.ToArray());
  }

  private static Dictionary<string, AttributeValue> ParseToken(string token)
  {
    using var document = JsonDocument.Parse(token);

    return document.RootElement.EnumerateObject()
      .ToDictionary(x => x.Name, x => new AttributeValue { S = x.Value.GetString() });
  }

  // SearchKey is APPLICATION#{ApplicationId}#STATUS#{Status}#TYPE#{Type}, only leading values
  // can be part of the prefix and each value is followed by its separator to match it exactly
  public static string GetSearchKeyPrefix(string? client, string? status, string? type)
  {
    if (string.IsNullOrEmpty(client))
    {
      return "APPLICATION#";
    }
    else if (string.IsNullOrEmpty(status))
    {
      return $"APPLICATION#{client}#STATUS#";
    }
    else if (string.IsNullOrEmpty(type))
    {
      return $"APPLICATION#{client}#STATUS#{status}#TYPE#";
    }

    return $"APPLICATION#{client}#STATUS#{status}#TYPE#{type}";
  }

  public static bool IsMatch(string? value, string? filter)
    => string.IsNullOrEmpty(filter) || string.Equals(value, filter, StringComparison.Ordinal);
}
