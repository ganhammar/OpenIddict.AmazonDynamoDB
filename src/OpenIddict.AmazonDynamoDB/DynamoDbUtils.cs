using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;

namespace OpenIddict.AmazonDynamoDB;

internal class DynamoDbUtils
{
  public static async Task WaitForActiveTableAsync(
    IAmazonDynamoDB client, string tableName, CancellationToken cancellationToken = default)
  {
    bool active;
    do
    {
      active = true;
      var response = await client.DescribeTableAsync(new DescribeTableRequest
      {
        TableName = tableName,
      }, cancellationToken);

      if (!Equals(response.Table.TableStatus, TableStatus.ACTIVE) ||
        !response.Table.GlobalSecondaryIndexes.TrueForAll(g => Equals(g.IndexStatus, IndexStatus.ACTIVE)))
      {
        active = false;
      }

      if (!active)
      {
        Console.WriteLine($"Waiting for table {tableName} to become active...");

        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
      }
    } while (!active);
  }

  public static async Task<(string?, List<T>)> Paginate<T>(
    IAmazonDynamoDB client,
    int? size = default,
    string? token = default,
    CancellationToken cancellationToken = default)
  {
    var page = new List<Document>();
    var context = new DynamoDBContext(client);
    var table = context.GetTargetTable<T>();

    var tableDefinition = await client.DescribeTableAsync(table.TableName, cancellationToken);
    if (size.HasValue == false)
    {
      size = unchecked((int)tableDefinition.Table.ItemCount);
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
