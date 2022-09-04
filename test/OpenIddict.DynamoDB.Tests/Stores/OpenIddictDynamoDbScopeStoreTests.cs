using Amazon.DynamoDBv2.DataModel;
using Xunit;

namespace OpenIddict.DynamoDB.Tests;

public class OpenIddictDynamoDbScopeStoreTests
{
    [Fact]
    public async Task Should_ThrowNotSupported_When_TryingToCountBasedOnLinq()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(
                database.Client);
            await scopeStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NotSupportedException>(async () =>
                await scopeStore.CountAsync<int>(default!, CancellationToken.None));
        }
    }

    [Fact]
    public async Task Should_ThrowNotSupported_When_TryingToListBasedOnLinq()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(
                database.Client);
            await scopeStore.EnsureInitializedAsync();

            // Act & Assert
            Assert.Throws<NotSupportedException>(() =>
                scopeStore.ListAsync<int, int>(default!, default, CancellationToken.None));
        }
    }

    [Fact]
    public async Task Should_ThrowNotSupported_When_TryingToGetBasedOnLinq()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(
                database.Client);
            await scopeStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NotSupportedException>(async () =>
                await scopeStore.GetAsync<int, int>(default!, default!, CancellationToken.None));
        }
    }

    [Fact]
    public async Task Should_ReturnZero_When_CountingScopesInEmptyDatabase()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(
                database.Client);
            await scopeStore.EnsureInitializedAsync();

            // Act
            var count = await scopeStore.CountAsync(CancellationToken.None);

            // Assert
            Assert.Equal(0, count);            
        }
    }

    [Fact]
    public async Task Should_ReturnOne_When_CountingScopesAfterCreatingOne()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(
                database.Client);
            await scopeStore.EnsureInitializedAsync();
            var scope = new OpenIddictDynamoDbScope();
            await scopeStore.CreateAsync(scope, CancellationToken.None);

            // Act
            var count = await scopeStore.CountAsync(CancellationToken.None);

            // Assert
            Assert.Equal(1, count);            
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToCreateScopeThatIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(
                database.Client);
            await scopeStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await scopeStore.CreateAsync(default!, CancellationToken.None));
            Assert.Equal("scope", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_CreateScope_When_ScopeIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(
                database.Client);
            await scopeStore.EnsureInitializedAsync();
            var scope = new OpenIddictDynamoDbScope
            {
                DisplayName = Guid.NewGuid().ToString(),
            };

            // Act
            await scopeStore.CreateAsync(scope, CancellationToken.None);

            // Assert
            var databaseScope = await context.LoadAsync<OpenIddictDynamoDbScope>(scope.Id);
            Assert.NotNull(databaseScope);
            Assert.Equal(scope.DisplayName, databaseScope.DisplayName);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToDeleteScopeThatIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(
                database.Client);
            await scopeStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await scopeStore.DeleteAsync(default!, CancellationToken.None));
            Assert.Equal("scope", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_DeleteScope_When_ScopeIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(
                database.Client);
            await scopeStore.EnsureInitializedAsync();
            var scope = new OpenIddictDynamoDbScope();
            await scopeStore.CreateAsync(scope, CancellationToken.None);

            // Act
            await scopeStore.DeleteAsync(scope, CancellationToken.None);

            // Assert
            var databaseScope = await context.LoadAsync<OpenIddictDynamoDbScope>(scope.Id);
            Assert.Null(databaseScope);
        }
    }
}