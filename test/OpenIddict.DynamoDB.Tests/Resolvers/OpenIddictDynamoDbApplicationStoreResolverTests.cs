using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using Xunit;

namespace OpenIddict.DynamoDB.Tests;

[Collection("Sequential")]
public class OpenIddictDynamoDbApplicationStoreResolverTests
{
    [Fact]
    public void Should_ReturnApplicationStore_When_ItHasBeenRegistered()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<
                IOpenIddictApplicationStore<OpenIddictDynamoDbApplication>,
                OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>>();
            serviceCollection.AddSingleton<IOptionsMonitor<OpenIddictDynamoDbOptions>>(TestUtils.GetOptions(new()
            {
                Database = database.Client,
            }));
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var resolver = new OpenIddictDynamoDbApplicationStoreResolver(serviceProvider);

            // Act
            var store = resolver.Get<OpenIddictDynamoDbApplication>();

            // Assert
            Assert.NotNull(store);
        }
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
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<
                IOpenIddictApplicationStore<OpenIddictDynamoDbApplication>,
                OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>>();
            serviceCollection.AddSingleton<IOptionsMonitor<OpenIddictDynamoDbOptions>>(TestUtils.GetOptions(new()
            {
                Database = database.Client,
            }));
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var resolver = new OpenIddictDynamoDbApplicationStoreResolver(serviceProvider);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                resolver.Get<OpenIddictDynamoDbAuthorization>());

            Assert.Equal(OpenIddictResources.GetResourceString(OpenIddictResources.ID0257), exception.Message);
        }
    }
}