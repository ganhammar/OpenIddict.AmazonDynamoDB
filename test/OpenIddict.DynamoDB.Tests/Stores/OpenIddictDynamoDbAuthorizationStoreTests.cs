using Amazon.DynamoDBv2.DataModel;
using Xunit;

namespace OpenIddict.DynamoDB.Tests;

[Collection("Sequential")]
public class OpenIddictDynamoDbAuthorizationStoreTests
{
    [Fact]
    public async Task Should_ReturnZero_When_CountingAuthorizationsInEmptyDatabase()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act
            var count = await authorizationStore.CountAsync(CancellationToken.None);

            // Assert
            Assert.Equal(0, count);            
        }
    }

    [Fact]
    public async Task Should_ReturnOne_When_CountingAuthorizationsAfterCreatingOne()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();
            var authorization = new OpenIddictDynamoDbAuthorization();
            await authorizationStore.CreateAsync(authorization, CancellationToken.None);

            // Act
            var count = await authorizationStore.CountAsync(CancellationToken.None);

            // Assert
            Assert.Equal(1, count);            
        }
    }

    [Fact]
    public async Task Should_ThrowNotSupported_When_TryingToCountBasedOnLinq()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NotSupportedException>(async () =>
                await authorizationStore.CountAsync<int>(default!, CancellationToken.None));
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToCreateAuthorizationThatIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await authorizationStore.CreateAsync(default!, CancellationToken.None));
            Assert.Equal("authorization", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_CreateAuthorization_When_AuthorizationIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();
            var authorization = new OpenIddictDynamoDbAuthorization
            {
                ApplicationId = Guid.NewGuid().ToString(),
            };

            // Act
            await authorizationStore.CreateAsync(authorization, CancellationToken.None);

            // Assert
            var databaseAuthorization = await context.LoadAsync<OpenIddictDynamoDbAuthorization>(authorization.Id);
            Assert.NotNull(databaseAuthorization);
            Assert.Equal(authorization.ApplicationId, databaseAuthorization.ApplicationId);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToDeleteAuthorizationThatIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await authorizationStore.DeleteAsync(default!, CancellationToken.None));
            Assert.Equal("authorization", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_DeleteAuthorization_When_AuthorizationIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();
            var authorization = new OpenIddictDynamoDbAuthorization
            {
                ApplicationId = Guid.NewGuid().ToString(),
            };
            await authorizationStore.CreateAsync(authorization, CancellationToken.None);

            // Act
            await authorizationStore.DeleteAsync(authorization, CancellationToken.None);

            // Assert
            var databaseAuthorization = await context.LoadAsync<OpenIddictDynamoDbAuthorization>(authorization.Id);
            Assert.Null(databaseAuthorization);
        }
    }
}