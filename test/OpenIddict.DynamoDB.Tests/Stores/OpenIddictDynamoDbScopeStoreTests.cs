using System.Collections.Immutable;
using Amazon.DynamoDBv2.DataModel;
using Xunit;

namespace OpenIddict.DynamoDB.Tests;

[Collection("Sequential")]
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

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindScopeByIdAndIdentifierIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(
                database.Client);
            await scopeStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await scopeStore.FindByIdAsync(default!, CancellationToken.None));
            Assert.Equal("identifier", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnScope_When_FindingScopesBySubjectWithMatch()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(
                database.Client);
            await scopeStore.EnsureInitializedAsync();

            var id = Guid.NewGuid().ToString();
            await scopeStore.CreateAsync(new OpenIddictDynamoDbScope
            {
                Id = id,
            }, CancellationToken.None);

            // Act
            var scope = await scopeStore.FindByIdAsync(id, CancellationToken.None);

            // Assert
            Assert.NotNull(scope);
            Assert.Equal(id, scope!.Id);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindScopeByNameAndNameIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(
                database.Client);
            await scopeStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await scopeStore.FindByNameAsync(default!, CancellationToken.None));
            Assert.Equal("name", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnScope_When_FindingScopesByNameWithMatch()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(
                database.Client);
            await scopeStore.EnsureInitializedAsync();

            var name = "some-scope";
            await scopeStore.CreateAsync(new OpenIddictDynamoDbScope
            {
                Name = name,
            }, CancellationToken.None);

            // Act
            var scope = await scopeStore.FindByNameAsync(name, CancellationToken.None);

            // Assert
            Assert.NotNull(scope);
            Assert.Equal(name, scope!.Name);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindScopeByNamesAndNamesIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(
                database.Client);
            await scopeStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                scopeStore.FindByNamesAsync(default!, CancellationToken.None));
            Assert.Equal("names", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindScopeByNamesAndNamesIsMoreThanAHundredItems()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(
                database.Client);
            await scopeStore.EnsureInitializedAsync();

            // Act & Assert
            var names = Enumerable.Range(0, 101).Select(index => index.ToString()).ToImmutableArray();
            var exception = Assert.Throws<NotSupportedException>(() =>
                scopeStore.FindByNamesAsync(names, CancellationToken.None));
        }
    }

    [Fact]
    public async Task Should_ReturnListOffOne_When_FindingAuthorizationsBySubjectWithMatch()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(
                database.Client);
            await scopeStore.EnsureInitializedAsync();

            var subject = Guid.NewGuid().ToString();
            var scopeCount = 10;
            var names = Enumerable.Range(0, scopeCount).Select(x => x.ToString()).ToImmutableArray();
            foreach (var name in names)
            {
                await scopeStore.CreateAsync(new OpenIddictDynamoDbScope
                {
                    Name = name,
                }, CancellationToken.None);
            }

            // Act
            var scopes = scopeStore.FindByNamesAsync(
                names, CancellationToken.None);

            // Assert
            var matchedScopes = new List<OpenIddictDynamoDbScope>();
            await foreach (var scope in scopes)
            {
                matchedScopes.Add(scope);
            }
            Assert.Equal(scopeCount, matchedScopes.Count);
        }
    }
}