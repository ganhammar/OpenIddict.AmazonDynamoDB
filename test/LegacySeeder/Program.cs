using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.AmazonDynamoDB;
using static OpenIddict.Abstractions.OpenIddictConstants;

// Usage: LegacySeeder <table name>. Creates the table with version 3 and fills it through version 3's
// stores and OpenIddict's managers, then prints what it wrote as JSON on the last line.
var tableName = args.Length > 0 ? args[0] : throw new ArgumentException("Pass the table name");
var endpoint = Environment.GetEnvironmentVariable("DYNAMODB_ENDPOINT") ?? "http://localhost:8000";
using var client = new AmazonDynamoDBClient(
  new BasicAWSCredentials("test", "test"),
  new AmazonDynamoDBConfig { ServiceURL = endpoint });

var services = new ServiceCollection();
services.AddLogging();
services.AddOpenIddict().AddCore(core => core.UseDynamoDb().UseDatabase(client).SetDefaultTableName(tableName));

await using var provider = services.BuildServiceProvider();
OpenIddictDynamoDbSetup.EnsureInitialized(provider);

var applications = provider.GetRequiredService<IOpenIddictApplicationManager>();
var scopes = provider.GetRequiredService<IOpenIddictScopeManager>();
var authorizations = provider.GetRequiredService<IOpenIddictAuthorizationManager>();
var tokens = provider.GetRequiredService<IOpenIddictTokenManager>();
var seed = new Seed();

for (var i = 0; i < 3; i++)
{
  var scope = await scopes.CreateAsync(new OpenIddictScopeDescriptor
  {
    Name = $"scope-{i}",
    Resources = { $"resource-{i}", "shared-resource" },
  });
  seed.Scopes.Add(new((await scopes.GetIdAsync(scope))!, $"scope-{i}", [$"resource-{i}", "shared-resource"]));
}

for (var i = 0; i < 5; i++)
{
  var redirectUris = new[] { $"https://app-{i}.example.com/callback", "https://shared.example.com/callback" };
  var postLogout = new[] { $"https://app-{i}.example.com/logout" };
  var descriptor = new OpenIddictApplicationDescriptor
  {
    ClientId = $"client-{i}",
    ClientSecret = i % 2 == 0 ? $"secret-{i}" : null,
    ClientType = i % 2 == 0 ? ClientTypes.Confidential : ClientTypes.Public,
    DisplayName = $"Application {i}",
    Permissions = { Permissions.Endpoints.Authorization, Permissions.Endpoints.Token, Permissions.GrantTypes.AuthorizationCode },
  };
  foreach (var uri in redirectUris)
  {
    descriptor.RedirectUris.Add(new Uri(uri));
  }
  foreach (var uri in postLogout)
  {
    descriptor.PostLogoutRedirectUris.Add(new Uri(uri));
  }
  var application = await applications.CreateAsync(descriptor);
  var applicationId = (await applications.GetIdAsync(application))!;
  var seededApplication = new SeedApplication(applicationId, $"client-{i}", $"Application {i}", redirectUris.ToList(), postLogout.ToList(), []);

  for (var j = 0; j < 2; j++)
  {
    var subject = $"user-{i}-{j}";
    var authorization = await authorizations.CreateAsync(new OpenIddictAuthorizationDescriptor
    {
      ApplicationId = applicationId,
      Subject = subject,
      Status = Statuses.Valid,
      Type = AuthorizationTypes.Permanent,
      Scopes = { "scope-0" },
    });
    var authorizationId = (await authorizations.GetIdAsync(authorization))!;
    var tokenIds = new List<SeedToken>();

    for (var k = 0; k < 2; k++)
    {
      var referenceId = $"ref-{i}-{j}-{k}";
      var token = await tokens.CreateAsync(new OpenIddictTokenDescriptor
      {
        ApplicationId = applicationId,
        AuthorizationId = authorizationId,
        Subject = subject,
        Status = Statuses.Valid,
        Type = k == 0 ? TokenTypeHints.RefreshToken : TokenTypeHints.AccessToken,
        ReferenceId = referenceId,
        Payload = $"payload-{i}-{j}-{k}",
        CreationDate = DateTimeOffset.UtcNow,
        ExpirationDate = DateTimeOffset.UtcNow.AddDays(1),
      });
      tokenIds.Add(new((await tokens.GetIdAsync(token))!, referenceId));
    }

    seededApplication.Authorizations.Add(new(authorizationId, subject, tokenIds));
  }

  seed.Applications.Add(seededApplication);
}

Console.WriteLine(JsonSerializer.Serialize(seed));

public sealed class Seed
{
  public List<SeedScope> Scopes { get; } = [];
  public List<SeedApplication> Applications { get; } = [];
}

public sealed record SeedScope(string Id, string Name, List<string> Resources);

public sealed record SeedToken(string Id, string ReferenceId);

public sealed record SeedAuthorization(string Id, string Subject, List<SeedToken> Tokens);

public sealed record SeedApplication(
  string Id, string ClientId, string DisplayName, List<string> RedirectUris, List<string> PostLogoutRedirectUris, List<SeedAuthorization> Authorizations);
