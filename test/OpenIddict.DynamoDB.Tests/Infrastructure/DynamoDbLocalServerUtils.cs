using System.Collections.Concurrent;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;

namespace OpenIddict.DynamoDB.Tests;

internal static class DynamoDbLocalServerUtils
{
    public static DisposableDatabase CreateDatabase() => new DisposableDatabase();
    private static ConcurrentDictionary<string, DescribeTableResponse> TableDefinitions = new();

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
            Console.WriteLine("Truncating {0}", tableName);
            var keys = await GetKeyDefinitions(tableName);
            var allItems = new List<Dictionary<string, AttributeValue>>();
            Dictionary<string, AttributeValue>? exclusiveStartKey = default;

            Console.WriteLine("Listing items for {0}", tableName);
            var iterations = 0;
            while ((exclusiveStartKey == default || exclusiveStartKey.Count > 0) && iterations < 5)
            {
                Console.WriteLine("Starting iteration of items for {0}, current iteration {1}", tableName, iterations);
                var data = await Client.ScanAsync(new ScanRequest
                {
                    TableName = tableName,
                    AttributesToGet = keys.Select(x => x.AttributeName).ToList(),
                    ExclusiveStartKey = exclusiveStartKey,
                });
                allItems.AddRange(data.Items);
                exclusiveStartKey = data.LastEvaluatedKey;
                iterations += 1;
                Console.WriteLine("End of iteration of items for {0}, current iteration {1}", tableName, iterations);
            }

            Console.WriteLine("Table {0} has {1} items", tableName, allItems.Count);
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

            Console.WriteLine("Deleting data in {0}", tableName);
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
            Console.WriteLine("All done with {0}", tableName);
        }

        public async Task<IEnumerable<KeyDefinition>> GetKeyDefinitions(string tableName)
        {
            Console.WriteLine("Getting key definitions for {0}", tableName);
            if (TableDefinitions.ContainsKey(tableName) == false)
            {
                TableDefinitions.TryAdd(tableName, await Client.DescribeTableAsync(new DescribeTableRequest
                {
                    TableName = tableName,
                }));
            }

            var tableDefinition = TableDefinitions.GetValueOrDefault(tableName)!;
            Console.WriteLine("Got table definition for {0}", tableName);

            return tableDefinition.Table.KeySchema.Select(x => new KeyDefinition
            {
                AttributeName = x.AttributeName,
                AttributeType = tableDefinition.Table.AttributeDefinitions
                    .First(y => y.AttributeName == x.AttributeName)
                    .AttributeType,
                KeyType = x.KeyType,
            });
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