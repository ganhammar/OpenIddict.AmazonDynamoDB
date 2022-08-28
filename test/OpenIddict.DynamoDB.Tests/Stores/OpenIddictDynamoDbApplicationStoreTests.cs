using Amazon.DynamoDBv2.DataModel;
using Xunit;

namespace OpenIddict.DynamoDB.Tests;

public class OpenIddictDynamoDbApplicationStoreTests
{
    [Fact]
    public async Task Should_ReturnZero_When_CountingApplicationsInEmptyDatabase()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();

            // Act
            var count = await applicationStore.CountAsync(CancellationToken.None);

            // Assert
            Assert.Equal(0, count);            
        }
    }

    [Fact]
    public async Task Should_ReturnOne_When_CountingApplicationsAfterCreatingOne()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();
            var application = new OpenIddictDynamoDbApplication
            {
                ClientId = Guid.NewGuid().ToString(),
            };
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var count = await applicationStore.CountAsync(CancellationToken.None);

            // Assert
            Assert.Equal(1, count);            
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToCreateApplicationThatIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.CreateAsync((OpenIddictDynamoDbApplication)null!, CancellationToken.None));
            Assert.Equal("application", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_CreateApplication_When_ApplicationIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();
            var application = new OpenIddictDynamoDbApplication
            {
                ClientId = Guid.NewGuid().ToString(),
            };

            // Act
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Assert
            var databaseApplication = await context.LoadAsync<OpenIddictDynamoDbApplication>(application.Id);
            Assert.NotNull(databaseApplication);
            Assert.Equal(application.ClientId, databaseApplication.ClientId);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToDeleteApplicationThatIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.DeleteAsync((OpenIddictDynamoDbApplication)null!, CancellationToken.None));
            Assert.Equal("application", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_DeleteApplication_When_ApplicationIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();
            var application = new OpenIddictDynamoDbApplication
            {
                ClientId = Guid.NewGuid().ToString(),
            };
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            await applicationStore.DeleteAsync(application, CancellationToken.None);

            // Assert
            var databaseApplication = await context.LoadAsync<OpenIddictDynamoDbApplication>(application.Id);
            Assert.Null(databaseApplication);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindApplicationWithoutIdentifier()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.FindByIdAsync((string)null!, CancellationToken.None));
            Assert.Equal("identifier", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_NotThrowException_When_TryingToFindApplicationWithIdentifierThatDoesntExist()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();

            // Act
            var application = await applicationStore.FindByIdAsync("doesnt-exist", CancellationToken.None);

            // Assert
            Assert.Null(application);
        }
    }

    [Fact]
    public async Task Should_ReturnApplication_When_TryingToFindApplicationByExistingIdentifier()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();
            var application = new OpenIddictDynamoDbApplication
            {
                ClientId = Guid.NewGuid().ToString(),
            };
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var result = await applicationStore.FindByIdAsync(application.Id, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(application.ClientId, result!.ClientId);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindApplicationByClientIdWithoutIdentifier()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.FindByClientIdAsync((string)null!, CancellationToken.None));
            Assert.Equal("identifier", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_NotThrowException_When_TryingToFindApplicationByClientIdWithIdentifierThatDoesntExist()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();

            // Act
            var application = await applicationStore.FindByClientIdAsync("doesnt-exist", CancellationToken.None);

            // Assert
            Assert.Null(application);
        }
    }

    [Fact]
    public async Task Should_ReturnApplication_When_TryingToFindApplicationByClientIdByExistingIdentifier()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();
            var application = new OpenIddictDynamoDbApplication
            {
                ClientId = Guid.NewGuid().ToString(),
            };
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var result = await applicationStore.FindByClientIdAsync(application.ClientId!, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(application.ClientId, result!.ClientId);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindApplicationByRedirectUriWithoutAddress()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                applicationStore.FindByRedirectUriAsync((string)null!, CancellationToken.None));
            Assert.Equal("address", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_NotThrowException_When_TryingToFindApplicationByRedirectUriWithAddressThatDoesntExist()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();

            // Act
            var applications = applicationStore.FindByRedirectUriAsync("doesnt-exist", CancellationToken.None);

            // Assert
            var matchedApplications = new List<OpenIddictDynamoDbApplication>();
            await foreach (var application in applications)
            {
                matchedApplications.Add(application);
            }
            Assert.Empty(matchedApplications);
        }
    }

    [Fact]
    public async Task Should_ReturnApplication_When_TryingToFindApplicationByRedirectUriByExistingAddress()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();
            var redirectUri = "http://test.com/test/redirect";
            var application = new OpenIddictDynamoDbApplication
            {
                RedirectUris = new List<string> { redirectUri },
            };
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var applications = applicationStore.FindByRedirectUriAsync(redirectUri, CancellationToken.None);

            // Assert
            var matchedApplications = new List<OpenIddictDynamoDbApplication>();
            await foreach (var matchedApplication in applications)
            {
                matchedApplications.Add(matchedApplication);
            }
            Assert.NotEmpty(matchedApplications);
            Assert.Single(matchedApplications);
            Assert.Equal(matchedApplications[0].Id, application.Id);
        }
    }

    [Fact]
    public async Task Should_ReturnApplication_When_TryingToFindApplicationByRedirectUriByExistingAddressAmongOthers()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();
            var redirectUri = "http://test.com/test/redirect";
            var application = new OpenIddictDynamoDbApplication
            {
                RedirectUris = new List<string>
                {
                    "http://test.com/test/redirect1",
                    redirectUri,
                    "http://test.com/test/redirect2",
                },
            };
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var applications = applicationStore.FindByRedirectUriAsync(redirectUri, CancellationToken.None);

            // Assert
            var matchedApplications = new List<OpenIddictDynamoDbApplication>();
            await foreach (var matchedApplication in applications)
            {
                matchedApplications.Add(matchedApplication);
            }
            Assert.NotEmpty(matchedApplications);
            Assert.Single(matchedApplications);
            Assert.Equal(matchedApplications[0].Id, application.Id);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindApplicationByPostLogoutRedirectUriWithoutAddress()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                applicationStore.FindByPostLogoutRedirectUriAsync((string)null!, CancellationToken.None));
            Assert.Equal("address", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_NotThrowException_When_TryingToFindApplicationByPostLogoutRedirectUriWithAddressThatDoesntExist()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();

            // Act
            var applications = applicationStore.FindByPostLogoutRedirectUriAsync("doesnt-exist", CancellationToken.None);

            // Assert
            var matchedApplications = new List<OpenIddictDynamoDbApplication>();
            await foreach (var application in applications)
            {
                matchedApplications.Add(application);
            }
            Assert.Empty(matchedApplications);
        }
    }

    [Fact]
    public async Task Should_ReturnApplication_When_TryingToFindApplicationByPostLogoutRedirectUriByExistingAddress()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();
            var redirectUri = "http://test.com/test/redirect";
            var application = new OpenIddictDynamoDbApplication
            {
                PostLogoutRedirectUris = new List<string> { redirectUri },
            };
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var applications = applicationStore.FindByPostLogoutRedirectUriAsync(redirectUri, CancellationToken.None);

            // Assert
            var matchedApplications = new List<OpenIddictDynamoDbApplication>();
            await foreach (var matchedApplication in applications)
            {
                matchedApplications.Add(matchedApplication);
            }
            Assert.NotEmpty(matchedApplications);
            Assert.Single(matchedApplications);
            Assert.Equal(matchedApplications[0].Id, application.Id);
        }
    }

    [Fact]
    public async Task Should_ReturnApplication_When_TryingToFindApplicationByPostLogoutRedirectUriByExistingAddressAmongOthers()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();
            var redirectUri = "http://test.com/test/redirect";
            var application = new OpenIddictDynamoDbApplication
            {
                PostLogoutRedirectUris = new List<string>
                {
                    "http://test.com/test/redirect1",
                    redirectUri,
                    "http://test.com/test/redirect2",
                },
            };
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var applications = applicationStore.FindByPostLogoutRedirectUriAsync(redirectUri, CancellationToken.None);

            // Assert
            var matchedApplications = new List<OpenIddictDynamoDbApplication>();
            await foreach (var matchedApplication in applications)
            {
                matchedApplications.Add(matchedApplication);
            }
            Assert.NotEmpty(matchedApplications);
            Assert.Single(matchedApplications);
            Assert.Equal(matchedApplications[0].Id, application.Id);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_ToUpdateApplicationThatIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.UpdateAsync((OpenIddictDynamoDbApplication)null!, CancellationToken.None));
            Assert.Equal("application", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToUpdateApplicationThatDoesntExist()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
                await applicationStore.UpdateAsync(new OpenIddictDynamoDbApplication(), CancellationToken.None));
            Assert.Equal("application", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_ConcurrencyTokenHasChanged()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();
            var application = new OpenIddictDynamoDbApplication();
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act & Assert
            application.ConcurrencyToken = Guid.NewGuid().ToString();
            var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
                await applicationStore.UpdateAsync(application, CancellationToken.None));
            Assert.Equal("application", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_UpdateApplication_When_ApplicationIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();
            var application = new OpenIddictDynamoDbApplication();
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            application.DisplayName = "Testing To Update";
            await applicationStore.UpdateAsync(application, CancellationToken.None);

            // Assert
            var databaseApplication = await context.LoadAsync<OpenIddictDynamoDbApplication>(application.Id);
            Assert.NotNull(databaseApplication);
            Assert.Equal(databaseApplication.DisplayName, application.DisplayName);
        }
    }

    [Fact]
    public async Task Should_UpdateApplicationWithRedirectUris_When_ApplicationIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();
            var application = new OpenIddictDynamoDbApplication
            {
                RedirectUris = new List<string>
                {
                    "http://test.com/return",
                },
                PostLogoutRedirectUris = new List<string>
                {
                    "http://test.com/return",
                },
            };
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var redirectUri = "http://test.com/return/again";
            application.RedirectUris = new List<string>
            {
                redirectUri,
            };
            await applicationStore.UpdateAsync(application, CancellationToken.None);

            // Assert
            var updatedApplication = await applicationStore.FindByIdAsync(application.Id, CancellationToken.None);
            Assert.NotNull(updatedApplication);
            Assert.Equal(updatedApplication!.RedirectUris!.First(), redirectUri);
            Assert.Single(updatedApplication.RedirectUris);
            Assert.Single(updatedApplication.PostLogoutRedirectUris);
        }
    }
}