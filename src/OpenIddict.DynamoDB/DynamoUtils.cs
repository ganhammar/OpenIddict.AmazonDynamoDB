using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace OpenIddict.DynamoDB;

public class DynamoUtils
{
    public static async Task WaitForActiveTableAsync(IAmazonDynamoDB client, string tableName)
    {
        bool active;
        do
        {
            active = true;
            var response = await client.DescribeTableAsync(new DescribeTableRequest
            {
                TableName = tableName,
            });

            if (!Equals(response.Table.TableStatus, TableStatus.ACTIVE) ||
                !response.Table.GlobalSecondaryIndexes.TrueForAll(g => Equals(g.IndexStatus, IndexStatus.ACTIVE)))
            {
                active = false;
            }

            if (!active)
            {
                Console.WriteLine($"Waiting for table {tableName} to become active...");

                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        } while (!active);
    }

    public static async Task UpdateSecondaryIndexes(IAmazonDynamoDB client, string tableName, List<GlobalSecondaryIndex> globalSecondaryIndexes)
    {
        var response = await client.DescribeTableAsync(new DescribeTableRequest { TableName = tableName });
        var table = response.Table;

        var indexesToAdd = globalSecondaryIndexes
            .Where(g => !table.GlobalSecondaryIndexes
                .Exists(gd => gd.IndexName.Equals(g.IndexName)));
        var indexUpdates = indexesToAdd
            .Select(index => new GlobalSecondaryIndexUpdate
            {
                Create = new CreateGlobalSecondaryIndexAction
                {
                    IndexName = index.IndexName,
                    KeySchema = index.KeySchema,
                    ProvisionedThroughput = index.ProvisionedThroughput,
                    Projection = index.Projection
                }
            })
            .ToList();

        if (indexUpdates.Count > 0)
        {
            await UpdateTableAsync(client, tableName, indexUpdates);
        }
    }

    private static async Task UpdateTableAsync(IAmazonDynamoDB client, string tableName,
        List<GlobalSecondaryIndexUpdate> indexUpdates)
    {
        await client.UpdateTableAsync(new UpdateTableRequest
        {
            TableName = tableName,
            GlobalSecondaryIndexUpdates = indexUpdates
        });

        await DynamoUtils.WaitForActiveTableAsync(client, tableName);
    }
}
