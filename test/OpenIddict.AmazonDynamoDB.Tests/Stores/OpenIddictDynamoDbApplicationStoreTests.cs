using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.IdentityModel.Tokens;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OpenIddict.AmazonDynamoDB.Tests;

[Collection(Constants.DatabaseCollection)]
public class OpenIddictDynamoDbApplicationStoreTests
{
  public readonly IAmazonDynamoDB _client;

  public OpenIddictDynamoDbApplicationStoreTests(DatabaseFixture fixture) => _client = fixture.Client;

  [Fact]
  public void Should_ThrowArgumentNullException_When_OptionsIsNotSet()
  {
    // Arrange, Act & Assert
    var exception = Assert.Throws<ArgumentNullException>(() =>
      new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(null!));

    Assert.Equal("optionsMonitor", exception.ParamName);
  }

  [Fact]
  public void Should_ThrowArgumentNullException_When_DatabaseIsNotSet()
  {
    // Arrange, Act & Assert
    var exception = Assert.Throws<ArgumentNullException>(() =>
      new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(TestUtils.GetOptions(new())));

    Assert.Equal("options.Database", exception.ParamName);
  }

  [Fact]
  public async Task Should_GetDatabaseFromServiceProvider_When_DatabaseIsNullInOptions()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new());
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options, _client);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options, _client);

    // Act
    var application = new OpenIddictDynamoDbApplication();
    await applicationStore.CreateAsync(application, CancellationToken.None);

    // Assert
    var databaseApplication = await context.LoadAsync<OpenIddictDynamoDbApplication>(
      application.PartitionKey, application.SortKey);
    Assert.NotNull(databaseApplication);
  }

  [Fact]
  public async Task Should_IncreaseCount_When_CountingApplicationsAfterCreatingOne()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var application = new OpenIddictDynamoDbApplication
    {
      ClientId = Guid.NewGuid().ToString(),
    };
    var beforeCount = await applicationStore.CountAsync(CancellationToken.None);
    await applicationStore.CreateAsync(application, CancellationToken.None);

    // Act
    var count = await applicationStore.CountAsync(CancellationToken.None);

    // Assert
    Assert.Equal(beforeCount + 1, count);
  }

  [Fact]
  public async Task Should_ThrowNotSupported_When_TryingToCountBasedOnLinq()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    await Assert.ThrowsAsync<NotSupportedException>(async () =>
      await applicationStore.CountAsync(x => x.Where(y => y.DisplayName == "Test"), CancellationToken.None));
  }

  [Fact]
  public async Task Should_ThrowException_When_TryingToCreateApplicationThatIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.CreateAsync(default!, CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_CreateApplication_When_ApplicationIsValid()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var application = new OpenIddictDynamoDbApplication
    {
      ClientId = Guid.NewGuid().ToString(),
    };

    // Act
    await applicationStore.CreateAsync(application, CancellationToken.None);

    // Assert
    var databaseApplication = await context.LoadAsync<OpenIddictDynamoDbApplication>(
      application.PartitionKey, application.SortKey);
    Assert.NotNull(databaseApplication);
    Assert.Equal(application.ClientId, databaseApplication.ClientId);
  }

  [Fact]
  public async Task Should_ThrowException_When_TryingToDeleteApplicationThatIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.DeleteAsync(default!, CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_DeleteApplication_When_ApplicationIsValid()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
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
    var databaseApplication = await context.LoadAsync<OpenIddictDynamoDbApplication>(
      application.PartitionKey, application.SortKey);
    Assert.Null(databaseApplication);
  }

  [Fact]
  public async Task Should_ThrowException_When_TryingToFindApplicationWithoutIdentifier()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.FindByIdAsync((string)null!, CancellationToken.None));
    Assert.Equal("identifier", exception.ParamName);
  }

  [Fact]
  public async Task Should_NotThrowException_When_TryingToFindApplicationWithIdentifierThatDoesntExist()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act
    var application = await applicationStore.FindByIdAsync("doesnt-exist", CancellationToken.None);

    // Assert
    Assert.Null(application);
  }

  [Fact]
  public async Task Should_ReturnApplication_When_TryingToFindApplicationByExistingIdentifier()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_ThrowException_When_TryingToFindApplicationByClientIdWithoutIdentifier()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.FindByClientIdAsync((string)null!, CancellationToken.None));
    Assert.Equal("identifier", exception.ParamName);
  }

  [Fact]
  public async Task Should_NotThrowException_When_TryingToFindApplicationByClientIdWithIdentifierThatDoesntExist()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act
    var application = await applicationStore.FindByClientIdAsync("doesnt-exist", CancellationToken.None);

    // Assert
    Assert.Null(application);
  }

  [Fact]
  public async Task Should_ReturnApplication_When_TryingToFindApplicationByClientIdByExistingIdentifier()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_ThrowException_When_TryingToFindApplicationByRedirectUriWithoutAddress()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = Assert.Throws<ArgumentException>(() =>
      applicationStore.FindByRedirectUriAsync((string)null!, CancellationToken.None));
    Assert.Equal("address", exception.ParamName);
  }

  [Fact]
  public async Task Should_NotThrowException_When_TryingToFindApplicationByRedirectUriWithAddressThatDoesntExist()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_ReturnApplication_When_TryingToFindApplicationByRedirectUriByExistingAddress()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var redirectUri = $"http://test.com/test/redirect/{Guid.NewGuid()}";
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

  [Fact]
  public async Task Should_ReturnApplication_When_TryingToFindApplicationByRedirectUriByExistingAddressAmongOthers()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var redirectUri = $"http://test.com/test/redirect/{Guid.NewGuid()}";
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

  [Fact]
  public async Task Should_ThrowException_When_TryingToFindApplicationByPostLogoutRedirectUriWithoutAddress()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = Assert.Throws<ArgumentException>(() =>
      applicationStore.FindByPostLogoutRedirectUriAsync((string)null!, CancellationToken.None));
    Assert.Equal("address", exception.ParamName);
  }

  [Fact]
  public async Task Should_NotThrowException_When_TryingToFindApplicationByPostLogoutRedirectUriWithAddressThatDoesntExist()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_ReturnApplication_When_TryingToFindApplicationByPostLogoutRedirectUriByExistingAddress()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var redirectUri = $"http://test.com/test/redirect/{Guid.NewGuid()}";
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

  [Fact]
  public async Task Should_ReturnApplication_When_TryingToFindApplicationByPostLogoutRedirectUriByExistingAddressAmongOthers()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_ThrowException_When_ToUpdateApplicationThatIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.UpdateAsync(default!, CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_ThrowException_When_TryingToUpdateApplicationThatDoesntExist()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
      await applicationStore.UpdateAsync(new OpenIddictDynamoDbApplication(), CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_ThrowException_When_ConcurrencyTokenHasChanged()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_UpdateApplication_When_ApplicationIsValid()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var application = new OpenIddictDynamoDbApplication();
    await applicationStore.CreateAsync(application, CancellationToken.None);

    // Act
    application.DisplayName = "Testing To Update";
    await applicationStore.UpdateAsync(application, CancellationToken.None);

    // Assert
    var databaseApplication = await context.LoadAsync<OpenIddictDynamoDbApplication>(
      application.PartitionKey, application.SortKey);
    Assert.NotNull(databaseApplication);
    Assert.Equal(databaseApplication.DisplayName, application.DisplayName);
  }

  [Fact]
  public async Task Should_UpdateApplicationWithRedirectUris_When_ApplicationIsValid()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
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
    Assert.Single(updatedApplication.RedirectUris!);
    Assert.Single(updatedApplication.PostLogoutRedirectUris!);
  }

  [Fact]
  public async Task Should_ThrowNotSupported_When_TryingToGetBasedOnLinq()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    await Assert.ThrowsAsync<NotSupportedException>(async () =>
      await applicationStore.GetAsync<int, int>(default!, default, CancellationToken.None));
  }

  [Fact]
  public async Task Should_ThrowException_When_TryingToGetClientIdAndApplicationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.GetClientIdAsync(default!, CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_ReturnClientId_When_ApplicationIsValid()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_ThrowException_When_TryingToGetClientSecretAndApplicationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.GetClientSecretAsync(default!, CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_ReturnClientSecret_When_ApplicationIsValid()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_ThrowException_When_TryingToGetClientTypeAndApplicationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.GetClientTypeAsync(default!, CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_ReturnClientType_When_ApplicationIsValid()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_ThrowException_When_TryingToGetConsentTypeAndApplicationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.GetConsentTypeAsync(default!, CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_ReturnConsentType_When_ApplicationIsValid()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_ThrowException_When_TryingToGetDisplayNameAndApplicationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.GetDisplayNameAsync(default!, CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_ReturnDisplayName_When_ApplicationIsValid()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_ThrowException_When_TryingToGetIdAndApplicationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.GetIdAsync(default!, CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_ReturnId_When_ApplicationIsValid()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_ThrowException_When_TryingToSetClientIdAndApplicationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.SetClientIdAsync(default!, default, CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_SetClientId_When_ApplicationIsValid()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var application = new OpenIddictDynamoDbApplication();

    // Act
    var clientId = Guid.NewGuid().ToString();
    await applicationStore.SetClientIdAsync(application, clientId, CancellationToken.None);

    // Assert
    Assert.Equal(clientId, application.ClientId);
  }

  [Fact]
  public async Task Should_ThrowException_When_TryingToSetClientSecretAndApplicationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.SetClientSecretAsync(default!, default, CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_SetClientSecret_When_ApplicationIsValid()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var application = new OpenIddictDynamoDbApplication();

    // Act
    var clientSecret = Guid.NewGuid().ToString();
    await applicationStore.SetClientSecretAsync(application, clientSecret, CancellationToken.None);

    // Assert
    Assert.Equal(clientSecret, application.ClientSecret);
  }

  [Fact]
  public async Task Should_ThrowException_When_TryingToSetClientTypeAndApplicationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.SetClientTypeAsync(default!, default, CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_SetClientType_When_ApplicationIsValid()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var application = new OpenIddictDynamoDbApplication();

    // Act
    var clientType = Guid.NewGuid().ToString();
    await applicationStore.SetClientTypeAsync(application, clientType, CancellationToken.None);

    // Assert
    Assert.Equal(clientType, application.Type);
  }

  [Fact]
  public async Task Should_ThrowException_When_TryingToSetConsentTypeAndApplicationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.SetConsentTypeAsync(default!, default, CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_SetConsentType_When_ApplicationIsValid()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var application = new OpenIddictDynamoDbApplication();

    // Act
    var consentType = Guid.NewGuid().ToString();
    await applicationStore.SetConsentTypeAsync(application, consentType, CancellationToken.None);

    // Assert
    Assert.Equal(consentType, application.ConsentType);
  }

  [Fact]
  public async Task Should_ThrowException_When_TryingToSetDisplayNameAndApplicationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.SetDisplayNameAsync(default!, default, CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_SetDisplayName_When_ApplicationIsValid()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var application = new OpenIddictDynamoDbApplication();

    // Act
    var displayName = Guid.NewGuid().ToString();
    await applicationStore.SetDisplayNameAsync(application, displayName, CancellationToken.None);

    // Assert
    Assert.Equal(displayName, application.DisplayName);
  }

  [Fact]
  public async Task Should_ThrowException_When_TryingToGetPermissionsAndApplicationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.GetPermissionsAsync(default!, CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_ReturnEmptyList_When_ApplicationDoesntHaveAnyPermissions()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var application = new OpenIddictDynamoDbApplication();
    await applicationStore.CreateAsync(application, CancellationToken.None);

    // Act
    var permissions = await applicationStore.GetPermissionsAsync(application, CancellationToken.None);

    // Assert
    Assert.Empty(permissions);
  }

  [Fact]
  public async Task Should_ReturnPermissions_When_ApplicationHasPermissions()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_ThrowException_When_TryingToGetPostLogoutRedirectUrisAndApplicationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.GetPostLogoutRedirectUrisAsync(default!, CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_ReturnEmptyList_When_ApplicationDoesntHaveAnyPostLogoutRedirectUris()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var application = new OpenIddictDynamoDbApplication();
    await applicationStore.CreateAsync(application, CancellationToken.None);

    // Act
    var postLogoutRedirectUris = await applicationStore.GetPostLogoutRedirectUrisAsync(application, CancellationToken.None);

    // Assert
    Assert.Empty(postLogoutRedirectUris);
  }

  [Fact]
  public async Task Should_ReturnPostLogoutRedirectUris_When_ApplicationHasPostLogoutRedirectUris()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_ThrowException_When_TryingToGetRedirectUrisAndApplicationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.GetRedirectUrisAsync(default!, CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_ReturnEmptyList_When_ApplicationDoesntHaveAnyRedirectUris()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var application = new OpenIddictDynamoDbApplication();
    await applicationStore.CreateAsync(application, CancellationToken.None);

    // Act
    var postLogoutRedirectUris = await applicationStore.GetRedirectUrisAsync(application, CancellationToken.None);

    // Assert
    Assert.Empty(postLogoutRedirectUris);
  }

  [Fact]
  public async Task Should_ReturnRedirectUris_When_ApplicationHasRedirectUris()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_ThrowException_When_TryingToGetRequirementsAndApplicationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.GetRequirementsAsync(default!, CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_ReturnEmptyList_When_ApplicationDoesntHaveAnyRequirements()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var application = new OpenIddictDynamoDbApplication();
    await applicationStore.CreateAsync(application, CancellationToken.None);

    // Act
    var requirements = await applicationStore.GetRequirementsAsync(application, CancellationToken.None);

    // Assert
    Assert.Empty(requirements);
  }

  [Fact]
  public async Task Should_ReturnRequirements_When_ApplicationHasRequirements()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_ThrowException_When_TryingToGetDisplayNamesAndApplicationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.GetDisplayNamesAsync(default!, CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_ReturnEmptyList_When_ApplicationDoesntHaveAnyDisplayNames()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var application = new OpenIddictDynamoDbApplication();
    await applicationStore.CreateAsync(application, CancellationToken.None);

    // Act
    var displayNames = await applicationStore.GetDisplayNamesAsync(application, CancellationToken.None);

    // Assert
    Assert.Empty(displayNames);
  }

  [Fact]
  public async Task Should_ReturnDisplayNames_When_ApplicationHasDisplayNames()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_ThrowException_When_TryingToSetDisplayNamesAndApplicationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.SetDisplayNamesAsync(default!, default!, CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_SetNull_When_SetEmptyDictionaryAsDisplayNames()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_SetDisplayNames_When_SettingDisplayNames()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_ThrowException_When_TryingToSetPermissionsAndApplicationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.SetPermissionsAsync(default!, default!, CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_SetNull_When_SetEmptyListAsPermissions()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_SetPermissions_When_SettingPermissions()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_ThrowException_When_TryingToSetPostLogoutRedirectUrisAndApplicationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.SetPostLogoutRedirectUrisAsync(default!, default!, CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_SetNull_When_SetEmptyListAsPostLogoutRedirectUris()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_SetPostLogoutRedirectUris_When_SettingPostLogoutRedirectUris()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_ThrowException_When_TryingToSetRedirectUrisAndApplicationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.SetRedirectUrisAsync(default!, default!, CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_SetNull_When_SetEmptyListAsRedirectUris()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_SetRedirectUris_When_SettingRedirectUris()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_ThrowException_When_TryingToSetRequirementsAndApplicationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.SetRequirementsAsync(default!, default!, CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_SetNull_When_SetEmptyListAsRequirements()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_SetRequirements_When_SettingRequirements()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_ThrowException_When_TryingToSetPropertiesAndApplicationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.SetPropertiesAsync(default!, default!, CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_SetNull_When_SetEmptyListAsProperties()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_SetProperties_When_SettingProperties()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_ThrowException_When_TryingToGetPropertiesAndApplicationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.GetPropertiesAsync(default!, CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_ReturnEmptyDictionary_When_PropertiesIsNull()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_ReturnNonEmptyDictionary_When_PropertiesExists()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_ReturnNewApplication_When_CallingInstantiate()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act
    var application = await applicationStore.InstantiateAsync(CancellationToken.None);

    // Assert
    Assert.IsType<OpenIddictDynamoDbApplication>(application);
  }

  [Fact]
  public async Task Should_ThrowNotSupported_When_TryingToListBasedOnLinq()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    Assert.Throws<NotSupportedException>(() =>
      applicationStore.ListAsync<int, int>(default!, default, CancellationToken.None));
  }

  [Fact]
  public async Task Should_ReturnList_When_ListingApplications()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    var applicationCount = 10;
    var applicationIds = new List<string>();
    foreach (var index in Enumerable.Range(0, applicationCount))
    {
      var application = new OpenIddictDynamoDbApplication
      {
        DisplayName = index.ToString(),
      };
      await applicationStore.CreateAsync(application, CancellationToken.None);
      applicationIds.Add(application.Id);
    }

    // Act
    var applications = applicationStore.ListAsync(default, default, CancellationToken.None);

    // Assert
    var matchedApplications = new List<OpenIddictDynamoDbApplication>();
    await foreach (var application in applications)
    {
      matchedApplications.Add(application);
    }
    Assert.True(matchedApplications.Count >= applicationCount);
    Assert.False(applicationIds.Except(matchedApplications.Select(x => x.Id)).Any());
  }

  [Fact]
  public async Task Should_ReturnFirstFive_When_ListingApplicationsWithCount()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_ReturnLastFive_When_ListingApplicationsWithCountAndOffset()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
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

  [Fact]
  public async Task Should_ThrowNotSupported_When_TryingToFetchWithOffsetWithoutFirstFetchingPreviousPages()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    Assert.Throws<NotSupportedException>(() =>
      applicationStore.ListAsync(5, 5, CancellationToken.None));
  }

  [Fact]
  public async Task Should_ThrowException_When_TryingToGetApplicationTypeAndApplicationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.GetApplicationTypeAsync(default!, CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_ReturnApplicationType_When_ApplicationIsValid()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var application = new OpenIddictDynamoDbApplication
    {
      ApplicationType = ApplicationTypes.Web,
    };
    await applicationStore.CreateAsync(application, CancellationToken.None);

    // Act
    var applicationType = await applicationStore.GetApplicationTypeAsync(application, CancellationToken.None);

    // Assert
    Assert.NotNull(applicationType);
    Assert.Equal(application.ApplicationType, applicationType);
  }

  [Fact]
  public async Task Should_ThrowException_When_TryingToSetApplicationTypeAndApplicationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.SetApplicationTypeAsync(default!, default, CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_SetApplicationType_When_ApplicationIsValid()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var application = new OpenIddictDynamoDbApplication();

    // Act
    var applicationType = Guid.NewGuid().ToString();
    await applicationStore.SetApplicationTypeAsync(application, applicationType, CancellationToken.None);

    // Assert
    Assert.Equal(applicationType, application.ApplicationType);
  }

  [Fact]
  public async Task Should_ThrowException_When_TryingToGetJsonWebKeySetAndApplicationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.GetJsonWebKeySetAsync(default!, CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_ReturnJsonWebKeySet_When_ApplicationIsValid()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var application = new OpenIddictDynamoDbApplication
    {
      JsonWebKeySet = "{\"e\":\"AQAB\",\"n\":\"nZD7QWmIwj-3N_RZ1qJjX6CdibU87y2l02yMay4KunambalP9g0fU9yZLwLX9WYJINcXZDUf6QeZ-SSbblET-h8Q4OvfSQ7iuu0WqcvBGy8M0qoZ7I-NiChw8dyybMJHgpiP_AyxpCQnp3bQ6829kb3fopbb4cAkOilwVRBYPhRLboXma0cwcllJHPLvMp1oGa7Ad8osmmJhXhM9qdFFASg_OCQdPnYVzp8gOFeOGwlXfSFEgt5vgeU25E-ycUOREcnP7BnMUk7wpwYqlE537LWGOV5z_1Dqcqc9LmN-z4HmNV7b23QZW4_mzKIOY4IqjmnUGgLU9ycFj5YGDCts7Q\",\"alg\":\"RS256\",\"kid\":\"8f796169-0ac4-48a3-a202-fa4f3d814fcd\",\"kty\":\"RSA\",\"use\":\"sig\"}",
    };
    await applicationStore.CreateAsync(application, CancellationToken.None);

    // Act
    var jsonWebKeySet = await applicationStore.GetJsonWebKeySetAsync(application, CancellationToken.None);

    // Assert
    Assert.NotNull(jsonWebKeySet);
  }

  [Fact]
  public async Task Should_ReturnNullResult_When_JsonWebKeySetIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var application = new OpenIddictDynamoDbApplication();
    await applicationStore.CreateAsync(application, CancellationToken.None);

    // Act
    var jsonWebKeySet = await applicationStore.GetJsonWebKeySetAsync(application, CancellationToken.None);

    // Assert
    Assert.Null(jsonWebKeySet);
  }

  [Fact]
  public async Task Should_ThrowException_When_TryingToSetJsonWebKeySetAndApplicationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.SetJsonWebKeySetAsync(default!, default, CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_SetJsonWebKeySet_When_ApplicationIsValid()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var application = new OpenIddictDynamoDbApplication();

    // Act
    var jsonWebKeySet = JsonWebKeySet.Create("{\"e\":\"AQAB\",\"n\":\"nZD7QWmIwj-3N_RZ1qJjX6CdibU87y2l02yMay4KunambalP9g0fU9yZLwLX9WYJINcXZDUf6QeZ-SSbblET-h8Q4OvfSQ7iuu0WqcvBGy8M0qoZ7I-NiChw8dyybMJHgpiP_AyxpCQnp3bQ6829kb3fopbb4cAkOilwVRBYPhRLboXma0cwcllJHPLvMp1oGa7Ad8osmmJhXhM9qdFFASg_OCQdPnYVzp8gOFeOGwlXfSFEgt5vgeU25E-ycUOREcnP7BnMUk7wpwYqlE537LWGOV5z_1Dqcqc9LmN-z4HmNV7b23QZW4_mzKIOY4IqjmnUGgLU9ycFj5YGDCts7Q\",\"alg\":\"RS256\",\"kid\":\"8f796169-0ac4-48a3-a202-fa4f3d814fcd\",\"kty\":\"RSA\",\"use\":\"sig\"}");
    await applicationStore.SetJsonWebKeySetAsync(application, jsonWebKeySet, CancellationToken.None);

    // Assert
    Assert.NotNull(application.JsonWebKeySet);
  }

  [Fact]
  public async Task Should_ThrowException_When_TryingToGetSettingsAndApplicationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.GetSettingsAsync(default!, CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_ReturnEmptyList_When_ApplicationDoesntHaveAnySettings()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var application = new OpenIddictDynamoDbApplication();
    await applicationStore.CreateAsync(application, CancellationToken.None);

    // Act
    var settings = await applicationStore.GetSettingsAsync(application, CancellationToken.None);

    // Assert
    Assert.Empty(settings);
  }

  [Fact]
  public async Task Should_ReturnSettings_When_ApplicationHasSettings()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var application = new OpenIddictDynamoDbApplication
    {
      Settings = new Dictionary<string, string>
      {
        { "sv-SE", "Testar" },
        { "es-ES", "Testado" },
        { "en-US", "Testing" },
      },
    };
    await applicationStore.CreateAsync(application, CancellationToken.None);

    // Act
    var settings = await applicationStore.GetSettingsAsync(application, CancellationToken.None);

    // Assert
    Assert.Equal(3, settings.Count);
  }

  [Fact]
  public async Task Should_ThrowException_When_TryingToSetSettingsAndApplicationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await applicationStore.SetSettingsAsync(default!, default!, CancellationToken.None));
    Assert.Equal("application", exception.ParamName);
  }

  [Fact]
  public async Task Should_SetNull_When_SetEmptyDictionaryAsSettings()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var application = new OpenIddictDynamoDbApplication();
    await applicationStore.CreateAsync(application, CancellationToken.None);

    // Act
    await applicationStore.SetSettingsAsync(
      application,
      ImmutableDictionary.Create<string, string>(),
      CancellationToken.None);

    // Assert
    Assert.Null(application.Settings);
  }

  [Fact]
  public async Task Should_SetSettings_When_SettingSettings()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var application = new OpenIddictDynamoDbApplication();
    await applicationStore.CreateAsync(application, CancellationToken.None);

    // Act
    var settings = new Dictionary<string, string>
    {
      { "Toast", "Testar" },
      { "Toastado", "Testado" },
      { "Toasting", "Testing" },
    };
    await applicationStore.SetSettingsAsync(
      application,
      settings.ToImmutableDictionary(x => x.Key, x => x.Value),
      CancellationToken.None);

    // Assert
    Assert.NotNull(application.Settings);
    Assert.Equal(3, application.Settings!.Count);
  }
}
