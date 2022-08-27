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

            Console.WriteLine($"Waiting for table {tableName} to become active...");

            await Task.Delay(TimeSpan.FromSeconds(5));
        } while (!active);
    }
}