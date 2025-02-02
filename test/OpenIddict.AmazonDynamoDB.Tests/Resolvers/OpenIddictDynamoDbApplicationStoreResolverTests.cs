using Amazon.DynamoDBv2;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;

namespace OpenIddict.AmazonDynamoDB.Tests;

[Collection(Constants.DatabaseCollection)]
public class OpenIddictDynamoDbApplicationStoreResolverTests(DatabaseFixture fixture)
{
  public readonly IAmazonDynamoDB _client = fixture.Client;

  [Fact]
  public void Should_ReturnApplicationStore_When_ItHasBeenRegistered()
  {
    // Arrange
    var serviceCollection = new ServiceCollection();
    serviceCollection.AddSingleton<
      IOpenIddictApplicationStore<OpenIddictDynamoDbApplication>,
      OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>>();
    serviceCollection.AddSingleton(TestUtils.GetOptions(new()
    {
      Database = _client,
    }));
    var serviceProvider = serviceCollection.BuildServiceProvider();
    var resolver = new OpenIddictDynamoDbApplicationStoreResolver(serviceProvider);

    // Act
    var store = resolver.Get<OpenIddictDynamoDbApplication>();

    // Assert
    Assert.NotNull(store);
  }

  [Fact]
  public void Should_ThrowArgumentNullException_When_ServiceProviderIsNull()
  {
    // Arrange, Act & Assert
    Assert.Throws<ArgumentNullException>(() =>
      new OpenIddictDynamoDbApplicationStoreResolver(null!));
  }

  [Fact]
  public void Should_ThrowInvalidOperationException_When_NoImplementationHasBeenRegistered()
  {
    // Arrange
    var serviceCollection = new ServiceCollection();
    var serviceProvider = serviceCollection.BuildServiceProvider();
    var resolver = new OpenIddictDynamoDbApplicationStoreResolver(serviceProvider);

    // Act & Assert
    Assert.Throws<InvalidOperationException>(() =>
      resolver.Get<OpenIddictDynamoDbApplication>());
  }

  [Fact]
  public void Should_ThrowInvalidOperationException_When_TypeIsNotCorrectType()
  {
    // Arrange
    var serviceCollection = new ServiceCollection();
    serviceCollection.AddSingleton<
      IOpenIddictApplicationStore<OpenIddictDynamoDbApplication>,
      OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>>();
    serviceCollection.AddSingleton(TestUtils.GetOptions(new()
    {
      Database = _client,
    }));
    var serviceProvider = serviceCollection.BuildServiceProvider();
    var resolver = new OpenIddictDynamoDbApplicationStoreResolver(serviceProvider);

    // Act & Assert
    var exception = Assert.Throws<InvalidOperationException>(() =>
      resolver.Get<OpenIddictDynamoDbAuthorization>());

    Assert.Equal(OpenIddictResources.GetResourceString(OpenIddictResources.ID0257), exception.Message);
  }
}
