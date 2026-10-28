using Amazon.DynamoDBv2.Model;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OpenIddict.AmazonDynamoDB.Tests;

// The lookup items that replace the indexes: unique values are enforced, lookups move with
// changed values and go away with their entity
[Collection(Constants.DatabaseCollection)]
public class LookupTests(DatabaseFixture fixture)
{
  [Fact]
  public async Task Should_Throw_When_ClientIdIsTaken()
  {
    // Arrange
    var store = await CreateApplicationStore();
    var clientId = Guid.NewGuid().ToString();
    var first = new OpenIddictDynamoDbApplication { ClientId = clientId };
    await store.CreateAsync(first, CancellationToken.None);

    // Act & Assert
    var second = new OpenIddictDynamoDbApplication { ClientId = clientId };
    var exception = await Assert.ThrowsAsync<OpenIddictExceptions.ValidationException>(async () => await store.CreateAsync(second, CancellationToken.None));
    Assert.Contains("client identifier", exception.Message);
    Assert.Null(await store.FindByIdAsync(second.Id, CancellationToken.None));
    Assert.Equal(first.Id, (await store.FindByClientIdAsync(clientId, CancellationToken.None))!.Id);
  }

  [Fact]
  public async Task Should_CreateOneApplication_When_CreatingTheSameClientIdConcurrently()
  {
    // Arrange
    var store = await CreateApplicationStore();
    var clientId = Guid.NewGuid().ToString();

    // Act
    var results = await Task.WhenAll(Enumerable.Range(0, 10).Select(async _ =>
    {
      try
      {
        await store.CreateAsync(new OpenIddictDynamoDbApplication { ClientId = clientId }, CancellationToken.None);
        return true;
      }
      catch (OpenIddictExceptions.ValidationException)
      {
        return false;
      }
    }));

    // Assert
    Assert.Single(results, x => x);
    Assert.Equal(1, await TestUtils.CountItemsAsync(fixture.Client, "APPLICATION#", "#USER#") - await CountOtherApplications(store, clientId));
  }

  [Fact]
  public async Task Should_MoveLookup_When_ClientIdChanges()
  {
    // Arrange
    var store = await CreateApplicationStore();
    var oldClientId = Guid.NewGuid().ToString();
    var newClientId = Guid.NewGuid().ToString();
    var application = new OpenIddictDynamoDbApplication { ClientId = oldClientId };
    await store.CreateAsync(application, CancellationToken.None);
    var loaded = (await store.FindByIdAsync(application.Id, CancellationToken.None))!;
    loaded.ClientId = newClientId;

    // Act
    await store.UpdateAsync(loaded, CancellationToken.None);

    // Assert
    Assert.Null(await store.FindByClientIdAsync(oldClientId, CancellationToken.None));
    Assert.Equal(application.Id, (await store.FindByClientIdAsync(newClientId, CancellationToken.None))!.Id);
    await store.CreateAsync(new OpenIddictDynamoDbApplication { ClientId = oldClientId }, CancellationToken.None);
  }

  [Fact]
  public async Task Should_Throw_When_UpdatingToATakenClientId()
  {
    // Arrange
    var store = await CreateApplicationStore();
    var taken = Guid.NewGuid().ToString();
    await store.CreateAsync(new OpenIddictDynamoDbApplication { ClientId = taken }, CancellationToken.None);
    var application = new OpenIddictDynamoDbApplication { ClientId = Guid.NewGuid().ToString() };
    await store.CreateAsync(application, CancellationToken.None);
    var token = application.ConcurrencyToken;
    application.ClientId = taken;

    // Act & Assert
    await Assert.ThrowsAsync<OpenIddictExceptions.ValidationException>(async () => await store.UpdateAsync(application, CancellationToken.None));
    Assert.Equal(token, application.ConcurrencyToken);
  }

  [Fact]
  public async Task Should_UpdateRedirectListings_When_RedirectUrisChange()
  {
    // Arrange
    var store = await CreateApplicationStore();
    var oldUri = $"https://{Guid.NewGuid()}.example.com/cb";
    var newUri = $"https://{Guid.NewGuid()}.example.com/cb";
    var application = new OpenIddictDynamoDbApplication { ClientId = Guid.NewGuid().ToString(), RedirectUris = [oldUri], PostLogoutRedirectUris = [oldUri] };
    await store.CreateAsync(application, CancellationToken.None);
    application.RedirectUris = [newUri];
    application.PostLogoutRedirectUris = null;

    // Act
    await store.UpdateAsync(application, CancellationToken.None);

    // Assert
    Assert.Empty(await TestUtils.ToListAsync(store.FindByRedirectUriAsync(oldUri, CancellationToken.None)));
    Assert.Empty(await TestUtils.ToListAsync(store.FindByPostLogoutRedirectUriAsync(oldUri, CancellationToken.None)));
    Assert.Equal(application.Id, Assert.Single(await TestUtils.ToListAsync(store.FindByRedirectUriAsync(newUri, CancellationToken.None))).Id);
    Assert.Empty(await TestUtils.ToListAsync(store.FindByPostLogoutRedirectUriAsync(newUri, CancellationToken.None)));
  }

  [Fact]
  public async Task Should_RemoveLookupsAndListings_When_DeletingApplication()
  {
    // Arrange
    var store = await CreateApplicationStore();
    var clientId = Guid.NewGuid().ToString();
    var uri = $"https://{Guid.NewGuid()}.example.com/cb";
    var application = new OpenIddictDynamoDbApplication { ClientId = clientId, RedirectUris = [uri] };
    await store.CreateAsync(application, CancellationToken.None);

    // Act
    await store.DeleteAsync(application, CancellationToken.None);

    // Assert
    Assert.Null(await store.FindByClientIdAsync(clientId, CancellationToken.None));
    Assert.Empty(await TestUtils.ToListAsync(store.FindByRedirectUriAsync(uri, CancellationToken.None)));
    Assert.Null(await DynamoDbItems.GetAsync<LookupItem>(fixture.Client, DatabaseFixture.TableName, Lookups.ClientId(clientId), Lookups.SortKey, CancellationToken.None, consistentRead: true));
    Assert.Empty(await DynamoDbItems.QueryPartitionAsync<MemberItem>(fixture.Client, DatabaseFixture.TableName, Lookups.Redirect(RedirectType.RedirectUri, uri), default, CancellationToken.None, consistentRead: true));
  }

  [Fact]
  public async Task Should_Throw_When_ScopeNameIsTaken()
  {
    // Arrange
    var store = await CreateScopeStore();
    var name = Guid.NewGuid().ToString();
    await store.CreateAsync(new OpenIddictDynamoDbScope { Name = name }, CancellationToken.None);

    // Act & Assert
    var second = new OpenIddictDynamoDbScope { Name = name };
    var exception = await Assert.ThrowsAsync<OpenIddictExceptions.ValidationException>(async () => await store.CreateAsync(second, CancellationToken.None));
    Assert.Contains("same name", exception.Message);
    Assert.Null(await store.FindByIdAsync(second.Id, CancellationToken.None));
  }

  [Fact]
  public async Task Should_MoveLookupsAndFreeTheName_When_ScopeIsRenamedAndDeleted()
  {
    // Arrange
    var store = await CreateScopeStore();
    var oldName = Guid.NewGuid().ToString();
    var newName = Guid.NewGuid().ToString();
    var resource = Guid.NewGuid().ToString();
    var scope = new OpenIddictDynamoDbScope { Name = oldName, Resources = [resource] };
    await store.CreateAsync(scope, CancellationToken.None);
    var loaded = (await store.FindByIdAsync(scope.Id, CancellationToken.None))!;
    loaded.Name = newName;
    loaded.Resources = [];

    // Act
    await store.UpdateAsync(loaded, CancellationToken.None);

    // Assert
    Assert.Null(await store.FindByNameAsync(oldName, CancellationToken.None));
    Assert.Equal(scope.Id, (await store.FindByNameAsync(newName, CancellationToken.None))!.Id);
    Assert.Empty(await TestUtils.ToListAsync(store.FindByResourceAsync(resource, CancellationToken.None)));

    await store.DeleteAsync(loaded, CancellationToken.None);
    Assert.Null(await store.FindByNameAsync(newName, CancellationToken.None));
    await store.CreateAsync(new OpenIddictDynamoDbScope { Name = newName }, CancellationToken.None);
  }

  [Fact]
  public async Task Should_Throw_When_ReferenceIdIsTaken()
  {
    // Arrange
    var store = await CreateTokenStore();
    var referenceId = Guid.NewGuid().ToString();
    var first = new OpenIddictDynamoDbToken { ReferenceId = referenceId, Status = Statuses.Valid };
    await store.CreateAsync(first, CancellationToken.None);

    // Act & Assert
    var second = new OpenIddictDynamoDbToken { ReferenceId = referenceId, Status = Statuses.Valid };
    var exception = await Assert.ThrowsAsync<OpenIddictExceptions.ValidationException>(async () => await store.CreateAsync(second, CancellationToken.None));
    Assert.Contains("reference identifier", exception.Message);
    Assert.Equal(first.Id, (await store.FindByReferenceIdAsync(referenceId, CancellationToken.None))!.Id);
  }

  [Fact]
  public async Task Should_GiveLookupsTheTokensTimeToLive_AndRemoveThemWithTheToken()
  {
    // Arrange
    var store = await CreateTokenStore();
    var referenceId = Guid.NewGuid().ToString();
    var authorizationId = Guid.NewGuid().ToString();
    var expires = DateTime.UtcNow.AddHours(1);
    var token = new OpenIddictDynamoDbToken { ReferenceId = referenceId, AuthorizationId = authorizationId, Status = Statuses.Valid, ExpirationDate = expires };
    await store.CreateAsync(token, CancellationToken.None);
    var member = new OpenIddictDynamoDbAuthorization { Id = authorizationId }.PartitionKey;

    // Assert the lookups
    var lookup = await DynamoDbItems.GetAsync<LookupItem>(fixture.Client, DatabaseFixture.TableName, Lookups.ReferenceId(referenceId), Lookups.SortKey, CancellationToken.None, consistentRead: true);
    var listing = Assert.Single(await DynamoDbItems.QueryPartitionAsync<MemberItem>(fixture.Client, DatabaseFixture.TableName, member, Lookups.TokenMemberPrefix, CancellationToken.None, consistentRead: true));
    Assert.Equal(Truncate(expires), lookup!.TTL);
    Assert.Equal(Truncate(expires), listing.TTL);
    Assert.Equal(token.Id, Assert.Single(await TestUtils.ToListAsync(store.FindByAuthorizationIdAsync(authorizationId, CancellationToken.None))).Id);

    // Revoking shortens the time to live of the token and its lookups alike
    var loaded = (await store.FindByIdAsync(token.Id, CancellationToken.None))!;
    loaded.Status = Statuses.Revoked;
    await store.UpdateAsync(loaded, CancellationToken.None);
    lookup = await DynamoDbItems.GetAsync<LookupItem>(fixture.Client, DatabaseFixture.TableName, Lookups.ReferenceId(referenceId), Lookups.SortKey, CancellationToken.None, consistentRead: true);
    Assert.Equal(Truncate(loaded.TTL!.Value), lookup!.TTL);
    Assert.True(lookup.TTL < expires);

    // Deleting removes them
    await store.DeleteAsync(loaded, CancellationToken.None);
    Assert.Null(await store.FindByReferenceIdAsync(referenceId, CancellationToken.None));
    Assert.Empty(await TestUtils.ToListAsync(store.FindByAuthorizationIdAsync(authorizationId, CancellationToken.None)));
    Assert.Empty(await DynamoDbItems.QueryPartitionAsync<MemberItem>(fixture.Client, DatabaseFixture.TableName, member, Lookups.TokenMemberPrefix, CancellationToken.None, consistentRead: true));
  }

  [Fact]
  public async Task Should_RemoveLookups_When_PruningTokens()
  {
    // Arrange
    var store = await CreateTokenStore();
    var referenceId = Guid.NewGuid().ToString();
    var authorizationId = Guid.NewGuid().ToString();
    var token = new OpenIddictDynamoDbToken
    {
      ReferenceId = referenceId,
      AuthorizationId = authorizationId,
      Status = Statuses.Revoked,
      CreationDate = DateTime.UtcNow.AddDays(-2),
    };
    await store.CreateAsync(token, CancellationToken.None);

    // Act
    await store.PruneAsync(DateTimeOffset.UtcNow.AddDays(-1), CancellationToken.None);

    // Assert
    Assert.Null(await store.FindByIdAsync(token.Id, CancellationToken.None));
    Assert.Null(await store.FindByReferenceIdAsync(referenceId, CancellationToken.None));
    Assert.Empty(await DynamoDbItems.QueryPartitionAsync<MemberItem>(
      fixture.Client, DatabaseFixture.TableName, new OpenIddictDynamoDbAuthorization { Id = authorizationId }.PartitionKey, Lookups.TokenMemberPrefix, CancellationToken.None, consistentRead: true));
  }

  private static DateTime Truncate(DateTime value) => DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(value).ToUnixTimeSeconds()).UtcDateTime;

  private async Task<int> CountOtherApplications(OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication> store, string clientId)
  {
    var all = await TestUtils.CountItemsAsync(fixture.Client, "APPLICATION#", "#USER#");
    var mine = await store.FindByClientIdAsync(clientId, CancellationToken.None) is null ? 0 : 1;
    return all - mine;
  }

  private async Task<OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>> CreateApplicationStore()
  {
    var options = TestUtils.GetOptions(new() { Database = fixture.Client });
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    return new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
  }

  private async Task<OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>> CreateScopeStore()
  {
    var options = TestUtils.GetOptions(new() { Database = fixture.Client });
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    return new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
  }

  private async Task<OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>> CreateTokenStore()
  {
    var options = TestUtils.GetOptions(new() { Database = fixture.Client });
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    return new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(options);
  }
}
