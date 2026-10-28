using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using OpenIddict.AmazonDynamoDB;
using static OpenIddict.Abstractions.OpenIddictConstants;

// An application, a scope, an authorization and a token go through OpenIddict's managers and the
// stores and come back out, against DynamoDB Local at DYNAMODB_ENDPOINT (default http://localhost:8000)
var endpoint = Environment.GetEnvironmentVariable("DYNAMODB_ENDPOINT") ?? "http://localhost:8000";
var tableName = $"native-aot-{Guid.NewGuid()}";
using var client = new AmazonDynamoDBClient(
  new BasicAWSCredentials("test", "test"),
  new AmazonDynamoDBConfig { ServiceURL = endpoint });

var services = new ServiceCollection();
services.AddLogging(logging => logging.AddConsole());
services.AddOpenIddict()
  .AddCore(core => core
    .UseDynamoDb()
    .UseDatabase(client)
    .SetDefaultTableName(tableName));
services.AddOpenIddict().AddCore(core => core.SetDefaultApplicationEntity<CustomApplication>());
services.AddSingleton<OpenIddictDynamoDbApplicationStore<CustomApplication>>();
services.AddSingleton<IOpenIddictApplicationStore<CustomApplication>>(
  provider => provider.GetRequiredService<OpenIddictDynamoDbApplicationStore<CustomApplication>>());

await using var provider = services.BuildServiceProvider();
await OpenIddictDynamoDbSetup.EnsureInitializedAsync(provider);

try
{
  var applications = provider.GetRequiredService<IOpenIddictApplicationManager>();
  var scopes = provider.GetRequiredService<IOpenIddictScopeManager>();
  var authorizations = provider.GetRequiredService<IOpenIddictAuthorizationManager>();
  var tokens = provider.GetRequiredService<IOpenIddictTokenManager>();

  var application = (CustomApplication)await applications.CreateAsync(new OpenIddictApplicationDescriptor
  {
    ClientId = "portal",
    ClientType = ClientTypes.Public,
    DisplayName = "Portal",
    RedirectUris = { new Uri("https://portal.example.com/callback") },
    Permissions = { Permissions.Endpoints.Authorization, Permissions.GrantTypes.AuthorizationCode },
  });
  application.Owner = "anna";
  await applications.UpdateAsync(application);

  await scopes.CreateAsync(new OpenIddictScopeDescriptor { Name = "api", Resources = { "resource-server" } });

  var authorization = await authorizations.CreateAsync(new OpenIddictAuthorizationDescriptor
  {
    ApplicationId = application.Id,
    Subject = "anna",
    Status = Statuses.Valid,
    Type = AuthorizationTypes.Permanent,
    Scopes = { "api" },
  });
  var token = await tokens.CreateAsync(new OpenIddictTokenDescriptor
  {
    ApplicationId = application.Id,
    AuthorizationId = await authorizations.GetIdAsync(authorization),
    Subject = "anna",
    Status = Statuses.Valid,
    Type = TokenTypeHints.RefreshToken,
    ReferenceId = "reference",
    ExpirationDate = DateTimeOffset.UtcNow.AddDays(1),
  });

  var byClientId = (CustomApplication?)await applications.FindByClientIdAsync("portal")
    ?? throw new Exception("The application was not found by its client id");
  Ensure(byClientId.Owner == "anna", "custom attribute");
  Ensure((await applications.GetRedirectUrisAsync(byClientId)).Single() == "https://portal.example.com/callback", "redirect uri");
  Ensure(await applications.FindByRedirectUriAsync("https://portal.example.com/callback").AnyAsync(), "redirect lookup");
  Ensure(await scopes.FindByResourceAsync("resource-server").AnyAsync(), "scope by resource");
  Ensure(await tokens.FindByReferenceIdAsync("reference") is not null, "token by reference id");
  Ensure(await authorizations.FindBySubjectAsync("anna").AnyAsync(), "authorization by subject");
  Ensure(await tokens.FindByAuthorizationIdAsync(await authorizations.GetIdAsync(authorization) ?? "").AnyAsync(), "token by authorization");
  Ensure(await tokens.CountAsync() == 1, "token count");

  Console.WriteLine("Native AOT: the stores round-tripped an application, a scope, an authorization and a token");
}
finally
{
  await client.DeleteTableAsync(new DeleteTableRequest { TableName = tableName });
}

static void Ensure(bool condition, string what)
{
  if (!condition)
  {
    throw new Exception($"Failed: {what}");
  }
}

public class CustomApplication : OpenIddictDynamoDbApplication
{
  public string? Owner { get; set; }

  protected override void WriteAttributes(Dictionary<string, AttributeValue> item)
  {
    base.WriteAttributes(item);
    item.Set("Owner", Owner);
  }

  protected override void ReadAttributes(Dictionary<string, AttributeValue> item)
  {
    base.ReadAttributes(item);
    Owner = item.GetString("Owner");
  }
}
