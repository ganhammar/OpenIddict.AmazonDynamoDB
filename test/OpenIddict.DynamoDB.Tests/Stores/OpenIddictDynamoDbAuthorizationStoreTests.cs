using System.Collections.Immutable;
using System.Text.Json;
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

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetTypeAndAuthorizationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await authorizationStore.SetTypeAsync(default!, default, CancellationToken.None));
            Assert.Equal("authorization", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetType_When_AuthorizationIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();
            var authorization = new OpenIddictDynamoDbAuthorization();

            // Act
            var type = "SomeType";
            await authorizationStore.SetTypeAsync(authorization, type, CancellationToken.None);

            // Assert
            Assert.Equal(type, authorization.Type);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetSubjectAndAuthorizationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await authorizationStore.SetSubjectAsync(default!, default, CancellationToken.None));
            Assert.Equal("authorization", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetSubject_When_AuthorizationIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();
            var authorization = new OpenIddictDynamoDbAuthorization();

            // Act
            var subject = "SomeSubject";
            await authorizationStore.SetSubjectAsync(authorization, subject, CancellationToken.None);

            // Assert
            Assert.Equal(subject, authorization.Subject);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetStatusAndAuthorizationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await authorizationStore.SetStatusAsync(default!, default, CancellationToken.None));
            Assert.Equal("authorization", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetStatus_When_AuthorizationIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();
            var authorization = new OpenIddictDynamoDbAuthorization();

            // Act
            var status = "SomeStatus";
            await authorizationStore.SetStatusAsync(authorization, status, CancellationToken.None);

            // Assert
            Assert.Equal(status, authorization.Status);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetScopesAndAuthorizationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await authorizationStore.SetScopesAsync(default!, default!, CancellationToken.None));
            Assert.Equal("authorization", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetNull_When_SetEmptyListAsScopes()
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
            await authorizationStore.SetScopesAsync(
                authorization,
                default,
                CancellationToken.None);

            // Assert
            Assert.Null(authorization.Scopes);
        }
    }

    [Fact]
    public async Task Should_SetScopes_When_SettingScopes()
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
            var redirectUris = new List<string>
            {
                "something",
                "other",
                "some_more",
            };
            await authorizationStore.SetScopesAsync(
                authorization,
                redirectUris.ToImmutableArray(),
                CancellationToken.None);

            // Assert
            Assert.NotNull(authorization.Scopes);
            Assert.Equal(3, authorization.Scopes!.Count);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetPropertiesAndAuthorizationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await authorizationStore.SetPropertiesAsync(default!, default!, CancellationToken.None));
            Assert.Equal("authorization", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetNull_When_SetEmptyListAsProperties()
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
            await authorizationStore.SetPropertiesAsync(
                authorization,
                default!,
                CancellationToken.None);

            // Assert
            Assert.Null(authorization.Properties);
        }
    }

    [Fact]
    public async Task Should_SetProperties_When_SettingProperties()
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
            var properties = new Dictionary<string, JsonElement>
            {
                { "Test", JsonDocument.Parse("{ \"Test\": true }").RootElement },
                { "Testing", JsonDocument.Parse("{ \"Test\": true }").RootElement },
                { "Testicles", JsonDocument.Parse("{ \"Test\": true }").RootElement },
            };
            await authorizationStore.SetPropertiesAsync(
                authorization,
                properties.ToImmutableDictionary(x => x.Key, x => x.Value),
                CancellationToken.None);

            // Assert
            Assert.NotNull(authorization.Properties);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetPropertiesAndAuthorizationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await authorizationStore.GetPropertiesAsync(default!, CancellationToken.None));
            Assert.Equal("authorization", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnEmptyDictionary_When_PropertiesIsNull()
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
            var properties = await authorizationStore.GetPropertiesAsync(
                authorization,
                CancellationToken.None);

            // Assert
            Assert.Empty(properties);
        }
    }

    [Fact]
    public async Task Should_ReturnNonEmptyDictionary_When_PropertiesExists()
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
                Properties = "{ \"Test\": { \"Something\": true }, \"Testing\": { \"Something\": true }, \"Testicles\": { \"Something\": true } }",
            };
            await authorizationStore.CreateAsync(authorization, CancellationToken.None);

            // Act
            var properties = await authorizationStore.GetPropertiesAsync(
                authorization,
                CancellationToken.None);

            // Assert
            Assert.NotNull(properties);
            Assert.Equal(3, properties.Count);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetCreationDateAndAuthorizationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await authorizationStore.SetCreationDateAsync(default!, default, CancellationToken.None));
            Assert.Equal("authorization", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetCreationDate_When_AuthorizationIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();
            var authorization = new OpenIddictDynamoDbAuthorization();

            // Act
            var creationDate = DateTimeOffset.Now;
            await authorizationStore.SetCreationDateAsync(authorization, creationDate, CancellationToken.None);

            // Assert
            Assert.Equal(creationDate.UtcDateTime, authorization.CreationDate);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetApplicationIdAndAuthorizationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await authorizationStore.SetApplicationIdAsync(default!, default, CancellationToken.None));
            Assert.Equal("authorization", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetApplicationId_When_AuthorizationIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();
            var authorization = new OpenIddictDynamoDbAuthorization();

            // Act
            var applicationId = Guid.NewGuid().ToString();
            await authorizationStore.SetApplicationIdAsync(authorization, applicationId, CancellationToken.None);

            // Assert
            Assert.Equal(applicationId, authorization.ApplicationId);
        }
    }

    [Fact]
    public async Task Should_ReturnNewApplication_When_CallingInstantiate()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act
            var authorization = await authorizationStore.InstantiateAsync(CancellationToken.None);

            // Assert
            Assert.IsType<OpenIddictDynamoDbAuthorization>(authorization);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetTypeAndAuthorizationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await authorizationStore.GetTypeAsync(default!, CancellationToken.None));
            Assert.Equal("authorization", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnType_When_AuthorizationIsValid()
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
                Type = "SomeType",
            };
            await authorizationStore.CreateAsync(authorization, CancellationToken.None);

            // Act
            var type = await authorizationStore.GetTypeAsync(authorization, CancellationToken.None);

            // Assert
            Assert.NotNull(type);
            Assert.Equal(authorization.Type, type);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetSubjectAndAuthorizationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await authorizationStore.GetSubjectAsync(default!, CancellationToken.None));
            Assert.Equal("authorization", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnSubject_When_AuthorizationIsValid()
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
                Subject = "SomeSubject",
            };
            await authorizationStore.CreateAsync(authorization, CancellationToken.None);

            // Act
            var subject = await authorizationStore.GetSubjectAsync(authorization, CancellationToken.None);

            // Assert
            Assert.NotNull(subject);
            Assert.Equal(authorization.Subject, subject);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetStatusAndAuthorizationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await authorizationStore.GetStatusAsync(default!, CancellationToken.None));
            Assert.Equal("authorization", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnStatus_When_AuthorizationIsValid()
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
                Status = "SomeStatus",
            };
            await authorizationStore.CreateAsync(authorization, CancellationToken.None);

            // Act
            var subject = await authorizationStore.GetStatusAsync(authorization, CancellationToken.None);

            // Assert
            Assert.NotNull(subject);
            Assert.Equal(authorization.Status, subject);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetIdAndAuthorizationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await authorizationStore.GetIdAsync(default!, CancellationToken.None));
            Assert.Equal("authorization", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnId_When_AuthorizationIsValid()
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
                Id = Guid.NewGuid().ToString(),
            };
            await authorizationStore.CreateAsync(authorization, CancellationToken.None);

            // Act
            var id = await authorizationStore.GetIdAsync(authorization, CancellationToken.None);

            // Assert
            Assert.NotNull(id);
            Assert.Equal(authorization.Id, id);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetCreationDateAndAuthorizationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await authorizationStore.GetCreationDateAsync(default!, CancellationToken.None));
            Assert.Equal("authorization", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnCreationDate_When_AuthorizationIsValid()
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
                CreationDate = DateTime.Now,
            };
            await authorizationStore.CreateAsync(authorization, CancellationToken.None);

            // Act
            var creationDate = await authorizationStore.GetCreationDateAsync(authorization, CancellationToken.None);

            // Assert
            Assert.NotNull(creationDate);
            Assert.Equal(authorization.CreationDate, creationDate);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetApplicationIdAndAuthorizationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await authorizationStore.GetApplicationIdAsync(default!, CancellationToken.None));
            Assert.Equal("authorization", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnApplicationId_When_AuthorizationIsValid()
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
            var applicationId = await authorizationStore.GetApplicationIdAsync(authorization, CancellationToken.None);

            // Assert
            Assert.NotNull(applicationId);
            Assert.Equal(authorization.ApplicationId, applicationId);
        }
    }

    [Fact]
    public async Task Should_ThrowNotSupported_When_TryingToGetBasedOnLinq()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NotSupportedException>(async () =>
                await authorizationStore.GetAsync<int, int>(default!, default!, CancellationToken.None));
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetScopesAndAuthorizationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await authorizationStore.GetScopesAsync(default!, CancellationToken.None));
            Assert.Equal("authorization", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_AuthorizationDoesntHaveAnyScopes()
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
            var postLogoutScopes = await authorizationStore.GetScopesAsync(authorization, CancellationToken.None);

            // Assert
            Assert.Empty(postLogoutScopes);
        }
    }

    [Fact]
    public async Task Should_ReturnScopes_When_AuthorizationHasScopes()
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
                Scopes = new List<string>
                {
                    "get",
                    "set",
                    "delete",
                },
            };
            await authorizationStore.CreateAsync(authorization, CancellationToken.None);

            // Act
            var redirectUris = await authorizationStore.GetScopesAsync(authorization, CancellationToken.None);

            // Assert
            Assert.Equal(3, redirectUris.Length);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindAuthorizationAndSubjectIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                authorizationStore.FindAsync(default!, "test", CancellationToken.None));
            Assert.Equal("subject", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindAuthorizationAndClientIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                authorizationStore.FindAsync("test", default!, CancellationToken.None));
            Assert.Equal("client", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_FindingAuthorizationsWithNoMatch()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act
            var authorizations = authorizationStore.FindAsync("test", "test", CancellationToken.None);

            // Assert
            var matchedAuthorizations = new List<OpenIddictDynamoDbAuthorization>();
            await foreach (var authorization in authorizations)
            {
                matchedAuthorizations.Add(authorization);
            }
            Assert.Empty(matchedAuthorizations);
        }
    }

    [Fact]
    public async Task Should_ReturnListOffOne_When_FindingAuthorizationsWithMatch()
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
                    ApplicationId = index.ToString(),
                }, CancellationToken.None);
            }

            // Act
            var authorizations = authorizationStore.FindAsync("5", "5", CancellationToken.None);

            // Assert
            var matchedAuthorizations = new List<OpenIddictDynamoDbAuthorization>();
            await foreach (var authorization in authorizations)
            {
                matchedAuthorizations.Add(authorization);
            }
            Assert.Single(matchedAuthorizations);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindWithStatusAuthorizationAndSubjectIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                authorizationStore.FindAsync(default!, "test", "test", CancellationToken.None));
            Assert.Equal("subject", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindWithStatusAuthorizationAndClientIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                authorizationStore.FindAsync("test", default!, "test", CancellationToken.None));
            Assert.Equal("client", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindAuthorizationAndStatusIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                authorizationStore.FindAsync("test", "test", default!, CancellationToken.None));
            Assert.Equal("status", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnListOffOne_When_FindingAuthorizationsWithStatusMatch()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            var status = "some-status";
            foreach (var index in Enumerable.Range(0, 10))
            {
                await authorizationStore.CreateAsync(new OpenIddictDynamoDbAuthorization
                {
                    Subject = index.ToString(),
                    ApplicationId = index.ToString(),
                    Status = status,
                }, CancellationToken.None);
            }

            // Act
            var authorizations = authorizationStore.FindAsync("5", "5", status, CancellationToken.None);

            // Assert
            var matchedAuthorizations = new List<OpenIddictDynamoDbAuthorization>();
            await foreach (var authorization in authorizations)
            {
                matchedAuthorizations.Add(authorization);
            }
            Assert.Single(matchedAuthorizations);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindWithTypeAuthorizationAndSubjectIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                authorizationStore.FindAsync(default!, "test", "test", "test", CancellationToken.None));
            Assert.Equal("subject", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindWithTypeAuthorizationAndClientIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                authorizationStore.FindAsync("test", default!, "test", "test", CancellationToken.None));
            Assert.Equal("client", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindWithTypeAuthorizationAndStatusIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                authorizationStore.FindAsync("test", "test", default!, "test", CancellationToken.None));
            Assert.Equal("status", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindWithTypeAuthorizationAndTypeIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                authorizationStore.FindAsync("test", "test", "test", default!, CancellationToken.None));
            Assert.Equal("type", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnListOffOne_When_FindingAuthorizationsWithTypeMatch()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            var status = "some-status";
            var type = "some-type";
            foreach (var index in Enumerable.Range(0, 10))
            {
                await authorizationStore.CreateAsync(new OpenIddictDynamoDbAuthorization
                {
                    Subject = index.ToString(),
                    ApplicationId = index.ToString(),
                    Status = status,
                    Type = type,
                }, CancellationToken.None);
            }

            // Act
            var authorizations = authorizationStore.FindAsync("5", "5", status, type, CancellationToken.None);

            // Assert
            var matchedAuthorizations = new List<OpenIddictDynamoDbAuthorization>();
            await foreach (var authorization in authorizations)
            {
                matchedAuthorizations.Add(authorization);
            }
            Assert.Single(matchedAuthorizations);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindWithScopesAuthorizationAndSubjectIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                authorizationStore.FindAsync(
                    default!,
                    "test",
                    "test",
                    "test",
                    new[] { "get" }.ToImmutableArray(),
                    CancellationToken.None));
            Assert.Equal("subject", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindWithScopesAuthorizationAndClientIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                authorizationStore.FindAsync(
                    "test",
                    default!,
                    "test",
                    "test",
                    new[] { "get" }.ToImmutableArray(),
                    CancellationToken.None));
            Assert.Equal("client", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindWithScopesAuthorizationAndStatusIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                authorizationStore.FindAsync(
                    "test",
                    "test",
                    default!,
                    "test",
                    new[] { "get" }.ToImmutableArray(),
                    CancellationToken.None));
            Assert.Equal("status", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindWithScopesAuthorizationAndTypeIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                authorizationStore.FindAsync(
                    "test",
                    "test",
                    "test",
                    default!,
                    new[] { "get" }.ToImmutableArray(),
                    CancellationToken.None));
            Assert.Equal("type", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindWithScopesAuthorizationAndScopesIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                authorizationStore.FindAsync(
                    "test",
                    "test",
                    "test",
                    "test",
                    default!,
                    CancellationToken.None));
            Assert.Equal("scopes", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnListOffOne_When_FindingAuthorizationsWithScopesMatch()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            var status = "some-status";
            var type = "some-type";
            var scopes = new List<string> { "get" };
            foreach (var index in Enumerable.Range(0, 10))
            {
                await authorizationStore.CreateAsync(new OpenIddictDynamoDbAuthorization
                {
                    Subject = index.ToString(),
                    ApplicationId = index.ToString(),
                    Status = status,
                    Type = type,
                    Scopes = scopes,
                }, CancellationToken.None);
            }

            // Act
            var authorizations = authorizationStore.FindAsync(
                "5",
                "5",
                status,
                type,
                scopes.ToImmutableArray(),
                CancellationToken.None);

            // Assert
            var matchedAuthorizations = new List<OpenIddictDynamoDbAuthorization>();
            await foreach (var authorization in authorizations)
            {
                matchedAuthorizations.Add(authorization);
            }
            Assert.Single(matchedAuthorizations);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindAuthorizationByApplicationIdAndIdentifierIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                authorizationStore.FindByApplicationIdAsync(default!, CancellationToken.None));
            Assert.Equal("identifier", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_FindingAuthorizationsByApplicationIdWithNoMatch()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act
            var authorizations = authorizationStore.FindByApplicationIdAsync(
                Guid.NewGuid().ToString(), CancellationToken.None);

            // Assert
            var matchedAuthorizations = new List<OpenIddictDynamoDbAuthorization>();
            await foreach (var authorization in authorizations)
            {
                matchedAuthorizations.Add(authorization);
            }
            Assert.Empty(matchedAuthorizations);
        }
    }

    [Fact]
    public async Task Should_ReturnListOffOne_When_FindingAuthorizationsByApplicationIdWithMatch()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            var applicationId = Guid.NewGuid().ToString();
            var authorizationCount = 10;
            foreach (var index in Enumerable.Range(0, authorizationCount))
            {
                await authorizationStore.CreateAsync(new OpenIddictDynamoDbAuthorization
                {
                    Subject = index.ToString(),
                    ApplicationId = applicationId,
                }, CancellationToken.None);
            }

            // Act
            var authorizations = authorizationStore.FindByApplicationIdAsync(
                applicationId, CancellationToken.None);

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
    public async Task Should_ThrowException_When_TryingToFindAuthorizationBySubjectAndSubjectIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                authorizationStore.FindBySubjectAsync(default!, CancellationToken.None));
            Assert.Equal("subject", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_FindingAuthorizationsBySubjectWithNoMatch()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act
            var authorizations = authorizationStore.FindBySubjectAsync(
                Guid.NewGuid().ToString(), CancellationToken.None);

            // Assert
            var matchedAuthorizations = new List<OpenIddictDynamoDbAuthorization>();
            await foreach (var authorization in authorizations)
            {
                matchedAuthorizations.Add(authorization);
            }
            Assert.Empty(matchedAuthorizations);
        }
    }

    [Fact]
    public async Task Should_ReturnListOffOne_When_FindingAuthorizationsBySubjectWithMatch()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            var subject = Guid.NewGuid().ToString();
            var authorizationCount = 10;
            foreach (var index in Enumerable.Range(0, authorizationCount))
            {
                await authorizationStore.CreateAsync(new OpenIddictDynamoDbAuthorization
                {
                    ApplicationId = index.ToString(),
                    Subject = subject,
                }, CancellationToken.None);
            }

            // Act
            var authorizations = authorizationStore.FindBySubjectAsync(
                subject, CancellationToken.None);

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
    public async Task Should_ThrowException_When_TryingToFindAuthorizationByIdAndIdentifierIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await authorizationStore.FindByIdAsync(default!, CancellationToken.None));
            Assert.Equal("identifier", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnAuthorization_When_FindingAuthorizationsBySubjectWithNoMatch()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(
                database.Client);
            await authorizationStore.EnsureInitializedAsync();

            var id = Guid.NewGuid().ToString();
            await authorizationStore.CreateAsync(new OpenIddictDynamoDbAuthorization
            {
                Id = id,
            }, CancellationToken.None);

            // Act
            var authorization = await authorizationStore.FindByIdAsync(id, CancellationToken.None);

            // Assert
            Assert.NotNull(authorization);
            Assert.Equal(id, authorization!.Id);
        }
    }
}