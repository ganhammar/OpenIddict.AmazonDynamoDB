using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using Xunit;

namespace OpenIddict.DynamoDB.Tests;

[Collection("Sequential")]
public class OpenIddictDynamoDbScopeStoreResolverTests
{
    [Fact]
    public void Should_ReturnScopeStore_When_ItHasBeenRegistered()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<
                IOpenIddictScopeStore<OpenIddictDynamoDbScope>,
                OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>>();
            serviceCollection.AddSingleton<OpenIddictDynamoDbOptions>(new OpenIddictDynamoDbOptions
            {
                Database = database.Client,
            });
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var resolver = new OpenIddictDynamoDbScopeStoreResolver(serviceProvider);

            // Act
            var store = resolver.Get<OpenIddictDynamoDbScope>();

            // Assert
            Assert.NotNull(store);
        }
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
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<
                IOpenIddictScopeStore<OpenIddictDynamoDbScope>,
                OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>>();
            serviceCollection.AddSingleton<OpenIddictDynamoDbOptions>(new OpenIddictDynamoDbOptions
            {
                Database = database.Client,
            });
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var resolver = new OpenIddictDynamoDbScopeStoreResolver(serviceProvider);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                resolver.Get<OpenIddictDynamoDbAuthorization>());

            Assert.Equal(OpenIddictResources.GetResourceString(OpenIddictResources.ID0259), exception.Message);
        }
    }
}