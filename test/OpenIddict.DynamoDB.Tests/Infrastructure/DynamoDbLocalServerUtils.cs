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
            foreach (var tableName in tables.TableNames)
            {
                DeleteTableData(tableName).GetAwaiter().GetResult();
            }

            Client.Dispose();
            _disposed = true;
        }

        public async Task DeleteTableData(string tableName)
        {
            Console.WriteLine("Truncating table {0}", tableName);
            var (numberOfItems, keys) = await GetKeyDefinitions(tableName);

            if (numberOfItems == 0)
            {
                return;
            }

            var allItems = new List<Dictionary<string, AttributeValue>>();
            Dictionary<string, AttributeValue>? exclusiveStartKey = default;

            Console.WriteLine("Fetching data for table {0}", tableName);
            while (exclusiveStartKey == default || exclusiveStartKey.Count > 0)
            {
                Console.WriteLine("Starting scan for items in table {0}", tableName);
                var data = await Client.ScanAsync(new ScanRequest
                {
                    TableName = tableName,
                    AttributesToGet = keys.Select(x => x.AttributeName).ToList(),
                    ExclusiveStartKey = exclusiveStartKey,
                });
                Console.WriteLine("End scan for items in table {0}", tableName);
                allItems.AddRange(data.Items);
                exclusiveStartKey = data.LastEvaluatedKey;
            }

            Console.WriteLine("Items fetched for table {0}", tableName);
            if (allItems.Any() == false)
            {
                return;
            }

            var writeRequests = allItems
                .Select(x => new WriteRequest
                {
                    DeleteRequest = new DeleteRequest
                    {
                        Key = x,
                    },
                })
                .ToList();

            var batches = ToChunks(writeRequests, 25);

            Console.WriteLine("Starting to delete items in table {0}", tableName);
            foreach (var batch in batches)
            {
                var request = new BatchWriteItemRequest
                {
                    RequestItems = new Dictionary<string, List<WriteRequest>>
                    {
                        { tableName, batch.ToList() },
                    },
                };

                await Client.BatchWriteItemAsync(request);
            }
            Console.WriteLine("Done with table {0}", tableName);
        }

        public async Task<(long, IEnumerable<KeyDefinition>)> GetKeyDefinitions(string tableName)
        {
            Console.WriteLine("Fetching table information about {0}", tableName);
            var tableDefinition = await Client.DescribeTableAsync(new DescribeTableRequest
            {
                TableName = tableName,
            });

            Console.WriteLine("Got table information about {0} contains {1} items", tableName, tableDefinition.Table.ItemCount);
            return (tableDefinition.Table.ItemCount, tableDefinition.Table.KeySchema.Select(x => new KeyDefinition
            {
                AttributeName = x.AttributeName,
                AttributeType = tableDefinition.Table.AttributeDefinitions
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