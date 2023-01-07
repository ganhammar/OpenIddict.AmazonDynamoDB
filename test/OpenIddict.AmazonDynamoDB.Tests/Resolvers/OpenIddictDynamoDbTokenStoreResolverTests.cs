using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using Xunit;

namespace OpenIddict.AmazonDynamoDB.Tests;

[Collection("Sequential")]
public class OpenIddictDynamoDbTokenStoreResolverTests
{
  [Fact]
  public void Should_ReturnTokenStore_When_ItHasBeenRegistered()
  {
    using (var database = DynamoDbLocalServerUtils.CreateDatabase())
    {
      // Arrange
      var serviceCollection = new ServiceCollection();
      serviceCollection.AddSingleton<
          IOpenIddictTokenStore<OpenIddictDynamoDbToken>,
          OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>>();
      serviceCollection.AddSingleton<IOptionsMonitor<OpenIddictDynamoDbOptions>>(TestUtils.GetOptions(new()
      {
        Database = database.Client,
      }));
      var serviceProvider = serviceCollection.BuildServiceProvider();
      var resolver = new OpenIddictDynamoDbTokenStoreResolver(serviceProvider);

      // Act
      var store = resolver.Get<OpenIddictDynamoDbToken>();

      // Assert
      Assert.NotNull(store);
    }
  }

  [Fact]
  public void Should_ThrowArgumentNullException_When_ServiceProviderIsNull()
  {
    // Arrange, Act & Assert
    Assert.Throws<ArgumentNullException>(() =>
        new OpenIddictDynamoDbTokenStoreResolver(null!));
  }

  [Fact]
  public void Should_ThrowInvalidOperationException_When_NoImplementationHasBeenRegistered()
  {
    // Arrange
    var serviceCollection = new ServiceCollection();
    var serviceProvider = serviceCollection.BuildServiceProvider();
    var resolver = new OpenIddictDynamoDbTokenStoreResolver(serviceProvider);

    // Act & Assert
    Assert.Throws<InvalidOperationException>(() =>
        resolver.Get<OpenIddictDynamoDbToken>());
  }

  [Fact]
  public void Should_ThrowInvalidOperationException_When_TypeIsNotCorrectType()
  {
    using (var database = DynamoDbLocalServerUtils.CreateDatabase())
    {
      // Arrange
      var serviceCollection = new ServiceCollection();
      serviceCollection.AddSingleton<
          IOpenIddictTokenStore<OpenIddictDynamoDbToken>,
          OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>>();
      serviceCollection.AddSingleton<IOptionsMonitor<OpenIddictDynamoDbOptions>>(TestUtils.GetOptions(new()
      {
        Database = database.Client,
      }));
      var serviceProvider = serviceCollection.BuildServiceProvider();
      var resolver = new OpenIddictDynamoDbTokenStoreResolver(serviceProvider);

      // Act & Assert
      var exception = Assert.Throws<InvalidOperationException>(() =>
          resolver.Get<OpenIddictDynamoDbAuthorization>());

      Assert.Equal(OpenIddictResources.GetResourceString(OpenIddictResources.ID0260), exception.Message);
    }
  }
}
