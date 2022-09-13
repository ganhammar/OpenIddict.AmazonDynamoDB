using Amazon.DynamoDBv2;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using Xunit;

namespace OpenIddict.DynamoDB.Tests;

[Collection("Sequential")]
public class OpenIddictDynamoDbAuthorizationStoreResolverTests
{
    [Fact]
    public void Should_ReturnAuthorizationStore_When_ItHasBeenRegistered()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<
                IOpenIddictAuthorizationStore<OpenIddictDynamoDbAuthorization>,
                OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>>();
            serviceCollection.AddSingleton<IAmazonDynamoDB>(database.Client);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var resolver = new OpenIddictDynamoDbAuthorizationStoreResolver(serviceProvider);

            // Act
            var store = resolver.Get<OpenIddictDynamoDbAuthorization>();

            // Assert
            Assert.NotNull(store);
        }
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
}