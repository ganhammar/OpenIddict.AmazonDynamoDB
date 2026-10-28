using Amazon.DynamoDBv2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using Xunit;

namespace OpenIddict.AmazonDynamoDB.UpgradeTests;

public class UpgradeTests(LegacyTableFixture fixture) : IClassFixture<LegacyTableFixture>
{
  private IOpenIddictApplicationManager Applications => fixture.Services.GetRequiredService<IOpenIddictApplicationManager>();

  private IOpenIddictScopeManager Scopes => fixture.Services.GetRequiredService<IOpenIddictScopeManager>();

  private IOpenIddictAuthorizationManager Authorizations => fixture.Services.GetRequiredService<IOpenIddictAuthorizationManager>();

  private IOpenIddictTokenManager Tokens => fixture.Services.GetRequiredService<IOpenIddictTokenManager>();

  // The last seeded application is changed and deleted by Should_UpdateAndDeleteMigratedApplications
  private IEnumerable<SeedApplication> UntouchedApplications => fixture.Seed.Applications.SkipLast(1);

  [Fact]
  public void Should_WarnAboutLegacyIndexes_When_SettingUpTheLegacyTable()
  {
    Assert.Contains(fixture.SetupLog, x => x.Level == LogLevel.Warning && x.Message.Contains("indexes of version 3"));
    Assert.Equal(8, fixture.IndexCountBeforeRemoval);
  }

  [Fact]
  public void Should_WriteLookupsForEveryItem_When_Migrating()
  {
    var applications = fixture.Seed.Applications;
    Assert.Equal(applications.Count, fixture.FirstMigration.Applications);
    Assert.Equal(applications.Sum(x => x.RedirectUris.Count + x.PostLogoutRedirectUris.Count), fixture.FirstMigration.Redirects);
    Assert.Equal(fixture.Seed.Scopes.Count, fixture.FirstMigration.Scopes);
    Assert.Equal(applications.Sum(x => x.Authorizations.Sum(y => y.Tokens.Count)), fixture.FirstMigration.Tokens);
    Assert.Equal(fixture.FirstMigration, fixture.SecondMigration);
  }

  [Fact]
  public async Task Should_HaveKeptOnlyTheSharedIndexes()
  {
    var table = await fixture.Client.DescribeTableAsync(fixture.TableName);

    Assert.Equal(TableStatus.ACTIVE, table.Table.TableStatus);
    Assert.Equal(["ApplicationId-index", "Subject-index"], table.Table.GlobalSecondaryIndexes.Select(x => x.IndexName).Order());
  }

  [Fact]
  public async Task Should_FindEveryApplication_ByClientIdAndRedirectUris()
  {
    foreach (var seeded in UntouchedApplications)
    {
      var application = await Applications.FindByClientIdAsync(seeded.ClientId);
      Assert.Equal(seeded.Id, await Applications.GetIdAsync(application!));
      Assert.Equal(seeded.DisplayName, await Applications.GetDisplayNameAsync(application!));
      Assert.Equal(seeded.RedirectUris.Order(), (await Applications.GetRedirectUrisAsync(application!)).Order());

      foreach (var uri in seeded.RedirectUris)
      {
        Assert.Contains(await Applications.FindByRedirectUriAsync(uri).ToListAsync(), x => seeded.Id == (x as OpenIddictDynamoDbApplication)!.Id);
      }

      foreach (var uri in seeded.PostLogoutRedirectUris)
      {
        Assert.Contains(await Applications.FindByPostLogoutRedirectUriAsync(uri).ToListAsync(), x => seeded.Id == (x as OpenIddictDynamoDbApplication)!.Id);
      }
    }

    // The shared redirect URI lists every application
    var shared = await Applications.FindByRedirectUriAsync("https://shared.example.com/callback").ToListAsync();
    Assert.Equal(UntouchedApplications.Count(), shared.Count(x => UntouchedApplications.Any(y => y.Id == (x as OpenIddictDynamoDbApplication)!.Id)));
  }

  [Fact]
  public async Task Should_FindEveryScope_ByNameAndResource()
  {
    foreach (var seeded in fixture.Seed.Scopes)
    {
      var scope = await Scopes.FindByNameAsync(seeded.Name);
      Assert.Equal(seeded.Id, await Scopes.GetIdAsync(scope!));
      Assert.Equal(seeded.Resources.Order(), (await Scopes.GetResourcesAsync(scope!)).Order());
      Assert.Single(await Scopes.FindByResourceAsync(seeded.Resources[0]).ToListAsync());
    }

    Assert.Equal(fixture.Seed.Scopes.Count, (await Scopes.FindByResourceAsync("shared-resource").ToListAsync()).Count);
    Assert.Equal(fixture.Seed.Scopes.Count, (await Scopes.FindByNamesAsync([.. fixture.Seed.Scopes.Select(x => x.Name)]).ToListAsync()).Count);
  }

  [Fact]
  public async Task Should_FindEveryToken_ByReferenceIdAuthorizationAndSubject()
  {
    foreach (var application in UntouchedApplications)
    {
      foreach (var authorization in application.Authorizations)
      {
        var byAuthorization = await Tokens.FindByAuthorizationIdAsync(authorization.Id).ToListAsync();
        Assert.Equal(authorization.Tokens.Select(x => x.Id).Order(), byAuthorization.Select(x => (x as OpenIddictDynamoDbToken)!.Id).Order());

        foreach (var token in authorization.Tokens)
        {
          var byReference = await Tokens.FindByReferenceIdAsync(token.ReferenceId);
          Assert.Equal(token.Id, await Tokens.GetIdAsync(byReference!));
          Assert.StartsWith("payload-", await Tokens.GetPayloadAsync(byReference!));
        }

        // The indexes that stay keep working
        Assert.Equal(2, (await Tokens.FindBySubjectAsync(authorization.Subject).ToListAsync()).Count);
        Assert.Single(await Authorizations.FindBySubjectAsync(authorization.Subject).ToListAsync());
      }

      Assert.Equal(4, (await Tokens.FindByApplicationIdAsync(application.Id).ToListAsync()).Count);
    }
  }

  [Fact]
  public async Task Should_EnforceUniqueness_AgainstMigratedData()
  {
    var existing = fixture.Seed.Applications[0];
    var store = fixture.Services.GetRequiredService<IOpenIddictApplicationStore<OpenIddictDynamoDbApplication>>();
    var scopeStore = fixture.Services.GetRequiredService<IOpenIddictScopeStore<OpenIddictDynamoDbScope>>();

    await Assert.ThrowsAsync<OpenIddictExceptions.ValidationException>(async () =>
      await store.CreateAsync(new OpenIddictDynamoDbApplication { ClientId = existing.ClientId }, CancellationToken.None));
    await Assert.ThrowsAsync<OpenIddictExceptions.ValidationException>(async () =>
      await scopeStore.CreateAsync(new OpenIddictDynamoDbScope { Name = fixture.Seed.Scopes[0].Name }, CancellationToken.None));
  }

  [Fact]
  public async Task Should_UpdateAndDeleteMigratedApplications()
  {
    var seeded = fixture.Seed.Applications[^1];
    var application = (await Applications.FindByClientIdAsync(seeded.ClientId))!;
    var newClientId = $"renamed-{Guid.NewGuid()}";
    var descriptor = new OpenIddictApplicationDescriptor();
    await Applications.PopulateAsync(descriptor, application);
    descriptor.ClientId = newClientId;
    descriptor.RedirectUris.Clear();
    descriptor.RedirectUris.Add(new Uri("https://renamed.example.com/callback"));

    await Applications.UpdateAsync(application, descriptor);
    Assert.Null(await Applications.FindByClientIdAsync(seeded.ClientId));
    Assert.Equal(seeded.Id, await Applications.GetIdAsync((await Applications.FindByClientIdAsync(newClientId))!));
    Assert.Empty(await Applications.FindByRedirectUriAsync(seeded.RedirectUris[0]).ToListAsync());
    Assert.Single(await Applications.FindByRedirectUriAsync("https://renamed.example.com/callback").ToListAsync());

    await Applications.DeleteAsync(application);
    Assert.Null(await Applications.FindByClientIdAsync(newClientId));
    Assert.Empty(await Applications.FindByRedirectUriAsync("https://renamed.example.com/callback").ToListAsync());
    Assert.DoesNotContain(await Applications.FindByRedirectUriAsync("https://shared.example.com/callback").ToListAsync(), x => (x as OpenIddictDynamoDbApplication)!.Id == seeded.Id);
  }
}
