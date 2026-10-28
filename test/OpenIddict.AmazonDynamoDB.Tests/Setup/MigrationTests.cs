using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OpenIddict.AmazonDynamoDB.Tests;

// A table created and filled by version 3 has eight global secondary indexes and no lookup items
// for client ids, redirect URIs, reference ids or the tokens of an authorization; the migration
// adds the lookup items and the legacy indexes are removed afterwards
[Collection(Constants.DatabaseCollection)]
public class MigrationTests(DatabaseFixture fixture)
{
  [Fact]
  public async Task Should_WriteLookupItems_When_MigratingLegacyTable()
  {
    // Arrange
    var tableName = await LegacyTable.CreateAsync(fixture.Client);

    try
    {
      var seed = await SeedLegacyData(tableName);
      var options = TestUtils.GetOptions(new() { Database = fixture.Client, DefaultTableName = tableName });
      var applications = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
      var scopes = new OpenIddictDynamoDbScopeStore<OpenIddictDynamoDbScope>(options);
      var tokens = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(options);
      var authorizations = new OpenIddictDynamoDbAuthorizationStore<OpenIddictDynamoDbAuthorization>(options);
      Assert.Null(await applications.FindByClientIdAsync(seed.Application.ClientId!, CancellationToken.None));

      // Act
      var result = await DynamoDbTableSetup.MigrateAsync(options.CurrentValue, fixture.Client);

      // Assert
      Assert.Equal(new MigrationResult(Applications: 1, Redirects: 2, Scopes: 1, Tokens: 1), result);
      Assert.Equal(seed.Application.Id, (await applications.FindByClientIdAsync(seed.Application.ClientId!, CancellationToken.None))!.Id);
      Assert.Equal(seed.Application.Id, Assert.Single(await TestUtils.ToListAsync(applications.FindByRedirectUriAsync("https://app.example.com/callback", CancellationToken.None))).Id);
      Assert.Equal(seed.Application.Id, Assert.Single(await TestUtils.ToListAsync(applications.FindByPostLogoutRedirectUriAsync("https://app.example.com/logout", CancellationToken.None))).Id);
      Assert.Equal(seed.Scope.Id, (await scopes.FindByNameAsync(seed.Scope.Name!, CancellationToken.None))!.Id);
      Assert.Equal(seed.Scope.Id, Assert.Single(await TestUtils.ToListAsync(scopes.FindByResourceAsync("resource-server", CancellationToken.None))).Id);
      Assert.Equal(seed.Scope.Id, Assert.Single(await TestUtils.ToListAsync(scopes.FindByNamesAsync([seed.Scope.Name!], CancellationToken.None))).Id);
      Assert.Equal(seed.Token.Id, (await tokens.FindByReferenceIdAsync(seed.Token.ReferenceId!, CancellationToken.None))!.Id);
      Assert.Equal(seed.Token.Id, Assert.Single(await TestUtils.ToListAsync(tokens.FindByAuthorizationIdAsync(seed.Authorization.Id, CancellationToken.None))).Id);
      // The indexes that stay keep working
      Assert.Equal(seed.Token.Id, Assert.Single(await TestUtils.ToListAsync(tokens.FindBySubjectAsync("anna", CancellationToken.None))).Id);
      Assert.Equal(seed.Authorization.Id, Assert.Single(await TestUtils.ToListAsync(authorizations.FindByApplicationIdAsync(seed.Application.Id, CancellationToken.None))).Id);
      // Token lookups expire with the token
      var lookup = await DynamoDbItems.GetAsync<LookupItem>(fixture.Client, tableName, Lookups.ReferenceId(seed.Token.ReferenceId!), Lookups.SortKey, CancellationToken.None, consistentRead: true);
      var storedToken = (await tokens.FindByIdAsync(seed.Token.Id, CancellationToken.None))!;
      Assert.NotNull(storedToken.TTL);
      Assert.Equal(storedToken.TTL, lookup!.TTL);
    }
    finally
    {
      await fixture.Client.DeleteTableAsync(tableName);
    }
  }

  [Fact]
  public async Task Should_WriteNothingNew_When_MigratingTwice()
  {
    // Arrange
    var tableName = await LegacyTable.CreateAsync(fixture.Client);

    try
    {
      await SeedLegacyData(tableName);
      var options = new OpenIddictDynamoDbOptions { Database = fixture.Client, DefaultTableName = tableName };
      var first = await DynamoDbTableSetup.MigrateAsync(options, fixture.Client);
      var itemsAfterFirst = await LegacyTable.CountItemsAsync(fixture.Client, tableName);

      // Act
      var second = await DynamoDbTableSetup.MigrateAsync(options, fixture.Client);

      // Assert
      Assert.Equal(first, second);
      Assert.Equal(itemsAfterFirst, await LegacyTable.CountItemsAsync(fixture.Client, tableName));
    }
    finally
    {
      await fixture.Client.DeleteTableAsync(tableName);
    }
  }

  [Fact]
  public async Task Should_KeepStoresWorking_When_LegacyIndexesAreStillPresent()
  {
    // Arrange, version 4 runs against the table before the indexes are removed
    var tableName = await LegacyTable.CreateAsync(fixture.Client);

    try
    {
      var options = TestUtils.GetOptions(new() { Database = fixture.Client, DefaultTableName = tableName });
      await DynamoDbTableSetup.MigrateAsync(options.CurrentValue, fixture.Client);
      var applications = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(options);
      var application = new OpenIddictDynamoDbApplication { ClientId = "new-client", RedirectUris = ["https://new.example.com/cb"] };

      // Act
      await applications.CreateAsync(application, CancellationToken.None);
      application.ClientId = "renamed-client";
      await applications.UpdateAsync(application, CancellationToken.None);

      // Assert
      Assert.Null(await applications.FindByClientIdAsync("new-client", CancellationToken.None));
      Assert.Equal(application.Id, (await applications.FindByClientIdAsync("renamed-client", CancellationToken.None))!.Id);
      Assert.Single(await TestUtils.ToListAsync(applications.FindByRedirectUriAsync("https://new.example.com/cb", CancellationToken.None)));
      await applications.DeleteAsync(application, CancellationToken.None);
      Assert.Null(await applications.FindByClientIdAsync("renamed-client", CancellationToken.None));
      Assert.Empty(await TestUtils.ToListAsync(applications.FindByRedirectUriAsync("https://new.example.com/cb", CancellationToken.None)));
    }
    finally
    {
      await fixture.Client.DeleteTableAsync(tableName);
    }
  }

  [Fact]
  public async Task Should_RemoveLegacyIndexes_When_Requested()
  {
    // Arrange
    var tableName = await LegacyTable.CreateAsync(fixture.Client);

    try
    {
      var seed = await SeedLegacyData(tableName);
      var options = new OpenIddictDynamoDbOptions { Database = fixture.Client, DefaultTableName = tableName };
      await DynamoDbTableSetup.MigrateAsync(options, fixture.Client);

      // Act
      await DynamoDbTableSetup.RemoveLegacyIndexesAsync(options, fixture.Client);

      // Assert
      var table = await fixture.Client.DescribeTableAsync(tableName);
      Assert.Equal(TableStatus.ACTIVE, table.Table.TableStatus);
      Assert.Equal(["ApplicationId-index", "Subject-index"], table.Table.GlobalSecondaryIndexes.Select(x => x.IndexName).Order());
      var applications = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(TestUtils.GetOptions(new() { Database = fixture.Client, DefaultTableName = tableName }));
      Assert.Equal(seed.Application.Id, (await applications.FindByClientIdAsync(seed.Application.ClientId!, CancellationToken.None))!.Id);

      // Running it again on a table without the indexes is a no-op
      await DynamoDbTableSetup.RemoveLegacyIndexesAsync(options, fixture.Client);
    }
    finally
    {
      await fixture.Client.DeleteTableAsync(tableName);
    }
  }

  [Fact]
  public async Task Should_WarnOnlyWhileLegacyIndexesExist()
  {
    // Arrange
    var tableName = await LegacyTable.CreateAsync(fixture.Client);

    try
    {
      var loggerProvider = new TestLoggerProvider();
      var services = new ServiceCollection();
      services.AddLogging(x => x.AddProvider(loggerProvider));
      services.AddOpenIddict().AddCore().UseDynamoDb().UseDatabase(fixture.Client).SetDefaultTableName(tableName);
      var provider = services.BuildServiceProvider();

      // Act
      await OpenIddictDynamoDbSetup.EnsureInitializedAsync(provider);
      var warned = loggerProvider.Entries.Any(x => x.Level == LogLevel.Warning && x.Message.Contains("indexes of version 3"));
      await OpenIddictDynamoDbSetup.MigrateAsync(provider);
      await OpenIddictDynamoDbSetup.RemoveLegacyIndexesAsync(provider);
      loggerProvider.Entries.Clear();
      await OpenIddictDynamoDbSetup.EnsureInitializedAsync(provider);

      // Assert
      Assert.True(warned);
      Assert.DoesNotContain(loggerProvider.Entries, x => x.Level == LogLevel.Warning);
    }
    finally
    {
      await fixture.Client.DeleteTableAsync(tableName);
    }
  }

  [Fact]
  public async Task Should_PointAtOneApplicationAndWarn_When_LegacyApplicationsShareAClientId()
  {
    // Arrange, version 3 enforced no uniqueness, so a race could leave two applications with one client id
    var tableName = await LegacyTable.CreateAsync(fixture.Client);

    try
    {
      var context = new DynamoDBContextBuilder().WithDynamoDBClient(() => fixture.Client).Build();
      var save = new SaveConfig { OverrideTableName = tableName };
      var first = new OpenIddictDynamoDbApplication { ClientId = "twin" };
      var second = new OpenIddictDynamoDbApplication { ClientId = "twin" };
      await context.SaveAsync(first, save);
      await context.SaveAsync(second, save);
      var loggerProvider = new TestLoggerProvider();
      var services = new ServiceCollection();
      services.AddLogging(x => x.AddProvider(loggerProvider));
      services.AddOpenIddict().AddCore().UseDynamoDb().UseDatabase(fixture.Client).SetDefaultTableName(tableName);

      // Act
      var result = await OpenIddictDynamoDbSetup.MigrateAsync(services.BuildServiceProvider());

      // Assert
      Assert.Equal(2, result.Applications);
      var applications = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(TestUtils.GetOptions(new() { Database = fixture.Client, DefaultTableName = tableName }));
      var found = await applications.FindByClientIdAsync("twin", CancellationToken.None);
      Assert.Contains(found!.Id, new[] { first.Id, second.Id });
      Assert.Contains(loggerProvider.Entries, x => x.Level == LogLevel.Warning && x.Message.Contains("share the value of CLIENTID#twin"));
    }
    finally
    {
      await fixture.Client.DeleteTableAsync(tableName);
    }
  }

  private sealed record LegacySeed(
    OpenIddictDynamoDbApplication Application, OpenIddictDynamoDbScope Scope, OpenIddictDynamoDbAuthorization Authorization, OpenIddictDynamoDbToken Token);

  // Writes items the way version 3 did: through the object persistence model, with redirect and
  // scope lookup items but without the lookups version 4 reads
  private async Task<LegacySeed> SeedLegacyData(string tableName)
  {
    var context = new DynamoDBContextBuilder().WithDynamoDBClient(() => fixture.Client).Build();
    var save = new SaveConfig { OverrideTableName = tableName };
    var application = new OpenIddictDynamoDbApplication
    {
      ClientId = "portal",
      RedirectUris = ["https://app.example.com/callback"],
      PostLogoutRedirectUris = ["https://app.example.com/logout"],
    };
    var scope = new OpenIddictDynamoDbScope { Name = "api", Resources = ["resource-server"] };
    var authorization = new OpenIddictDynamoDbAuthorization
    {
      ApplicationId = application.Id,
      Subject = "anna",
      Status = Statuses.Valid,
      Type = AuthorizationTypes.Permanent,
      CreationDate = DateTime.UtcNow,
    };
    var token = new OpenIddictDynamoDbToken
    {
      ApplicationId = application.Id,
      AuthorizationId = authorization.Id,
      Subject = "anna",
      Status = Statuses.Valid,
      Type = TokenTypeHints.RefreshToken,
      ReferenceId = "reference",
      CreationDate = DateTime.UtcNow,
      ExpirationDate = DateTime.UtcNow.AddDays(1),
      TTL = DateTime.UtcNow.AddDays(1),
    };

    await context.SaveAsync(application, save);
    await context.SaveAsync(new OpenIddictDynamoDbApplicationRedirect { ApplicationId = application.Id, RedirectUri = "https://app.example.com/callback", RedirectType = RedirectType.RedirectUri }, save);
    await context.SaveAsync(new OpenIddictDynamoDbApplicationRedirect { ApplicationId = application.Id, RedirectUri = "https://app.example.com/logout", RedirectType = RedirectType.PostLogoutRedirectUri }, save);
    await context.SaveAsync(scope, save);
    await context.SaveAsync(new OpenIddictDynamoDbScopeLookup("api", LookupType.Name) { ScopeId = scope.Id }, save);
    await context.SaveAsync(new OpenIddictDynamoDbScopeLookup("resource-server", LookupType.Resource, scope.Id) { ScopeId = scope.Id }, save);
    await context.SaveAsync(authorization, save);
    await context.SaveAsync(token, save);

    return new LegacySeed(application, scope, authorization, token);
  }
}
