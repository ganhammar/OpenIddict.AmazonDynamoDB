using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Amazon.DynamoDBv2.DataModel;
using Xunit;

namespace OpenIddict.DynamoDB.Tests;

[Collection("Sequential")]
public class OpenIddictDynamoDbApplicationStoreTests
{
    [Fact]
    public void Should_ThrowArgumentNullException_When_OptionsIsNotSet()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(null!));

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
                new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(TestUtils.GetOptions(new())));

            Assert.Equal(nameof(OpenIddictDynamoDbOptions.Database), exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnZero_When_CountingApplicationsInEmptyDatabase()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
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
    public async Task Should_ThrowNotSupported_When_TryingToCountBasedOnLinq()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            await Assert.ThrowsAsync<NotSupportedException>(async () =>
                await applicationStore.CountAsync(x => x.Where(y => y.DisplayName == "Test"), CancellationToken.None));
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToCreateApplicationThatIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.CreateAsync(default!, CancellationToken.None));
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
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
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
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.DeleteAsync(default!, CancellationToken.None));
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
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
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
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
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
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
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
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
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
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
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
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
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
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
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
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.UpdateAsync(default!, CancellationToken.None));
            Assert.Equal("application", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToUpdateApplicationThatDoesntExist()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
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
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
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
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
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

    [Fact]
    public async Task Should_ThrowNotSupported_When_TryingToGetBasedOnLinq()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            await Assert.ThrowsAsync<NotSupportedException>(async () =>
                await applicationStore.GetAsync<int, int>(default!, default, CancellationToken.None));
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetClientIdAndApplicationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.GetClientIdAsync(default!, CancellationToken.None));
            Assert.Equal("application", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnClientId_When_ApplicationIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication
            {
                ClientId = Guid.NewGuid().ToString(),
            };
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var clientId = await applicationStore.GetClientIdAsync(application, CancellationToken.None);

            // Assert
            Assert.NotNull(clientId);
            Assert.Equal(application.ClientId, clientId);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetClientSecretAndApplicationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.GetClientSecretAsync(default!, CancellationToken.None));
            Assert.Equal("application", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnClientSecret_When_ApplicationIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication
            {
                ClientSecret = Guid.NewGuid().ToString(),
            };
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var clientSecret = await applicationStore.GetClientSecretAsync(application, CancellationToken.None);

            // Assert
            Assert.NotNull(clientSecret);
            Assert.Equal(application.ClientSecret, clientSecret);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetClientTypeAndApplicationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.GetClientTypeAsync(default!, CancellationToken.None));
            Assert.Equal("application", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnClientType_When_ApplicationIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication
            {
                Type = Guid.NewGuid().ToString(),
            };
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var clientType = await applicationStore.GetClientTypeAsync(application, CancellationToken.None);

            // Assert
            Assert.NotNull(clientType);
            Assert.Equal(application.Type, clientType);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetConsentTypeAndApplicationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.GetConsentTypeAsync(default!, CancellationToken.None));
            Assert.Equal("application", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnConsentType_When_ApplicationIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication
            {
                ConsentType = Guid.NewGuid().ToString(),
            };
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var consentType = await applicationStore.GetConsentTypeAsync(application, CancellationToken.None);

            // Assert
            Assert.NotNull(consentType);
            Assert.Equal(application.ConsentType, consentType);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetDisplayNameAndApplicationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.GetDisplayNameAsync(default!, CancellationToken.None));
            Assert.Equal("application", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnDisplayName_When_ApplicationIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication
            {
                DisplayName = Guid.NewGuid().ToString(),
            };
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var displayName = await applicationStore.GetDisplayNameAsync(application, CancellationToken.None);

            // Assert
            Assert.NotNull(displayName);
            Assert.Equal(application.DisplayName, displayName);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetIdAndApplicationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.GetIdAsync(default!, CancellationToken.None));
            Assert.Equal("application", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnId_When_ApplicationIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication();
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var id = await applicationStore.GetIdAsync(application, CancellationToken.None);

            // Assert
            Assert.NotNull(id);
            Assert.Equal(application.Id, id);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetClientIdAndApplicationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.SetClientIdAsync(default!, default, CancellationToken.None));
            Assert.Equal("application", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetClientId_When_ApplicationIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication();

            // Act
            var clientId = Guid.NewGuid().ToString();
            await applicationStore.SetClientIdAsync(application, clientId, CancellationToken.None);

            // Assert
            Assert.Equal(clientId, application.ClientId);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetClientSecretAndApplicationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.SetClientSecretAsync(default!, default, CancellationToken.None));
            Assert.Equal("application", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetClientSecret_When_ApplicationIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication();

            // Act
            var clientSecret = Guid.NewGuid().ToString();
            await applicationStore.SetClientSecretAsync(application, clientSecret, CancellationToken.None);

            // Assert
            Assert.Equal(clientSecret, application.ClientSecret);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetClientTypeAndApplicationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.SetClientTypeAsync(default!, default, CancellationToken.None));
            Assert.Equal("application", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetClientType_When_ApplicationIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication();

            // Act
            var clientType = Guid.NewGuid().ToString();
            await applicationStore.SetClientTypeAsync(application, clientType, CancellationToken.None);

            // Assert
            Assert.Equal(clientType, application.Type);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetConsentTypeAndApplicationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.SetConsentTypeAsync(default!, default, CancellationToken.None));
            Assert.Equal("application", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetConsentType_When_ApplicationIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication();

            // Act
            var consentType = Guid.NewGuid().ToString();
            await applicationStore.SetConsentTypeAsync(application, consentType, CancellationToken.None);

            // Assert
            Assert.Equal(consentType, application.ConsentType);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetDisplayNameAndApplicationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.SetDisplayNameAsync(default!, default, CancellationToken.None));
            Assert.Equal("application", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetDisplayName_When_ApplicationIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication();

            // Act
            var displayName = Guid.NewGuid().ToString();
            await applicationStore.SetDisplayNameAsync(application, displayName, CancellationToken.None);

            // Assert
            Assert.Equal(displayName, application.DisplayName);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetPermissionsAndApplicationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.GetPermissionsAsync(default!, CancellationToken.None));
            Assert.Equal("application", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_ApplicationDoesntHaveAnyPermissions()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication();
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var permissions = await applicationStore.GetPermissionsAsync(application, CancellationToken.None);

            // Assert
            Assert.Empty(permissions);
        }
    }

    [Fact]
    public async Task Should_ReturnPermissions_When_ApplicationHasPermissions()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication
            {
                Permissions = new List<string>
                {
                    "Get",
                    "Set",
                    "And Other Things",
                },
            };
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var permissions = await applicationStore.GetPermissionsAsync(application, CancellationToken.None);

            // Assert
            Assert.Equal(3, permissions.Length);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetPostLogoutRedirectUrisAndApplicationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.GetPostLogoutRedirectUrisAsync(default!, CancellationToken.None));
            Assert.Equal("application", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_ApplicationDoesntHaveAnyPostLogoutRedirectUris()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication();
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var postLogoutRedirectUris = await applicationStore.GetPostLogoutRedirectUrisAsync(application, CancellationToken.None);

            // Assert
            Assert.Empty(postLogoutRedirectUris);
        }
    }

    [Fact]
    public async Task Should_ReturnPostLogoutRedirectUris_When_ApplicationHasPostLogoutRedirectUris()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication
            {
                PostLogoutRedirectUris = new List<string>
                {
                    "https://test.io/logout",
                    "https://test.io/logout/even/more",
                    "https://test.io/logout/login/noop/logout",
                },
            };
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var postLogoutRedirectUris = await applicationStore.GetPostLogoutRedirectUrisAsync(application, CancellationToken.None);

            // Assert
            Assert.Equal(3, postLogoutRedirectUris.Length);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetRedirectUrisAndApplicationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.GetRedirectUrisAsync(default!, CancellationToken.None));
            Assert.Equal("application", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_ApplicationDoesntHaveAnyRedirectUris()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication();
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var postLogoutRedirectUris = await applicationStore.GetRedirectUrisAsync(application, CancellationToken.None);

            // Assert
            Assert.Empty(postLogoutRedirectUris);
        }
    }

    [Fact]
    public async Task Should_ReturnRedirectUris_When_ApplicationHasRedirectUris()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication
            {
                RedirectUris = new List<string>
                {
                    "https://test.io/login",
                    "https://test.io/login/even/more",
                    "https://test.io/login/logout/noop/login",
                },
            };
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var redirectUris = await applicationStore.GetRedirectUrisAsync(application, CancellationToken.None);

            // Assert
            Assert.Equal(3, redirectUris.Length);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetRequirementsAndApplicationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.GetRequirementsAsync(default!, CancellationToken.None));
            Assert.Equal("application", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_ApplicationDoesntHaveAnyRequirements()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication();
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var requirements = await applicationStore.GetRequirementsAsync(application, CancellationToken.None);

            // Assert
            Assert.Empty(requirements);
        }
    }

    [Fact]
    public async Task Should_ReturnRequirements_When_ApplicationHasRequirements()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication
            {
                Requirements = new List<string>
                {
                    "Do",
                    "Dont",
                    "Doer",
                },
            };
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var requirements = await applicationStore.GetRequirementsAsync(application, CancellationToken.None);

            // Assert
            Assert.Equal(3, requirements.Length);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetDisplayNamesAndApplicationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.GetDisplayNamesAsync(default!, CancellationToken.None));
            Assert.Equal("application", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_ApplicationDoesntHaveAnyDisplayNames()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication();
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var displayNames = await applicationStore.GetDisplayNamesAsync(application, CancellationToken.None);

            // Assert
            Assert.Empty(displayNames);
        }
    }

    [Fact]
    public async Task Should_ReturnDisplayNames_When_ApplicationHasDisplayNames()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication
            {
                DisplayNames = new Dictionary<string, string>
                {
                    { "sv-SE", "Testar" },
                    { "es-ES", "Testado" },
                    { "en-US", "Testing" },
                },
            };
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var displayNames = await applicationStore.GetDisplayNamesAsync(application, CancellationToken.None);

            // Assert
            Assert.Equal(3, displayNames.Count);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetDisplayNamesAndApplicationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.SetDisplayNamesAsync(default!, default!, CancellationToken.None));
            Assert.Equal("application", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetNull_When_SetEmptyDictionaryAsDisplayNames()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication();
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            await applicationStore.SetDisplayNamesAsync(
                application,
                ImmutableDictionary.Create<CultureInfo, string>(),
                CancellationToken.None);

            // Assert
            Assert.Null(application.DisplayNames);
        }
    }

    [Fact]
    public async Task Should_SetDisplayNames_When_SettingDisplayNames()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication();
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var displayNames = new Dictionary<CultureInfo, string>
            {
                { new CultureInfo("sv-SE"), "Testar" },
                { new CultureInfo("es-ES"), "Testado" },
                { new CultureInfo("en-US"), "Testing" },
            };
            await applicationStore.SetDisplayNamesAsync(
                application,
                displayNames.ToImmutableDictionary(x => x.Key, x => x.Value),
                CancellationToken.None);

            // Assert
            Assert.NotNull(application.DisplayNames);
            Assert.Equal(3, application.DisplayNames!.Count);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetPermissionsAndApplicationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.SetPermissionsAsync(default!, default!, CancellationToken.None));
            Assert.Equal("application", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetNull_When_SetEmptyListAsPermissions()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication();
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            await applicationStore.SetPermissionsAsync(
                application,
                default,
                CancellationToken.None);

            // Assert
            Assert.Null(application.Permissions);
        }
    }

    [Fact]
    public async Task Should_SetPermissions_When_SettingPermissions()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication();
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var permissions = new List<string>
            {
                "Get",
                "Set",
                "And Other Things",
            };
            await applicationStore.SetPermissionsAsync(
                application,
                permissions.ToImmutableArray(),
                CancellationToken.None);

            // Assert
            Assert.NotNull(application.Permissions);
            Assert.Equal(3, application.Permissions!.Count);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetPostLogoutRedirectUrisAndApplicationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.SetPostLogoutRedirectUrisAsync(default!, default!, CancellationToken.None));
            Assert.Equal("application", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetNull_When_SetEmptyListAsPostLogoutRedirectUris()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication();
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            await applicationStore.SetPostLogoutRedirectUrisAsync(
                application,
                default,
                CancellationToken.None);

            // Assert
            Assert.Null(application.PostLogoutRedirectUris);
        }
    }

    [Fact]
    public async Task Should_SetPostLogoutRedirectUris_When_SettingPostLogoutRedirectUris()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication();
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var postLogoutRedirectUris = new List<string>
            {
                "https://test.io/logout",
                "https://test.io/logout/even/more",
                "https://test.io/logout/login/noop/logout",
            };
            await applicationStore.SetPostLogoutRedirectUrisAsync(
                application,
                postLogoutRedirectUris.ToImmutableArray(),
                CancellationToken.None);

            // Assert
            Assert.NotNull(application.PostLogoutRedirectUris);
            Assert.Equal(3, application.PostLogoutRedirectUris!.Count);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetRedirectUrisAndApplicationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.SetRedirectUrisAsync(default!, default!, CancellationToken.None));
            Assert.Equal("application", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetNull_When_SetEmptyListAsRedirectUris()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication();
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            await applicationStore.SetRedirectUrisAsync(
                application,
                default,
                CancellationToken.None);

            // Assert
            Assert.Null(application.RedirectUris);
        }
    }

    [Fact]
    public async Task Should_SetRedirectUris_When_SettingRedirectUris()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication();
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var redirectUris = new List<string>
            {
                "https://test.io/login",
                "https://test.io/login/even/more",
                "https://test.io/login/logout/noop/login",
            };
            await applicationStore.SetRedirectUrisAsync(
                application,
                redirectUris.ToImmutableArray(),
                CancellationToken.None);

            // Assert
            Assert.NotNull(application.RedirectUris);
            Assert.Equal(3, application.RedirectUris!.Count);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetRequirementsAndApplicationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.SetRequirementsAsync(default!, default!, CancellationToken.None));
            Assert.Equal("application", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetNull_When_SetEmptyListAsRequirements()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication();
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            await applicationStore.SetRequirementsAsync(
                application,
                default,
                CancellationToken.None);

            // Assert
            Assert.Null(application.Requirements);
        }
    }

    [Fact]
    public async Task Should_SetRequirements_When_SettingRequirements()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication();
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var requirements = new List<string>
            {
                "Do",
                "Dont",
                "Doer",
            };
            await applicationStore.SetRequirementsAsync(
                application,
                requirements.ToImmutableArray(),
                CancellationToken.None);

            // Assert
            Assert.NotNull(application.Requirements);
            Assert.Equal(3, application.Requirements!.Count);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetPropertiesAndApplicationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.SetPropertiesAsync(default!, default!, CancellationToken.None));
            Assert.Equal("application", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetNull_When_SetEmptyListAsProperties()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication();
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            await applicationStore.SetPropertiesAsync(
                application,
                default!,
                CancellationToken.None);

            // Assert
            Assert.Null(application.Properties);
        }
    }

    [Fact]
    public async Task Should_SetProperties_When_SettingProperties()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication();
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var properties = new Dictionary<string, JsonElement>
            {
                { "Test", JsonDocument.Parse("{ \"Test\": true }").RootElement },
                { "Testing", JsonDocument.Parse("{ \"Test\": true }").RootElement },
                { "Testicles", JsonDocument.Parse("{ \"Test\": true }").RootElement },
            };
            await applicationStore.SetPropertiesAsync(
                application,
                properties.ToImmutableDictionary(x => x.Key, x => x.Value),
                CancellationToken.None);

            // Assert
            Assert.NotNull(application.Properties);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetPropertiesAndApplicationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.GetPropertiesAsync(default!, CancellationToken.None));
            Assert.Equal("application", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnEmptyDictionary_When_PropertiesIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication();
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var properties = await applicationStore.GetPropertiesAsync(
                application,
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
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
            var application = new OpenIddictDynamoDbApplication
            {
                Properties = "{ \"Test\": { \"Something\": true }, \"Testing\": { \"Something\": true }, \"Testicles\": { \"Something\": true } }",
            };
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var properties = await applicationStore.GetPropertiesAsync(
                application,
                CancellationToken.None);

            // Assert
            Assert.NotNull(properties);
            Assert.Equal(3, properties.Count);
        }
    }

    [Fact]
    public async Task Should_ReturnNewApplication_When_CallingInstantiate()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act
            var application = await applicationStore.InstantiateAsync(CancellationToken.None);

            // Assert
            Assert.IsType<OpenIddictDynamoDbApplication>(application);
        }
    }

    [Fact]
    public async Task Should_ThrowNotSupported_When_TryingToListBasedOnLinq()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            Assert.Throws<NotSupportedException>(() =>
                applicationStore.ListAsync<int, int>(default!, default, CancellationToken.None));
        }
    }

    [Fact]
    public async Task Should_ReturnList_When_ListingApplications()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            var applicationCount = 10;
            foreach (var index in Enumerable.Range(0, applicationCount))
            {
                await applicationStore.CreateAsync(new OpenIddictDynamoDbApplication
                {
                    DisplayName = index.ToString(),
                }, CancellationToken.None);
            }

            // Act
            var applications = applicationStore.ListAsync(default, default, CancellationToken.None);

            // Assert
            var matchedApplications = new List<OpenIddictDynamoDbApplication>();
            await foreach (var application in applications)
            {
                matchedApplications.Add(application);
            }
            Assert.Equal(applicationCount, matchedApplications.Count);
        }
    }

    [Fact]
    public async Task Should_ReturnFirstFive_When_ListingApplicationsWithCount()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            foreach (var index in Enumerable.Range(0, 10))
            {
                await applicationStore.CreateAsync(new OpenIddictDynamoDbApplication
                {
                    DisplayName = index.ToString(),
                }, CancellationToken.None);
            }

            // Act
            var expectedCount = 5;
            var applications = applicationStore.ListAsync(expectedCount, default, CancellationToken.None);

            // Assert
            var matchedApplications = new List<OpenIddictDynamoDbApplication>();
            await foreach (var application in applications)
            {
                matchedApplications.Add(application);
            }
            Assert.Equal(expectedCount, matchedApplications.Count);
        }
    }

    [Fact]
    public async Task Should_ReturnLastFive_When_ListingApplicationsWithCountAndOffset()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            foreach (var index in Enumerable.Range(0, 10))
            {
                await applicationStore.CreateAsync(new OpenIddictDynamoDbApplication
                {
                    DisplayName = index.ToString(),
                }, CancellationToken.None);
            }

            // Act
            var expectedCount = 5;

            // Need to fetch first page to be able to fetch second
            var first = applicationStore.ListAsync(expectedCount, default, CancellationToken.None);
            var firstApplications = new List<OpenIddictDynamoDbApplication>();
            await foreach (var application in first)
            {
                firstApplications.Add(application);
            }

            var applications = applicationStore.ListAsync(expectedCount, expectedCount, CancellationToken.None);

            // Assert
            var matchedApplications = new List<OpenIddictDynamoDbApplication>();
            await foreach (var application in applications)
            {
                matchedApplications.Add(application);
            }
            Assert.Equal(expectedCount, matchedApplications.Count);
            Assert.Empty(firstApplications.Select(x => x.Id).Intersect(matchedApplications.Select(x => x.Id)));
        }
    }

    [Fact]
    public async Task Should_ThrowNotSupported_When_TryingToFetchWithOffsetWithoutFirstFetchingPreviousPages()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new() {  Database = database.Client });
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Act & Assert
            Assert.Throws<NotSupportedException>(() =>
                applicationStore.ListAsync(5, 5, CancellationToken.None));
        }
    }
}