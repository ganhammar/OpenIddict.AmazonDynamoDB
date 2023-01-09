using Amazon.DynamoDBv2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace OpenIddict.AmazonDynamoDB.IntegrationTests;

public class Main
{
    [Fact]
    public async Task Should_CreateTable_When_CallingSetup()
    {
      // Arrange
      var tableName = Guid.NewGuid().ToString();
      var collection = new ServiceCollection()
        .AddOpenIddict()
        .AddCore()
        .UseDynamoDb()
        .SetDefaultTableName(tableName);
      var client = new AmazonDynamoDBClient();

      var mock = new Mock<IOptionsMonitor<OpenIddictDynamoDbOptions>>();
      mock.Setup(x => x.CurrentValue).Returns(new OpenIddictDynamoDbOptions
      {
        DefaultTableName = tableName,
        Database = client,
      });

      // Act
      OpenIddictDynamoDbSetup.EnsureInitialized(mock.Object);

      // Assert
      var tableNames = await client.ListTablesAsync();
      Assert.Contains(tableName, tableNames.TableNames);

      await client.DeleteTableAsync(tableName);
    }
}
