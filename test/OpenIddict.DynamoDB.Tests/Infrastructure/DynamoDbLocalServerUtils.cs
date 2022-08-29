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
                try
                {
                    Client.DeleteTableAsync(tableName).GetAwaiter().GetResult();
                } catch (ResourceNotFoundException) { }
            });

            Client.Dispose();
            _disposed = true;
        }
    }
}