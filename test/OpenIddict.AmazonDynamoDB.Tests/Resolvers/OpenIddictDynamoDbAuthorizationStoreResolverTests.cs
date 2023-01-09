using Amazon.DynamoDBv2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using Xunit;

namespace OpenIddict.AmazonDynamoDB.Tests;

[Collection(Constants.DatabaseCollection)]
public class OpenIddictDynamoDbAuthorizationStoreResolverTests
{
  public readonly IAmazonDynamoDB _client;

  public OpenIddictDynamoDbAuthorizationStoreResolverTests(DatabaseFixture fixture) => _client = fixture.Client;

  [Fact]
  public void Should_ReturnAuthorizationStore_When_ItHasBeenRegistered()
  {
    // Arrange
    var serviceCollection = new ServiceCollection();
    serviceCollection.AddSingleton<
      IOpenIddictAuthorizationStore<OpenIddictDynamoDbAuthorization>,
      OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>>();
    serviceCollection.AddSingleton<IOptionsMonitor<OpenIddictDynamoDbOptions>>(TestUtils.GetOptions(new()
    {
      Database = _client,
    }));
    var serviceProvider = serviceCollection.BuildServiceProvider();
    var resolver = new OpenIddictDynamoDbAuthorizationStoreResolver(serviceProvider);

    // Act
    var store = resolver.Get<OpenIddictDynamoDbAuthorization>();

    // Assert
    Assert.NotNull(store);
  }

  [Fact]
  public void Should_ThrowArgumentNullException_When_ServiceProviderIsNull()
  {
    // Arrange, Act & Assert
    Assert.Throws<ArgumentNullException>(() =>
      new OpenIddictDynamoDbAuthorizationStoreResolver(null!));
  }

  [Fact]
  public void Should_ThrowInvalidOperationException_When_NoImplementationHasBeenRegistered()
  {
    // Arrange
    var serviceCollection = new ServiceCollection();
    var serviceProvider = serviceCollection.BuildServiceProvider();
    var resolver = new OpenIddictDynamoDbAuthorizationStoreResolver(serviceProvider);

    // Act & Assert
    Assert.Throws<InvalidOperationException>(() =>
      resolver.Get<OpenIddictDynamoDbAuthorization>());
  }

  [Fact]
  public void Should_ThrowInvalidOperationException_When_TypeIsNotCorrectType()
  {
    // Arrange
    var serviceCollection = new ServiceCollection();
    serviceCollection.AddSingleton<
      IOpenIddictAuthorizationStore<OpenIddictDynamoDbAuthorization>,
      OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>>();
    serviceCollection.AddSingleton<IOptionsMonitor<OpenIddictDynamoDbOptions>>(TestUtils.GetOptions(new()
    {
      Database = _client,
    }));
    var serviceProvider = serviceCollection.BuildServiceProvider();
    var resolver = new OpenIddictDynamoDbAuthorizationStoreResolver(serviceProvider);

    // Act & Assert
    var exception = Assert.Throws<InvalidOperationException>(() =>
      resolver.Get<OpenIddictDynamoDbScope>());

    Assert.Equal(OpenIddictResources.GetResourceString(OpenIddictResources.ID0258), exception.Message);
  }
}
