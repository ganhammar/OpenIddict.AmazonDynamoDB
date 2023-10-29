using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace OpenIddict.AmazonDynamoDB.Migration;

public static class UpdateScopeIndexes
{
  public static async Task Migrate(
    OpenIddictDynamoDbOptions options,
    IAmazonDynamoDB database,
    CancellationToken cancellationToken)
  {
    var table = await database.DescribeTableAsync(options.DefaultTableName, cancellationToken);

    if (table.Table.GlobalSecondaryIndexes.Where(x => x.IndexName == "Resource-index").Any() == false)
    {
      return;
    }

    var provisionedThroughput = options.BillingMode != BillingMode.PAY_PER_REQUEST
      ? options.ProvisionedThroughput : default;

    await database.UpdateTableAsync(new()
    {
      TableName = options.DefaultTableName,
      GlobalSecondaryIndexUpdates = new()
      {
        new()
        {
          Delete = new()
          {
            IndexName = "Resource-index",
          },
        },
        new()
        {
          Create = new()
          {
            IndexName = "ScopeId-index",
            KeySchema = new List<KeySchemaElement>
            {
              new("ScopeId", KeyType.HASH),
              new("PartitionKey", KeyType.RANGE),
            },
            ProvisionedThroughput = provisionedThroughput,
            Projection = new Projection
            {
              ProjectionType = ProjectionType.ALL,
            },
          },
        },
      }
    }, cancellationToken);
  }
}
