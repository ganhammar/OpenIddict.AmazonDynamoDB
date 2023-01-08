using Amazon.DynamoDBv2;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace OpenIddict.AmazonDynamoDB.Tests;

[Collection(Constants.DatabaseCollection)]
public class OpenIddictDynamoDbSetupTests
{
  [Fact]
  public async Task Should_SetupTables_When_CalledSynchronously()
  {
    // Arrange
    var options = TestUtils.GetOptions(new()
    {
      Database = DatabaseFixture.Client,
    });

    // Act
    OpenIddictDynamoDbSetup.EnsureInitialized(options);

    // Assert
    var tableNames = await DatabaseFixture.Client.ListTablesAsync();
    Assert.Contains(DatabaseFixture.TableName, tableNames.TableNames);
  }

  [Fact]
  public async Task Should_SetupTables_When_CalledSynchronouslyWithServiceProvider()
  {
    // Arrange
    var services = new ServiceCollection();
    CreateBuilder(services).UseDatabase(DatabaseFixture.Client);

    // Act
    OpenIddictDynamoDbSetup.EnsureInitialized(services.BuildServiceProvider());

    // Assert
    var tableNames = await DatabaseFixture.Client.ListTablesAsync();
    Assert.Contains(DatabaseFixture.TableName, tableNames.TableNames);
  }

  [Fact]
  public async Task Should_SetupTables_When_CalledAsynchronously()
  {
    // Arrange
    var options = TestUtils.GetOptions(new()
    {
      Database = DatabaseFixture.Client,
    });

    // Act
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Assert
    var tableNames = await DatabaseFixture.Client.ListTablesAsync();
    Assert.Contains(DatabaseFixture.TableName, tableNames.TableNames);
  }

  [Fact]
  public async Task Should_SetupTables_When_CalledAsynchronouslyWithServiceProvider()
  {
    // Arrange
    var services = new ServiceCollection();
    CreateBuilder(services).UseDatabase(DatabaseFixture.Client);

    // Act
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(services.BuildServiceProvider());

    // Assert
    var tableNames = await DatabaseFixture.Client.ListTablesAsync();
    Assert.Contains(DatabaseFixture.TableName, tableNames.TableNames);
  }

  [Fact]
  public async Task Should_SetupTables_When_CalledAsynchronouslyWithDatbaseInServiceProvider()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddSingleton<IAmazonDynamoDB>(DatabaseFixture.Client);
    CreateBuilder(services);

    // Act
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(services.BuildServiceProvider());

    // Assert
    var tableNames = await DatabaseFixture.Client.ListTablesAsync();
    Assert.Contains(DatabaseFixture.TableName, tableNames.TableNames);
  }

  [Fact]
  public async Task Should_SetupTables_When_CalledSynchronouslyWithDatbaseInServiceProvider()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddSingleton<IAmazonDynamoDB>(DatabaseFixture.Client);
    CreateBuilder(services);

    // Act
    OpenIddictDynamoDbSetup.EnsureInitialized(services.BuildServiceProvider());

    // Assert
    var tableNames = await DatabaseFixture.Client.ListTablesAsync();
    Assert.Contains(DatabaseFixture.TableName, tableNames.TableNames);
  }

  private static OpenIddictDynamoDbBuilder CreateBuilder(IServiceCollection services)
    => services.AddOpenIddict().AddCore().UseDynamoDb().SetDefaultTableName(DatabaseFixture.TableName);
}
