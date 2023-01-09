using System.Collections.Immutable;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Xunit;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OpenIddict.AmazonDynamoDB.Tests;

[Collection(Constants.LocalDatabaseCollection)]
public class OpenIddictDynamoDbAuthorizationStoreTests
{
  public readonly IAmazonDynamoDB _client;

  public OpenIddictDynamoDbAuthorizationStoreTests(LocalDatabaseFixture fixture) => _client = fixture.Client;

  [Fact(Skip = "Test")]
  public void Should_ThrowArgumentNullException_When_OptionsIsNotSet()
  {
    // Arrange, Act & Assert
    var exception = Assert.Throws<ArgumentNullException>(() =>
      new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(null!));

    Assert.Equal("optionsMonitor", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public void Should_ThrowArgumentNullException_When_DatabaseIsNotSet()
  {
    // Arrange, Act & Assert
    var exception = Assert.Throws<ArgumentNullException>(() =>
      new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(TestUtils.GetOptions(new())));

    Assert.Equal("Database", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_GetDatabaseFromServiceProvider_When_DatabaseIsNullInOptions()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new());
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options, _client);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options, _client);

    // Act
    var authorization = new OpenIddictDynamoDbAuthorization();
    await authorizationStore.CreateAsync(authorization, CancellationToken.None);

    // Assert
    var databaseToken = await context.LoadAsync<OpenIddictDynamoDbAuthorization>(
      authorization.PartitionKey, authorization.SortKey);
    Assert.NotNull(databaseToken);
  }

  [Fact(Skip = "Test")]
  public async Task Should_IncreaseCount_When_CountingAuthorizationsAfterCreatingOne()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var authorization = new OpenIddictDynamoDbAuthorization();
    var beforeCount = await authorizationStore.CountAsync(CancellationToken.None);
    await authorizationStore.CreateAsync(authorization, CancellationToken.None);

    // Act
    var count = await authorizationStore.CountAsync(CancellationToken.None);

    // Assert
    Assert.Equal(beforeCount + 1, count);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ThrowNotSupported_When_TryingToCountBasedOnLinq()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<NotSupportedException>(async () =>
      await authorizationStore.CountAsync<int>(default!, CancellationToken.None));
  }

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToCreateAuthorizationThatIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        await authorizationStore.CreateAsync(default!, CancellationToken.None));
    Assert.Equal("authorization", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_CreateAuthorization_When_AuthorizationIsValid()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var authorization = new OpenIddictDynamoDbAuthorization
    {
      ApplicationId = Guid.NewGuid().ToString(),
    };

    // Act
    await authorizationStore.CreateAsync(authorization, CancellationToken.None);

    // Assert
    var databaseAuthorization = await context.LoadAsync<OpenIddictDynamoDbAuthorization>(
      authorization.PartitionKey, authorization.SortKey);
    Assert.NotNull(databaseAuthorization);
    Assert.Equal(authorization.ApplicationId, databaseAuthorization.ApplicationId);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToDeleteAuthorizationThatIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await authorizationStore.DeleteAsync(default!, CancellationToken.None));
    Assert.Equal("authorization", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_DeleteAuthorization_When_AuthorizationIsValid()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var authorization = new OpenIddictDynamoDbAuthorization
    {
      ApplicationId = Guid.NewGuid().ToString(),
    };
    await authorizationStore.CreateAsync(authorization, CancellationToken.None);

    // Act
    await authorizationStore.DeleteAsync(authorization, CancellationToken.None);

    // Assert
    var databaseAuthorization = await context.LoadAsync<OpenIddictDynamoDbAuthorization>(
      authorization.PartitionKey, authorization.SortKey);
    Assert.Null(databaseAuthorization);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ThrowNotSupported_When_TryingToListBasedOnLinq()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    Assert.Throws<NotSupportedException>(() =>
      authorizationStore.ListAsync<int, int>(default!, default, CancellationToken.None));
  }

  [Fact(Skip = "Test")]
  public async Task Should_ReturnList_When_ListingAuthorizations()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    var authorizationCount = 10;
    var authorizationIds = new List<string>();
    foreach (var index in Enumerable.Range(0, authorizationCount))
    {
      var authorization = new OpenIddictDynamoDbAuthorization
      {
        Subject = index.ToString(),
      };
      await authorizationStore.CreateAsync(authorization, CancellationToken.None);
      authorizationIds.Add(authorization.Id);
    }

    // Act
    var authorizations = authorizationStore.ListAsync(default, default, CancellationToken.None);

    // Assert
    var matchedAuthorizations = new List<OpenIddictDynamoDbAuthorization>();
    await foreach (var authorization in authorizations)
    {
      matchedAuthorizations.Add(authorization);
    }
    Assert.True(matchedAuthorizations.Count >= authorizationCount);
    Assert.False(authorizationIds.Except(matchedAuthorizations.Select(x => x.Id)).Any());
  }

  [Fact(Skip = "Test")]
  public async Task Should_ReturnFirstFive_When_ListingAuthorizationsWithCount()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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

  [Fact(Skip = "Test")]
  public async Task Should_ReturnLastFive_When_ListingAuthorizationsWithCountAndOffset()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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

  [Fact(Skip = "Test")]
  public async Task Should_ThrowNotSupported_When_TryingToFetchWithOffsetWithoutFirstFetchingPreviousPages()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    Assert.Throws<NotSupportedException>(() =>
      authorizationStore.ListAsync(5, 5, CancellationToken.None));
  }

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_ToUpdateAuthorizationThatIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await authorizationStore.UpdateAsync(default!, CancellationToken.None));
    Assert.Equal("authorization", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToUpdateAuthorizationThatDoesntExist()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
      await authorizationStore.UpdateAsync(new OpenIddictDynamoDbAuthorization(), CancellationToken.None));
    Assert.Equal("authorization", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_ConcurrencyTokenHasChanged()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var authorization = new OpenIddictDynamoDbAuthorization();
    await authorizationStore.CreateAsync(authorization, CancellationToken.None);

    // Act & Assert
    authorization.ConcurrencyToken = Guid.NewGuid().ToString();
    var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
      await authorizationStore.UpdateAsync(authorization, CancellationToken.None));
    Assert.Equal("authorization", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_UpdateAuthorization_When_AuthorizationIsValid()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var authorization = new OpenIddictDynamoDbAuthorization();
    await authorizationStore.CreateAsync(authorization, CancellationToken.None);

    // Act
    authorization.Subject = "testing-to-update";
    await authorizationStore.UpdateAsync(authorization, CancellationToken.None);

    // Assert
    var databaseAuthorization = await context.LoadAsync<OpenIddictDynamoDbAuthorization>(
      authorization.PartitionKey, authorization.SortKey);
    Assert.NotNull(databaseAuthorization);
    Assert.Equal(databaseAuthorization.Subject, authorization.Subject);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToSetTypeAndAuthorizationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await authorizationStore.SetTypeAsync(default!, default, CancellationToken.None));
    Assert.Equal("authorization", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_SetType_When_AuthorizationIsValid()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var authorization = new OpenIddictDynamoDbAuthorization();

    // Act
    var type = "SomeType";
    await authorizationStore.SetTypeAsync(authorization, type, CancellationToken.None);

    // Assert
    Assert.Equal(type, authorization.Type);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToSetSubjectAndAuthorizationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await authorizationStore.SetSubjectAsync(default!, default, CancellationToken.None));
    Assert.Equal("authorization", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_SetSubject_When_AuthorizationIsValid()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var authorization = new OpenIddictDynamoDbAuthorization();

    // Act
    var subject = "SomeSubject";
    await authorizationStore.SetSubjectAsync(authorization, subject, CancellationToken.None);

    // Assert
    Assert.Equal(subject, authorization.Subject);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToSetStatusAndAuthorizationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await authorizationStore.SetStatusAsync(default!, default, CancellationToken.None));
    Assert.Equal("authorization", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_SetStatus_When_AuthorizationIsValid()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var authorization = new OpenIddictDynamoDbAuthorization();

    // Act
    var status = "SomeStatus";
    await authorizationStore.SetStatusAsync(authorization, status, CancellationToken.None);

    // Assert
    Assert.Equal(status, authorization.Status);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToSetScopesAndAuthorizationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await authorizationStore.SetScopesAsync(default!, default!, CancellationToken.None));
    Assert.Equal("authorization", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_SetNull_When_SetEmptyListAsScopes()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
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

  [Fact(Skip = "Test")]
  public async Task Should_SetScopes_When_SettingScopes()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
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

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToSetPropertiesAndAuthorizationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await authorizationStore.SetPropertiesAsync(default!, default!, CancellationToken.None));
    Assert.Equal("authorization", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_SetNull_When_SetEmptyListAsProperties()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
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

  [Fact(Skip = "Test")]
  public async Task Should_SetProperties_When_SettingProperties()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
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

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToGetPropertiesAndAuthorizationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await authorizationStore.GetPropertiesAsync(default!, CancellationToken.None));
    Assert.Equal("authorization", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ReturnEmptyDictionary_When_PropertiesIsNull()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var authorization = new OpenIddictDynamoDbAuthorization();
    await authorizationStore.CreateAsync(authorization, CancellationToken.None);

    // Act
    var properties = await authorizationStore.GetPropertiesAsync(
      authorization,
      CancellationToken.None);

    // Assert
    Assert.Empty(properties);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ReturnNonEmptyDictionary_When_PropertiesExists()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
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

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToSetCreationDateAndAuthorizationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await authorizationStore.SetCreationDateAsync(default!, default, CancellationToken.None));
    Assert.Equal("authorization", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_SetCreationDate_When_AuthorizationIsValid()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var authorization = new OpenIddictDynamoDbAuthorization();

    // Act
    var creationDate = DateTimeOffset.Now;
    await authorizationStore.SetCreationDateAsync(authorization, creationDate, CancellationToken.None);

    // Assert
    Assert.Equal(creationDate.UtcDateTime, authorization.CreationDate);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToSetApplicationIdAndAuthorizationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await authorizationStore.SetApplicationIdAsync(default!, default, CancellationToken.None));
    Assert.Equal("authorization", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_SetApplicationId_When_AuthorizationIsValid()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var authorization = new OpenIddictDynamoDbAuthorization();

    // Act
    var applicationId = Guid.NewGuid().ToString();
    await authorizationStore.SetApplicationIdAsync(authorization, applicationId, CancellationToken.None);

    // Assert
    Assert.Equal(applicationId, authorization.ApplicationId);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ReturnNewApplication_When_CallingInstantiate()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act
    var authorization = await authorizationStore.InstantiateAsync(CancellationToken.None);

    // Assert
    Assert.IsType<OpenIddictDynamoDbAuthorization>(authorization);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToGetTypeAndAuthorizationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await authorizationStore.GetTypeAsync(default!, CancellationToken.None));
    Assert.Equal("authorization", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ReturnType_When_AuthorizationIsValid()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
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

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToGetSubjectAndAuthorizationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await authorizationStore.GetSubjectAsync(default!, CancellationToken.None));
    Assert.Equal("authorization", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ReturnSubject_When_AuthorizationIsValid()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
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

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToGetStatusAndAuthorizationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await authorizationStore.GetStatusAsync(default!, CancellationToken.None));
    Assert.Equal("authorization", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ReturnStatus_When_AuthorizationIsValid()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
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

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToGetIdAndAuthorizationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await authorizationStore.GetIdAsync(default!, CancellationToken.None));
    Assert.Equal("authorization", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ReturnId_When_AuthorizationIsValid()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
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

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToGetCreationDateAndAuthorizationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await authorizationStore.GetCreationDateAsync(default!, CancellationToken.None));
    Assert.Equal("authorization", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ReturnCreationDate_When_AuthorizationIsValid()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
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

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToGetApplicationIdAndAuthorizationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await authorizationStore.GetApplicationIdAsync(default!, CancellationToken.None));
    Assert.Equal("authorization", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ReturnApplicationId_When_AuthorizationIsValid()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
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

  [Fact(Skip = "Test")]
  public async Task Should_ThrowNotSupported_When_TryingToGetBasedOnLinq()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<NotSupportedException>(async () =>
      await authorizationStore.GetAsync<int, int>(default!, default!, CancellationToken.None));
  }

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToGetScopesAndAuthorizationIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await authorizationStore.GetScopesAsync(default!, CancellationToken.None));
    Assert.Equal("authorization", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ReturnEmptyList_When_AuthorizationDoesntHaveAnyScopes()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var authorization = new OpenIddictDynamoDbAuthorization();
    await authorizationStore.CreateAsync(authorization, CancellationToken.None);

    // Act
    var postLogoutScopes = await authorizationStore.GetScopesAsync(authorization, CancellationToken.None);

    // Assert
    Assert.Empty(postLogoutScopes);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ReturnScopes_When_AuthorizationHasScopes()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
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

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToFindAuthorizationAndSubjectIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = Assert.Throws<ArgumentNullException>(() =>
      authorizationStore.FindAsync(default!, "test", CancellationToken.None));
    Assert.Equal("subject", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToFindAuthorizationAndClientIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = Assert.Throws<ArgumentNullException>(() =>
      authorizationStore.FindAsync("test", default!, CancellationToken.None));
    Assert.Equal("client", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ReturnEmptyList_When_FindingAuthorizationsWithNoMatch()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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

  [Fact(Skip = "Test")]
  public async Task Should_ReturnListOffOne_When_FindingAuthorizationsWithMatch()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    var uniqueKey = Guid.NewGuid().ToString();
    foreach (var index in Enumerable.Range(0, 10))
    {
      await authorizationStore.CreateAsync(new OpenIddictDynamoDbAuthorization
      {
        Subject = $"{index.ToString()}-{uniqueKey}",
        ApplicationId = $"{index.ToString()}-{uniqueKey}",
      }, CancellationToken.None);
    }

    // Act
    var authorizations = authorizationStore
      .FindAsync($"5-{uniqueKey}", $"5-{uniqueKey}", CancellationToken.None);

    // Assert
    var matchedAuthorizations = new List<OpenIddictDynamoDbAuthorization>();
    await foreach (var authorization in authorizations)
    {
      matchedAuthorizations.Add(authorization);
    }
    Assert.Single(matchedAuthorizations);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToFindWithStatusAuthorizationAndSubjectIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = Assert.Throws<ArgumentNullException>(() =>
      authorizationStore.FindAsync(default!, "test", "test", CancellationToken.None));
    Assert.Equal("subject", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToFindWithStatusAuthorizationAndClientIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = Assert.Throws<ArgumentNullException>(() =>
      authorizationStore.FindAsync("test", default!, "test", CancellationToken.None));
    Assert.Equal("client", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToFindAuthorizationAndStatusIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = Assert.Throws<ArgumentNullException>(() =>
      authorizationStore.FindAsync("test", "test", default!, CancellationToken.None));
    Assert.Equal("status", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ReturnListOffOne_When_FindingAuthorizationsWithStatusMatch()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToFindWithTypeAuthorizationAndSubjectIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = Assert.Throws<ArgumentNullException>(() =>
      authorizationStore.FindAsync(default!, "test", "test", "test", CancellationToken.None));
    Assert.Equal("subject", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToFindWithTypeAuthorizationAndClientIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = Assert.Throws<ArgumentNullException>(() =>
      authorizationStore.FindAsync("test", default!, "test", "test", CancellationToken.None));
    Assert.Equal("client", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToFindWithTypeAuthorizationAndStatusIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = Assert.Throws<ArgumentNullException>(() =>
      authorizationStore.FindAsync("test", "test", default!, "test", CancellationToken.None));
    Assert.Equal("status", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToFindWithTypeAuthorizationAndTypeIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = Assert.Throws<ArgumentNullException>(() =>
      authorizationStore.FindAsync("test", "test", "test", default!, CancellationToken.None));
    Assert.Equal("type", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ReturnListOffOne_When_FindingAuthorizationsWithTypeMatch()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToFindWithScopesAuthorizationAndSubjectIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToFindWithScopesAuthorizationAndClientIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToFindWithScopesAuthorizationAndStatusIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToFindWithScopesAuthorizationAndTypeIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToFindWithScopesAuthorizationAndScopesIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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

  [Fact(Skip = "Test")]
  public async Task Should_ReturnListOffOne_When_FindingAuthorizationsWithScopesMatch()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToFindAuthorizationByApplicationIdAndIdentifierIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = Assert.Throws<ArgumentNullException>(() =>
      authorizationStore.FindByApplicationIdAsync(default!, CancellationToken.None));
    Assert.Equal("identifier", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ReturnEmptyList_When_FindingAuthorizationsByApplicationIdWithNoMatch()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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

  [Fact(Skip = "Test")]
  public async Task Should_ReturnListOffOne_When_FindingAuthorizationsByApplicationIdWithMatch()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToFindAuthorizationBySubjectAndSubjectIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = Assert.Throws<ArgumentNullException>(() =>
      authorizationStore.FindBySubjectAsync(default!, CancellationToken.None));
    Assert.Equal("subject", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ReturnEmptyList_When_FindingAuthorizationsBySubjectWithNoMatch()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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

  [Fact(Skip = "Test")]
  public async Task Should_ReturnListOffOne_When_FindingAuthorizationsBySubjectWithMatch()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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

  [Fact(Skip = "Test")]
  public async Task Should_ThrowException_When_TryingToFindAuthorizationByIdAndIdentifierIsNull()
  {
    // Arrange
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      await authorizationStore.FindByIdAsync(default!, CancellationToken.None));
    Assert.Equal("identifier", exception.ParamName);
  }

  [Fact(Skip = "Test")]
  public async Task Should_ReturnAuthorization_When_FindingAuthorizationsByIdWithMatch()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

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

  [Fact(Skip = "Test")]
  public async Task Should_DeleteAllAuthorizations_When_AllHasExpired()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var beforeCount = await authorizationStore.CountAsync(CancellationToken.None);

    foreach (var index in Enumerable.Range(0, 10))
    {
      await authorizationStore.CreateAsync(new OpenIddictDynamoDbAuthorization
      {
        CreationDate = DateTime.UtcNow.AddDays(-5),
      }, CancellationToken.None);
    }

    // Act
    await authorizationStore.PruneAsync(DateTime.UtcNow.AddDays(-4), CancellationToken.None);

    // Assert
    var count = await authorizationStore.CountAsync(CancellationToken.None);
    Assert.Equal(beforeCount, count);
  }

  [Fact(Skip = "Test")]
  public async Task Should_NotDeleteAnyAuthorizations_When_TheyAreOldButValid()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    await authorizationStore.PruneAsync(DateTime.UtcNow.AddDays(-4), CancellationToken.None);
    var beforeCount = await authorizationStore.CountAsync(CancellationToken.None);

    var numberOfTokens = 10;
    foreach (var index in Enumerable.Range(0, numberOfTokens))
    {
      await authorizationStore.CreateAsync(new OpenIddictDynamoDbAuthorization
      {
        CreationDate = DateTime.UtcNow.AddDays(-5),
        Status = Statuses.Valid,
      }, CancellationToken.None);
    }

    // Act
    await authorizationStore.PruneAsync(DateTime.UtcNow.AddDays(-4), CancellationToken.None);

    // Assert
    var count = await authorizationStore.CountAsync(CancellationToken.None);
    Assert.Equal(beforeCount + numberOfTokens, count);
  }

  [Fact(Skip = "Test")]
  public async Task Should_DeleteAllAuthorizations_When_TheyAreAdHocAndNoTokens()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var beforeCount = await authorizationStore.CountAsync(CancellationToken.None);

    foreach (var index in Enumerable.Range(0, 10))
    {
      await authorizationStore.CreateAsync(new OpenIddictDynamoDbAuthorization
      {
        CreationDate = DateTime.UtcNow.AddDays(-5),
        Status = Statuses.Valid,
        Type = AuthorizationTypes.AdHoc,
      }, CancellationToken.None);
    }

    // Act
    await authorizationStore.PruneAsync(DateTime.UtcNow.AddDays(-4), CancellationToken.None);

    // Assert
    var count = await authorizationStore.CountAsync(CancellationToken.None);
    Assert.Equal(beforeCount, count);
  }

  [Fact(Skip = "Test")]
  public async Task Should_NotDeleteAnyAuthorizations_When_TheyAreAdHocAndHaveTokens()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    var beforeCount = await authorizationStore.CountAsync(CancellationToken.None);
    var authorizationCount = 10;

    foreach (var index in Enumerable.Range(0, authorizationCount))
    {
      var authorization = new OpenIddictDynamoDbAuthorization
      {
        CreationDate = DateTime.UtcNow.AddDays(-5),
        Status = Statuses.Valid,
        Type = AuthorizationTypes.AdHoc,
      };
      await authorizationStore.CreateAsync(authorization, CancellationToken.None);
      await tokenStore.CreateAsync(new OpenIddictDynamoDbToken
      {
        CreationDate = DateTime.UtcNow.AddDays(-index),
        ExpirationDate = DateTime.UtcNow.AddHours(1),
        AuthorizationId = authorization.Id,
        Status = Statuses.Valid,
      }, CancellationToken.None);
    }

    // Act
    await authorizationStore.PruneAsync(DateTime.UtcNow.AddDays(-4), CancellationToken.None);

    // Assert
    var count = await authorizationStore.CountAsync(CancellationToken.None);
    Assert.Equal(beforeCount + authorizationCount, count);
  }

  [Fact(Skip = "Test")]
  public async Task Should_DeleteSomeAuthorizations_When_SomeAreOutsideOfTheThresholdRange()
  {
    // Arrange
    var context = new DynamoDBContext(_client);
    var options = TestUtils.GetOptions(new() { Database = _client });
    var authorizationStore = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    var beforeCount = await authorizationStore.CountAsync(CancellationToken.None);

    foreach (var index in Enumerable.Range(0, 10))
    {
      await authorizationStore.CreateAsync(new OpenIddictDynamoDbAuthorization
      {
        CreationDate = DateTime.UtcNow.AddDays(-index),
        Status = Statuses.Inactive,
      }, CancellationToken.None);
    }

    // Act
    await authorizationStore.PruneAsync(DateTime.UtcNow.AddDays(-5), CancellationToken.None);

    // Assert
    var count = await authorizationStore.CountAsync(CancellationToken.None);
    Assert.Equal(beforeCount + 5, count);
  }
}
