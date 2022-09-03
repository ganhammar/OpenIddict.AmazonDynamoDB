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

    [Fact]
    public async Task Should_ThrowNotSupported_When_TryingToListBasedOnLinq()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            Assert.Throws<NotSupportedException>(() =>
                authorizationStore.ListAsync<int, int>(default!, default, CancellationToken.None));
        }
    }

    [Fact]
    public async Task Should_ReturnList_When_ListingAuthorizations()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            var authorizationCount = 10;
            foreach (var index in Enumerable.Range(0, authorizationCount))
            {
                await authorizationStore.CreateAsync(new OpenIddictDynamoDbAuthorization
                {
                    Subject = index.ToString(),
                }, CancellationToken.None);
            }

            // Act
            var authorizations = authorizationStore.ListAsync(default, default, CancellationToken.None);

            // Assert
            var matchedAuthorizations = new List<OpenIddictDynamoDbAuthorization>();
            await foreach (var authorization in authorizations)
            {
                matchedAuthorizations.Add(authorization);
            }
            Assert.Equal(authorizationCount, matchedAuthorizations.Count);
        }
    }

    [Fact]
    public async Task Should_ReturnFirstFive_When_ListingAuthorizationsWithCount()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            foreach (var index in Enumerable.Range(0, 10))
            {
                await authorizationStore.CreateAsync(new OpenIddictDynamoDbAuthorization
                {
                    Subject = index.ToString(),
                }, CancellationToken.None);
            }

            // Act
            var expectedCount = 5;
            var authorizations = authorizationStore.ListAsync(expectedCount, default, CancellationToken.None);

            // Assert
            var matchedAuthorizations = new List<OpenIddictDynamoDbAuthorization>();
            await foreach (var authorization in authorizations)
            {
                matchedAuthorizations.Add(authorization);
            }
            Assert.Equal(expectedCount, matchedAuthorizations.Count);
        }
    }

    [Fact]
    public async Task Should_ReturnLastFive_When_ListingAuthorizationsWithCountAndOffset()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            foreach (var index in Enumerable.Range(0, 10))
            {
                await authorizationStore.CreateAsync(new OpenIddictDynamoDbAuthorization
                {
                    Subject = index.ToString(),
                }, CancellationToken.None);
            }

            // Act
            var expectedCount = 5;

            // Need to fetch first page to be able to fetch second
            var first = authorizationStore.ListAsync(expectedCount, default, CancellationToken.None);
            var firstAuthorizations = new List<OpenIddictDynamoDbAuthorization>();
            await foreach (var authorization in first)
            {
                firstAuthorizations.Add(authorization);
            }

            var authorizations = authorizationStore.ListAsync(expectedCount, expectedCount, CancellationToken.None);

            // Assert
            var matchedAuthorizations = new List<OpenIddictDynamoDbAuthorization>();
            await foreach (var authorization in authorizations)
            {
                matchedAuthorizations.Add(authorization);
            }
            Assert.Equal(expectedCount, matchedAuthorizations.Count);
            Assert.Empty(firstAuthorizations.Select(x => x.Id).Intersect(matchedAuthorizations.Select(x => x.Id)));
        }
    }

    [Fact]
    public async Task Should_ThrowNotSupported_When_TryingToFetchWithOffsetWithoutFirstFetchingPreviousPages()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            Assert.Throws<NotSupportedException>(() =>
                authorizationStore.ListAsync(5, 5, CancellationToken.None));
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_ToUpdateAuthorizationThatIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await authorizationStore.UpdateAsync(default!, CancellationToken.None));
            Assert.Equal("authorization", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToUpdateAuthorizationThatDoesntExist()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
                await authorizationStore.UpdateAsync(new OpenIddictDynamoDbAuthorization(), CancellationToken.None));
            Assert.Equal("authorization", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_ConcurrencyTokenHasChanged()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();
            var authorization = new OpenIddictDynamoDbAuthorization();
            await authorizationStore.CreateAsync(authorization, CancellationToken.None);

            // Act & Assert
            authorization.ConcurrencyToken = Guid.NewGuid().ToString();
            var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
                await authorizationStore.UpdateAsync(authorization, CancellationToken.None));
            Assert.Equal("authorization", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_UpdateAuthorization_When_AuthorizationIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();
            var authorization = new OpenIddictDynamoDbAuthorization();
            await authorizationStore.CreateAsync(authorization, CancellationToken.None);

            // Act
            authorization.Subject = "testing-to-update";
            await authorizationStore.UpdateAsync(authorization, CancellationToken.None);

            // Assert
            var databaseAuthorization = await context.LoadAsync<OpenIddictDynamoDbAuthorization>(authorization.Id);
            Assert.NotNull(databaseAuthorization);
            Assert.Equal(databaseAuthorization.Subject, authorization.Subject);
        }
    }
}