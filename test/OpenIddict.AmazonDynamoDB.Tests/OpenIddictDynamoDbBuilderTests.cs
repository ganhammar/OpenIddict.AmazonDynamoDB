using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Core;

namespace OpenIddict.AmazonDynamoDB.Tests;

public class OpenIddictDynamoDbBuilderTests
{
  [Fact]
  public void Should_ThrowException_When_ServicesIsNullInConstructor()
  {
    // Arrange
    var services = (IServiceCollection)null!;

    // Act & Assert
    var exception = Assert.Throws<ArgumentNullException>(() => new OpenIddictDynamoDbBuilder(services));
    Assert.Equal("services", exception.ParamName);
  }

  [Fact]
  public void Should_ThrowException_When_TryingToConfigureAndActionIsNull()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act & Assert
    var exception = Assert.Throws<ArgumentNullException>(() =>
      CreateBuilder(services).Configure(null!));
    Assert.Equal("configuration", exception.ParamName);
  }

  [Fact]
  public void Should_ThrowException_When_SetDefaultTableNameAndNameIsNull()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act & Assert
    var exception = Assert.Throws<ArgumentNullException>(() =>
      CreateBuilder(services).SetDefaultTableName(null!));
    Assert.Equal("name", exception.ParamName);
  }

  [Fact]
  public void Should_SetTableName_When_CallingSetDefaultTableName()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    CreateBuilder(services).SetDefaultTableName("test");

    // Assert
    var serviceProvider = services.BuildServiceProvider();
    var options = serviceProvider.GetRequiredService<IOptionsMonitor<OpenIddictDynamoDbOptions>>().CurrentValue;
    Assert.Equal("test", options.DefaultTableName);
  }

  [Fact]
  public void Should_ThrowException_When_CallingUseDatabaseAndDatabaseIsNull()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act & Assert
    var exception = Assert.Throws<ArgumentNullException>(() =>
      CreateBuilder(services).UseDatabase(null!));
    Assert.Equal("database", exception.ParamName);
  }

  [Fact]
  public void Should_SetDatabase_When_CallingUseDatabase()
  {
    // Arrange
    var services = new ServiceCollection();
    var db = new AmazonDynamoDBClient();

    // Act
    CreateBuilder(services).UseDatabase(db);

    // Assert
    var serviceProvider = services.BuildServiceProvider();
    var options = serviceProvider.GetRequiredService<IOptionsMonitor<OpenIddictDynamoDbOptions>>().CurrentValue;
    Assert.Equal(db, options.Database);
  }

  [Fact]
  public void Should_ThrowException_When_SettingBillingModeAndBillingModeIsNull()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act & Assert
    var exception = Assert.Throws<ArgumentNullException>(() =>
      CreateBuilder(services).SetBillingMode(null!));
    Assert.Equal("billingMode", exception.ParamName);
  }

  [Fact]
  public void Should_SetBillingMode_When_CallingSetBillingMode()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act
    CreateBuilder(services).SetBillingMode(BillingMode.PROVISIONED);

    // Assert
    var serviceProvider = services.BuildServiceProvider();
    var options = serviceProvider.GetRequiredService<IOptionsMonitor<OpenIddictDynamoDbOptions>>().CurrentValue;
    Assert.Equal(BillingMode.PROVISIONED, options.BillingMode);
  }

  [Fact]
  public void Should_ThrowException_When_SettingProvisionedThroughputAndProvisionedThroughputIsNull()
  {
    // Arrange
    var services = new ServiceCollection();

    // Act & Assert
    var exception = Assert.Throws<ArgumentNullException>(() =>
      CreateBuilder(services).SetProvisionedThroughput(null!));
    Assert.Equal("provisionedThroughput", exception.ParamName);
  }

  [Fact]
  public void Should_SetProvisionedThroughput_When_CallingSetProvisionedThroughput()
  {
    // Arrange
    var services = new ServiceCollection();
    var throughput = new ProvisionedThroughput
    {
      ReadCapacityUnits = 99,
      WriteCapacityUnits = 99,
    };

    // Act
    CreateBuilder(services).SetProvisionedThroughput(throughput);

    // Assert
    var serviceProvider = services.BuildServiceProvider();
    var options = serviceProvider.GetRequiredService<IOptionsMonitor<OpenIddictDynamoDbOptions>>().CurrentValue;
    Assert.Equal(throughput, options.ProvisionedThroughput);
  }

  private static OpenIddictDynamoDbBuilder CreateBuilder(IServiceCollection services)
    => services.AddOpenIddict().AddCore().UseDynamoDb().SetDefaultTableName(DatabaseFixture.TableName);

  public class CustomApplication : OpenIddictDynamoDbApplication { }
  public class CustomAuthorization : OpenIddictDynamoDbAuthorization { }
  public class CustomScope : OpenIddictDynamoDbScope { }
  public class CustomToken : OpenIddictDynamoDbToken { }
}
