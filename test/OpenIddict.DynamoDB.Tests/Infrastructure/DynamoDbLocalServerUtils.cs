using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;

namespace OpenIddict.DynamoDB.Tests;

internal static class DynamoDbLocalServerUtils
{
    public static DisposableDatabase CreateDatabase() => new DisposableDatabase();

    public class DisposableDatabase : IDisposable
    {
        private bool _disposed;

        public DisposableDatabase()
        {
            Client = new AmazonDynamoDBClient(
                new BasicAWSCredentials("test", "test"),
                new AmazonDynamoDBConfig
                {
                    ServiceURL = "http://localhost:8000"
                });
        }

        public IAmazonDynamoDB Client { get; }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            var tables = Client.ListTablesAsync().GetAwaiter().GetResult();
            tables.TableNames.ForEach(tableName =>
            {
                DeleteTableData(tableName).GetAwaiter().GetResult();
            });

            Client.Dispose();
            _disposed = true;
        }

        public async Task DeleteTableData(string tableName)
        {
            var (numberOfItems, keys) = await GetTableInformation(tableName);
            var allItems = new List<Dictionary<string, AttributeValue>>();
            Dictionary<string, AttributeValue>? exclusiveStartKey = default;

            while (exclusiveStartKey == default || exclusiveStartKey.Count > 0)
            {
                var data = await Client.ScanAsync(new ScanRequest
                {
                    TableName = tableName,
                    AttributesToGet = keys.Select(x => x.AttributeName).ToList(),
                    ExclusiveStartKey = exclusiveStartKey,
                });
                allItems.AddRange(data.Items);
                exclusiveStartKey = data.LastEvaluatedKey;
            }

            // if (allItems.Any() == false)
            // {
            //     return;
            // }

            // var writeRequests = allItems
            //     .Select(x => new WriteRequest
            //     {
            //         DeleteRequest = new DeleteRequest
            //         {
            //             Key = x,
            //         },
            //     })
            //     .ToList();

            // var batches = ToChunks(writeRequests, 25);

            // foreach (var batch in batches)
            // {
            //     var request = new BatchWriteItemRequest
            //     {
            //         RequestItems = new Dictionary<string, List<WriteRequest>>
            //         {
            //             { tableName, batch.ToList() },
            //         },
            //     };

            //     await Client.BatchWriteItemAsync(request);
            // }
        }

        public async Task<(long, IEnumerable<KeyDefinition>)> GetTableInformation(string tableName)
        {
            var response = await Client.DescribeTableAsync(new DescribeTableRequest
            {
                TableName = tableName,
            });

            return (response.Table.ItemCount, response.Table.KeySchema.Select(x => new KeyDefinition
            {
                AttributeName = x.AttributeName,
                AttributeType = response.Table.AttributeDefinitions
                    .First(y => y.AttributeName == x.AttributeName)
                    .AttributeType,
                KeyType = x.KeyType,
            }));
        }

        private IEnumerable<IEnumerable<T>> ToChunks<T>(List<T> fullList, int batchSize)
        {
            var total = 0;

            while (total < fullList.Count)
            {
                yield return fullList.Skip(total).Take(batchSize);
                total += batchSize;
            }
        }
    }

    public class KeyDefinition
    {
        public string? AttributeName { get; set; }
        public ScalarAttributeType? AttributeType { get; set; }
        public KeyType? KeyType { get; set; }
    }
}