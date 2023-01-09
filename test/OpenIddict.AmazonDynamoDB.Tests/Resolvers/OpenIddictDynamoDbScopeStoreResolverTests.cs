using Amazon.DynamoDBv2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace OpenIddict.AmazonDynamoDB.Tests;

[Collection(Constants.DatabaseCollection)]
public class OpenIddictDynamoDbScopeStoreResolverTests
{
  public readonly IAmazonDynamoDB _client;

  public OpenIddictDynamoDbScopeStoreResolverTests(DatabaseFixture fixture) => _client = fixture.Client;

  [Fact]
  public void Should_ReturnScopeStore_When_ItHasBeenRegistered()
  {
    // Arrange
    var serviceCollection = new ServiceCollection();
    serviceCollection.AddSingleton<
      IOpenIddictScopeStore<OpenIddictDynamoDbScope>,
      OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>>();
    serviceCollection.AddSingleton<IOptionsMonitor<OpenIddictDynamoDbOptions>>(TestUtils.GetOptions(new()
    {
      Database = _client,
    }));
    var serviceProvider = serviceCollection.BuildServiceProvider();
    var resolver = new OpenIddictDynamoDbScopeStoreResolver(serviceProvider);

    // Act
    var store = resolver.Get<OpenIddictDynamoDbScope>();

    // Assert
    Assert.NotNull(store);
  }

  [Fact]
  public void Should_ThrowArgumentNullException_When_ServiceProviderIsNull()
  {
    // Arrange, Act & Assert
    Assert.Throws<ArgumentNullException>(() =>
      new OpenIddictDynamoDbScopeStoreResolver(null!));
  }

  [Fact]
  public void Should_ThrowInvalidOperationException_When_NoImplementationHasBeenRegistered()
  {
    // Arrange
    var serviceCollection = new ServiceCollection();
    var serviceProvider = serviceCollection.BuildServiceProvider();
    var resolver = new OpenIddictDynamoDbScopeStoreResolver(serviceProvider);

    // Act & Assert
    Assert.Throws<InvalidOperationException>(() =>
      resolver.Get<OpenIddictDynamoDbScope>());
  }

  [Fact]
  public void Should_ThrowInvalidOperationException_When_TypeIsNotCorrectType()
  {
    // Arrange
    var serviceCollection = new ServiceCollection();
    serviceCollection.AddSingleton<
      IOpenIddictScopeStore<OpenIddictDynamoDbScope>,
      OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>>();
    serviceCollection.AddSingleton<IOptionsMonitor<OpenIddictDynamoDbOptions>>(TestUtils.GetOptions(new()
    {
      Database = _client,
    }));
    var serviceProvider = serviceCollection.BuildServiceProvider();
    var resolver = new OpenIddictDynamoDbScopeStoreResolver(serviceProvider);

    // Act & Assert
    var exception = Assert.Throws<InvalidOperationException>(() =>
      resolver.Get<OpenIddictDynamoDbAuthorization>());

    Assert.Equal(OpenIddictResources.GetResourceString(OpenIddictResources.ID0259), exception.Message);
  }
}
