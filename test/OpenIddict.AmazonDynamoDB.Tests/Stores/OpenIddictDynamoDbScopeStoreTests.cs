using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Amazon.DynamoDBv2.DataModel;
using Xunit;

namespace OpenIddict.AmazonDynamoDB.Tests;

[Collection("Sequential")]
public class OpenIddictDynamoDbScopeStoreTests
{
    [Fact]
    public void Should_ThrowArgumentNullException_When_OptionsIsNotSet()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(null!));

            Assert.Equal("optionsMonitor", exception.ParamName);
        }
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_DatabaseIsNotSet()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(TestUtils.GetOptions(new())));

            Assert.Equal("_openIddictDynamoDbOptions.Database", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ThrowNotSupported_When_TryingToCountBasedOnLinq()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
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
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
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
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
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
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var names = Enumerable.Range(0, 101).Select(index => index.ToString()).ToImmutableArray();
            var exception = Assert.Throws<NotSupportedException>(() =>
                scopeStore.FindByNamesAsync(names, CancellationToken.None));
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindScopeByResourceAndResourceIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                scopeStore.FindByResourceAsync(default!, CancellationToken.None));
            Assert.Equal("resource", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnScope_When_FindingScopesByResourceWithMatch()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            await database.Client.DeleteTableAsync(Constants.DefaultScopeResourceTableName);
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            var resource = "some-resource";
            await scopeStore.CreateAsync(new OpenIddictDynamoDbScope
            {
                Resources = new List<string>
                {
                    resource
                },
            }, CancellationToken.None);

            // Act
            var scopes = scopeStore.FindByResourceAsync(resource, CancellationToken.None);

            // Assert
            var matchedScopes = new List<OpenIddictDynamoDbScope>();
            await foreach (var scope in scopes)
            {
                matchedScopes.Add(scope);
            }
            Assert.Single(matchedScopes);
            Assert.Equal(resource, matchedScopes[0].Resources![0]);
        }
    }

    [Fact]
    public async Task Should_ReturnListOffOne_When_FindingAuthorizationsBySubjectWithMatch()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetDisplayNameAndScopeIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await scopeStore.GetDisplayNameAsync(default!, CancellationToken.None));
            Assert.Equal("scope", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnDisplayName_When_ScopeIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var scope = new OpenIddictDynamoDbScope
            {
                DisplayName = Guid.NewGuid().ToString(),
            };
            await scopeStore.CreateAsync(scope, CancellationToken.None);

            // Act
            var displayName = await scopeStore.GetDisplayNameAsync(scope, CancellationToken.None);

            // Assert
            Assert.NotNull(displayName);
            Assert.Equal(scope.DisplayName, displayName);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetDisplayNamesAndScopeIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await scopeStore.GetDisplayNamesAsync(default!, CancellationToken.None));
            Assert.Equal("scope", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_ScopeDoesntHaveAnyDisplayNames()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var scope = new OpenIddictDynamoDbScope();
            await scopeStore.CreateAsync(scope, CancellationToken.None);

            // Act
            var displayNames = await scopeStore.GetDisplayNamesAsync(scope, CancellationToken.None);

            // Assert
            Assert.Empty(displayNames);
        }
    }

    [Fact]
    public async Task Should_ReturnDisplayNames_When_ScopeHasDisplayNames()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var scope = new OpenIddictDynamoDbScope
            {
                DisplayNames = new Dictionary<string, string>
                {
                    { "sv-SE", "Testar" },
                    { "es-ES", "Testado" },
                    { "en-US", "Testing" },
                },
            };
            await scopeStore.CreateAsync(scope, CancellationToken.None);

            // Act
            var displayNames = await scopeStore.GetDisplayNamesAsync(scope, CancellationToken.None);

            // Assert
            Assert.Equal(3, displayNames.Count);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetDescriptionAndScopeIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await scopeStore.GetDescriptionAsync(default!, CancellationToken.None));
            Assert.Equal("scope", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnDescription_When_ScopeIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var scope = new OpenIddictDynamoDbScope
            {
                Description = Guid.NewGuid().ToString(),
            };
            await scopeStore.CreateAsync(scope, CancellationToken.None);

            // Act
            var description = await scopeStore.GetDescriptionAsync(scope, CancellationToken.None);

            // Assert
            Assert.NotNull(description);
            Assert.Equal(scope.Description, description);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetDescriptionsAndScopeIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await scopeStore.GetDescriptionsAsync(default!, CancellationToken.None));
            Assert.Equal("scope", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_ScopeDoesntHaveAnyDescriptions()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var scope = new OpenIddictDynamoDbScope();
            await scopeStore.CreateAsync(scope, CancellationToken.None);

            // Act
            var descriptions = await scopeStore.GetDescriptionsAsync(scope, CancellationToken.None);

            // Assert
            Assert.Empty(descriptions);
        }
    }

    [Fact]
    public async Task Should_ReturnDescriptions_When_ScopeHasDescriptions()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var scope = new OpenIddictDynamoDbScope
            {
                Descriptions = new Dictionary<string, string>
                {
                    { "sv-SE", "Testar" },
                    { "es-ES", "Testado" },
                    { "en-US", "Testing" },
                },
            };
            await scopeStore.CreateAsync(scope, CancellationToken.None);

            // Act
            var descriptions = await scopeStore.GetDescriptionsAsync(scope, CancellationToken.None);

            // Assert
            Assert.Equal(3, descriptions.Count);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetNameAndScopeIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await scopeStore.GetNameAsync(default!, CancellationToken.None));
            Assert.Equal("scope", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnName_When_ScopeIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var scope = new OpenIddictDynamoDbScope
            {
                Name = Guid.NewGuid().ToString(),
            };
            await scopeStore.CreateAsync(scope, CancellationToken.None);

            // Act
            var name = await scopeStore.GetNameAsync(scope, CancellationToken.None);

            // Assert
            Assert.NotNull(name);
            Assert.Equal(scope.Name, name);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetIdAndScopeIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await scopeStore.GetIdAsync(default!, CancellationToken.None));
            Assert.Equal("scope", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnId_When_ScopeIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var scope = new OpenIddictDynamoDbScope
            {
                Id = Guid.NewGuid().ToString(),
            };
            await scopeStore.CreateAsync(scope, CancellationToken.None);

            // Act
            var id = await scopeStore.GetIdAsync(scope, CancellationToken.None);

            // Assert
            Assert.NotNull(id);
            Assert.Equal(scope.Id, id);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetPropertiesAndScopeIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await scopeStore.GetPropertiesAsync(default!, CancellationToken.None));
            Assert.Equal("scope", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnEmptyDictionary_When_PropertiesIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var scope = new OpenIddictDynamoDbScope();
            await scopeStore.CreateAsync(scope, CancellationToken.None);

            // Act
            var properties = await scopeStore.GetPropertiesAsync(
                scope,
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
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var scope = new OpenIddictDynamoDbScope
            {
                Properties = "{ \"Test\": { \"Something\": true }, \"Testing\": { \"Something\": true }, \"Testicles\": { \"Something\": true } }",
            };
            await scopeStore.CreateAsync(scope, CancellationToken.None);

            // Act
            var properties = await scopeStore.GetPropertiesAsync(
                scope,
                CancellationToken.None);

            // Assert
            Assert.NotNull(properties);
            Assert.Equal(3, properties.Count);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetResourcesAndScopeIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await scopeStore.GetResourcesAsync(default!, CancellationToken.None));
            Assert.Equal("scope", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnEmptyDictionary_When_ResourcesIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var scope = new OpenIddictDynamoDbScope();
            await scopeStore.CreateAsync(scope, CancellationToken.None);

            // Act
            var resources = await scopeStore.GetResourcesAsync(
                scope,
                CancellationToken.None);

            // Assert
            Assert.Empty(resources);
        }
    }

    [Fact]
    public async Task Should_ReturnNonEmptyDictionary_When_ResourcesExists()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var scope = new OpenIddictDynamoDbScope
            {
                Resources = new List<string>
                {
                    "Thing",
                    "Other-Thing",
                    "More-Things",
                },
            };
            await scopeStore.CreateAsync(scope, CancellationToken.None);

            // Act
            var resources = await scopeStore.GetResourcesAsync(
                scope,
                CancellationToken.None);

            // Assert
            Assert.NotEmpty(resources);
            Assert.Equal(3, resources.Length);
        }
    }

    [Fact]
    public async Task Should_ReturnNewScope_When_CallingInstantiate()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act
            var scope = await scopeStore.InstantiateAsync(CancellationToken.None);

            // Assert
            Assert.IsType<OpenIddictDynamoDbScope>(scope);
        }
    }

    [Fact]
    public async Task Should_ReturnList_When_ListingScopes()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            var scopeCount = 10;
            foreach (var index in Enumerable.Range(0, scopeCount))
            {
                await scopeStore.CreateAsync(new OpenIddictDynamoDbScope
                {
                    DisplayName = index.ToString(),
                }, CancellationToken.None);
            }

            // Act
            var scopes = scopeStore.ListAsync(default, default, CancellationToken.None);

            // Assert
            var matchedScopes = new List<OpenIddictDynamoDbScope>();
            await foreach (var scope in scopes)
            {
                matchedScopes.Add(scope);
            }
            Assert.Equal(scopeCount, matchedScopes.Count);
        }
    }

    [Fact]
    public async Task Should_ReturnFirstFive_When_ListingScopesWithCount()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            foreach (var index in Enumerable.Range(0, 10))
            {
                await scopeStore.CreateAsync(new OpenIddictDynamoDbScope
                {
                    DisplayName = index.ToString(),
                }, CancellationToken.None);
            }

            // Act
            var expectedCount = 5;
            var scopes = scopeStore.ListAsync(expectedCount, default, CancellationToken.None);

            // Assert
            var matchedScopes = new List<OpenIddictDynamoDbScope>();
            await foreach (var scope in scopes)
            {
                matchedScopes.Add(scope);
            }
            Assert.Equal(expectedCount, matchedScopes.Count);
        }
    }

    [Fact]
    public async Task Should_ReturnLastFive_When_ListingScopesWithCountAndOffset()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            foreach (var index in Enumerable.Range(0, 10))
            {
                await scopeStore.CreateAsync(new OpenIddictDynamoDbScope
                {
                    DisplayName = index.ToString(),
                }, CancellationToken.None);
            }

            // Act
            var expectedCount = 5;

            // Need to fetch first page to be able to fetch second
            var first = scopeStore.ListAsync(expectedCount, default, CancellationToken.None);
            var firstScopes = new List<OpenIddictDynamoDbScope>();
            await foreach (var scope in first)
            {
                firstScopes.Add(scope);
            }

            var scopes = scopeStore.ListAsync(expectedCount, expectedCount, CancellationToken.None);

            // Assert
            var matchedScopes = new List<OpenIddictDynamoDbScope>();
            await foreach (var scope in scopes)
            {
                matchedScopes.Add(scope);
            }
            Assert.Equal(expectedCount, matchedScopes.Count);
            Assert.Empty(firstScopes.Select(x => x.Id).Intersect(matchedScopes.Select(x => x.Id)));
        }
    }

    [Fact]
    public async Task Should_ThrowNotSupported_When_TryingToFetchWithOffsetWithoutFirstFetchingPreviousPages()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            Assert.Throws<NotSupportedException>(() =>
                scopeStore.ListAsync(5, 5, CancellationToken.None));
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetDescriptionAndScopeIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await scopeStore.SetDescriptionAsync(default!, default, CancellationToken.None));
            Assert.Equal("scope", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetDescription_When_ScopeIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var scope = new OpenIddictDynamoDbScope();

            // Act
            var description = Guid.NewGuid().ToString();
            await scopeStore.SetDescriptionAsync(scope, description, CancellationToken.None);

            // Assert
            Assert.Equal(description, scope.Description);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetDisplayNameAndScopeIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await scopeStore.SetDisplayNameAsync(default!, default, CancellationToken.None));
            Assert.Equal("scope", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetDisplayName_When_ScopeIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var scope = new OpenIddictDynamoDbScope();

            // Act
            var displayName = Guid.NewGuid().ToString();
            await scopeStore.SetDisplayNameAsync(scope, displayName, CancellationToken.None);

            // Assert
            Assert.Equal(displayName, scope.DisplayName);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetNameAndScopeIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await scopeStore.SetNameAsync(default!, default, CancellationToken.None));
            Assert.Equal("scope", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetName_When_ScopeIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var scope = new OpenIddictDynamoDbScope();

            // Act
            var name = Guid.NewGuid().ToString();
            await scopeStore.SetNameAsync(scope, name, CancellationToken.None);

            // Assert
            Assert.Equal(name, scope.Name);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetPropertiesAndScopeIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await scopeStore.SetPropertiesAsync(default!, default!, CancellationToken.None));
            Assert.Equal("scope", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetNull_When_SetEmptyListAsProperties()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var scope = new OpenIddictDynamoDbScope();
            await scopeStore.CreateAsync(scope, CancellationToken.None);

            // Act
            await scopeStore.SetPropertiesAsync(
                scope,
                default!,
                CancellationToken.None);

            // Assert
            Assert.Null(scope.Properties);
        }
    }

    [Fact]
    public async Task Should_SetProperties_When_SettingProperties()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var scope = new OpenIddictDynamoDbScope();
            await scopeStore.CreateAsync(scope, CancellationToken.None);

            // Act
            var properties = new Dictionary<string, JsonElement>
            {
                { "Test", JsonDocument.Parse("{ \"Test\": true }").RootElement },
                { "Testing", JsonDocument.Parse("{ \"Test\": true }").RootElement },
                { "Testicles", JsonDocument.Parse("{ \"Test\": true }").RootElement },
            };
            await scopeStore.SetPropertiesAsync(
                scope,
                properties.ToImmutableDictionary(x => x.Key, x => x.Value),
                CancellationToken.None);

            // Assert
            Assert.NotNull(scope.Properties);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetDisplayNamesAndScopeIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await scopeStore.SetDisplayNamesAsync(default!, default!, CancellationToken.None));
            Assert.Equal("scope", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetNull_When_SetEmptyDictionaryAsDisplayNames()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var scope = new OpenIddictDynamoDbScope();
            await scopeStore.CreateAsync(scope, CancellationToken.None);

            // Act
            await scopeStore.SetDisplayNamesAsync(
                scope,
                ImmutableDictionary.Create<CultureInfo, string>(),
                CancellationToken.None);

            // Assert
            Assert.Null(scope.DisplayNames);
        }
    }

    [Fact]
    public async Task Should_SetDisplayNames_When_SettingDisplayNames()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var scope = new OpenIddictDynamoDbScope();
            await scopeStore.CreateAsync(scope, CancellationToken.None);

            // Act
            var displayNames = new Dictionary<CultureInfo, string>
            {
                { new CultureInfo("sv-SE"), "Testar" },
                { new CultureInfo("es-ES"), "Testado" },
                { new CultureInfo("en-US"), "Testing" },
            };
            await scopeStore.SetDisplayNamesAsync(
                scope,
                displayNames.ToImmutableDictionary(x => x.Key, x => x.Value),
                CancellationToken.None);

            // Assert
            Assert.NotNull(scope.DisplayNames);
            Assert.Equal(3, scope.DisplayNames!.Count);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetDescriptionsAndScopeIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await scopeStore.SetDescriptionsAsync(default!, default!, CancellationToken.None));
            Assert.Equal("scope", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetNull_When_SetEmptyDictionaryAsDescriptions()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var scope = new OpenIddictDynamoDbScope();
            await scopeStore.CreateAsync(scope, CancellationToken.None);

            // Act
            await scopeStore.SetDescriptionsAsync(
                scope,
                ImmutableDictionary.Create<CultureInfo, string>(),
                CancellationToken.None);

            // Assert
            Assert.Null(scope.Descriptions);
        }
    }

    [Fact]
    public async Task Should_SetDescriptions_When_SettingDescriptions()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var scope = new OpenIddictDynamoDbScope();
            await scopeStore.CreateAsync(scope, CancellationToken.None);

            // Act
            var descriptions = new Dictionary<CultureInfo, string>
            {
                { new CultureInfo("sv-SE"), "Testar" },
                { new CultureInfo("es-ES"), "Testado" },
                { new CultureInfo("en-US"), "Testing" },
            };
            await scopeStore.SetDescriptionsAsync(
                scope,
                descriptions.ToImmutableDictionary(x => x.Key, x => x.Value),
                CancellationToken.None);

            // Assert
            Assert.NotNull(scope.Descriptions);
            Assert.Equal(3, scope.Descriptions!.Count);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetResourcesAndScopeIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await scopeStore.SetResourcesAsync(default!, default!, CancellationToken.None));
            Assert.Equal("scope", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetNull_When_SetEmptyDictionaryAsResources()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var scope = new OpenIddictDynamoDbScope();
            await scopeStore.CreateAsync(scope, CancellationToken.None);

            // Act
            await scopeStore.SetResourcesAsync(
                scope,
                ImmutableArray.Create<string>(),
                CancellationToken.None);

            // Assert
            Assert.Null(scope.Resources);
        }
    }

    [Fact]
    public async Task Should_SetResources_When_SettingResources()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var scope = new OpenIddictDynamoDbScope();
            await scopeStore.CreateAsync(scope, CancellationToken.None);

            // Act
            var resources = new List<string>
            {
                "Testar",
                "Testado",
                "Testing",
            };
            await scopeStore.SetResourcesAsync(
                scope,
                resources.ToImmutableArray(),
                CancellationToken.None);

            // Assert
            Assert.NotNull(scope.Resources);
            Assert.Equal(3, scope.Resources!.Count);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToUpdateScopeThatIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await scopeStore.UpdateAsync(default!, CancellationToken.None));
            Assert.Equal("scope", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToUpdateScopeThatDoesntExist()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
                await scopeStore.UpdateAsync(new OpenIddictDynamoDbScope(), CancellationToken.None));
            Assert.Equal("scope", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_ConcurrencyTokenHasChanged()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var scope = new OpenIddictDynamoDbScope();
            await scopeStore.CreateAsync(scope, CancellationToken.None);

            // Act & Assert
            scope.ConcurrencyToken = Guid.NewGuid().ToString();
            var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
                await scopeStore.UpdateAsync(scope, CancellationToken.None));
            Assert.Equal("scope", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_UpdateScope_When_ScopeIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var scope = new OpenIddictDynamoDbScope();
            await scopeStore.CreateAsync(scope, CancellationToken.None);

            // Act
            scope.Name = "testing-to-update";
            await scopeStore.UpdateAsync(scope, CancellationToken.None);

            // Assert
            var databaseScope = await context.LoadAsync<OpenIddictDynamoDbScope>(scope.Id);
            Assert.NotNull(databaseScope);
            Assert.Equal(databaseScope.Name, scope.Name);
        }
    }

    [Fact]
    public async Task Should_UpdateScopeWithResources_When_ResourcesIsSet()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() { Database = database.Client });
            var scopeStore = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var scope = new OpenIddictDynamoDbScope
            {
                Resources = new List<string>
                {
                    "some-resource",
                },
            };
            await scopeStore.CreateAsync(scope, CancellationToken.None);

            // Act
            var resourceName = "some-new-resource";
            scope.Resources = new List<string>
            {
                resourceName,
            };
            await scopeStore.UpdateAsync(scope, CancellationToken.None);

            // Assert
            var updatedScope = await scopeStore.FindByIdAsync(scope.Id, CancellationToken.None);
            Assert.NotNull(updatedScope);
            Assert.NotNull(updatedScope?.Resources);
            Assert.Equal(resourceName, updatedScope!.Resources!.First());
        }
    }
}