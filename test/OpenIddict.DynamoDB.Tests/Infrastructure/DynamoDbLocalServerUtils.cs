using System.Collections.Concurrent;
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
        }

        private ConcurrentDictionary<string, DescribeTableResponse> TableDefinitions = new ConcurrentDictionary<string, DescribeTableResponse>();
        public async Task<(long, IEnumerable<KeyDefinition>)> GetTableInformation(string tableName)
        {
            if (TableDefinitions.ContainsKey(tableName) == false)
            {
                TableDefinitions.TryAdd(tableName, await Client.DescribeTableAsync(new DescribeTableRequest
                {
                    TableName = tableName,
                }));
            }

            var tableDefinition = TableDefinitions.GetValueOrDefault(tableName)!;

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