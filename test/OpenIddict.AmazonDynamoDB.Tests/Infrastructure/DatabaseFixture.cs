using Amazon.DynamoDBv2;
using Amazon.Runtime;

namespace OpenIddict.AmazonDynamoDB.Tests;

public class DatabaseFixture : IDisposable
{
  public static readonly string TableName = Guid.NewGuid().ToString();
  public readonly AmazonDynamoDBClient Client;
  private bool _disposed;

  public DatabaseFixture()
  {
    Client = new AmazonDynamoDBClient();
    CreateTable().GetAwaiter().GetResult();
  }

  protected DatabaseFixture(BasicAWSCredentials credentials, AmazonDynamoDBConfig config)
  {
    Client = new AmazonDynamoDBClient(credentials, config);
    CreateTable().GetAwaiter().GetResult();
  }

  private async Task CreateTable()
  {
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(TestUtils.GetOptions(new()
    {
      Database = Client,
      DefaultTableName = TableName,
    }));
  }

  public void Dispose()
  {
    if (_disposed)
    {
      return;
    }

    Client.DeleteTableAsync(TableName).GetAwaiter().GetResult();
    Client.Dispose();
    _disposed = true;
  }
}
