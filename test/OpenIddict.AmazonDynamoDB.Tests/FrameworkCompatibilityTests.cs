using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OpenIddict.AmazonDynamoDB.Tests;

// Guards against referencing packages from a newer major version than the target framework,
// which replaces the shared framework's assemblies in the applications using the stores
[Collection(Constants.DatabaseCollection)]
public class FrameworkCompatibilityTests(DatabaseFixture fixture)
{
  private readonly IAmazonDynamoDB _client = fixture.Client;

  [Theory]
  [InlineData("Microsoft.Extensions.DependencyInjection")]
  [InlineData("Microsoft.Extensions.DependencyInjection.Abstractions")]
  public void Should_LoadAssemblyMatchingTheRuntime_When_ReferencingStores(string assemblyName)
  {
    // Act
    var assembly = Assembly.Load(assemblyName);

    // Assert
    Assert.Equal(Environment.Version.Major, assembly.GetName().Version!.Major);
  }

  [Fact]
  public void Should_UnprotectData_When_ReferencingStores()
  {
    // Arrange
    var protector = new EphemeralDataProtectionProvider().CreateProtector("test");

    // Act
    var payload = protector.Unprotect(protector.Protect("payload"));

    // Assert
    Assert.Equal("payload", payload);
  }

  [Fact]
  public async Task Should_IssueIntrospectAndRevokeReferenceToken_When_UsingDynamoDbStores()
  {
    // Arrange
    await using var app = await StartApplication();
    var clientId = await CreateClients(app);
    using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

    // Act
    var tokenResponse = await Post(client, "/connect/token", new()
    {
      ["grant_type"] = GrantTypes.ClientCredentials,
      ["client_id"] = clientId,
      ["client_secret"] = "secret",
      ["scope"] = "api",
    });
    var accessToken = tokenResponse.GetProperty("access_token").GetString()!;
    var apiResponse = await CallApi(client, accessToken);
    var activeIntrospection = await Introspect(client, accessToken);
    var revocationResponse = await client.PostAsync("/connect/revoke", new FormUrlEncodedContent(new Dictionary<string, string>
    {
      ["token"] = accessToken,
      ["client_id"] = clientId,
      ["client_secret"] = "secret",
    }));
    var revokedApiResponse = await CallApi(client, accessToken);
    var revokedIntrospection = await Introspect(client, accessToken);

    // Assert
    Assert.Equal(HttpStatusCode.OK, apiResponse.StatusCode);
    Assert.Equal(clientId, await apiResponse.Content.ReadAsStringAsync());
    Assert.True(activeIntrospection.GetProperty("active").GetBoolean());
    Assert.Equal(HttpStatusCode.OK, revocationResponse.StatusCode);
    Assert.Equal(HttpStatusCode.Unauthorized, revokedApiResponse.StatusCode);
    Assert.False(revokedIntrospection.GetProperty("active").GetBoolean());
  }

  [Fact]
  public async Task Should_RejectTokenRequest_When_ClientSecretOrScopeIsInvalid()
  {
    // Arrange
    await using var app = await StartApplication();
    var clientId = await CreateClients(app);
    using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

    // Act
    var invalidSecret = await Post(client, "/connect/token", new()
    {
      ["grant_type"] = GrantTypes.ClientCredentials,
      ["client_id"] = clientId,
      ["client_secret"] = "wrong",
    });
    var invalidScope = await Post(client, "/connect/token", new()
    {
      ["grant_type"] = GrantTypes.ClientCredentials,
      ["client_id"] = clientId,
      ["client_secret"] = "secret",
      ["scope"] = "unknown",
    });

    // Assert
    Assert.Equal(Errors.InvalidClient, invalidSecret.GetProperty("error").GetString());
    Assert.Equal(Errors.InvalidScope, invalidScope.GetProperty("error").GetString());
  }

  private async Task<WebApplication> StartApplication()
  {
    var builder = WebApplication.CreateBuilder();
    builder.Logging.ClearProviders();
    builder.WebHost.UseUrls("http://127.0.0.1:0");
    builder.Services.AddSingleton(_client);
    builder.Services.AddOpenIddict()
      .AddCore(options => options.UseDynamoDb().SetDefaultTableName(DatabaseFixture.TableName))
      .AddServer(options =>
      {
        options
          .SetTokenEndpointUris("/connect/token")
          .SetIntrospectionEndpointUris("/connect/introspect")
          .SetRevocationEndpointUris("/connect/revoke")
          .AllowClientCredentialsFlow()
          .AddEphemeralEncryptionKey()
          .AddEphemeralSigningKey()
          .UseReferenceAccessTokens();
        options.UseAspNetCore()
          .EnableTokenEndpointPassthrough()
          .DisableTransportSecurityRequirement();
      })
      .AddValidation(options =>
      {
        options.UseLocalServer();
        options.UseAspNetCore();
        options.EnableTokenEntryValidation();
      });
    builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
    builder.Services.AddAuthorization();

    var app = builder.Build();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapPost("/connect/token", (HttpContext context) =>
    {
      var request = context.GetOpenIddictServerRequest()!;
      var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, Claims.Name, Claims.Role);
      identity.SetClaim(Claims.Subject, request.ClientId);
      identity.SetScopes(request.GetScopes()).SetResources("resource-server");
      identity.SetDestinations(_ => [Destinations.AccessToken]);
      return Results.SignIn(new ClaimsPrincipal(identity), authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    });
    app.MapGet("/api", (ClaimsPrincipal user) => user.FindFirst(Claims.Subject)?.Value).RequireAuthorization();

    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(app.Services);
    await app.StartAsync();

    return app;
  }

  private static async Task<string> CreateClients(WebApplication app)
  {
    using var scope = app.Services.CreateScope();
    var applications = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
    var scopes = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();
    var clientId = $"client-{Guid.NewGuid()}";

    if (await scopes.FindByNameAsync("api") == default)
    {
      await scopes.CreateAsync(new OpenIddictScopeDescriptor { Name = "api", Resources = { "resource-server" } });
    }

    await applications.CreateAsync(new OpenIddictApplicationDescriptor
    {
      ClientId = clientId,
      ClientSecret = "secret",
      ClientType = ClientTypes.Confidential,
      Permissions =
      {
        Permissions.Endpoints.Token,
        Permissions.Endpoints.Revocation,
        Permissions.GrantTypes.ClientCredentials,
        Permissions.Prefixes.Scope + "api",
      },
    });

    if (await applications.FindByClientIdAsync("resource-server") == default)
    {
      await applications.CreateAsync(new OpenIddictApplicationDescriptor
      {
        ClientId = "resource-server",
        ClientSecret = "secret",
        ClientType = ClientTypes.Confidential,
        Permissions = { Permissions.Endpoints.Introspection },
      });
    }

    return clientId;
  }

  private static async Task<JsonElement> Post(HttpClient client, string path, Dictionary<string, string> form)
  {
    var response = await client.PostAsync(path, new FormUrlEncodedContent(form));
    return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
  }

  private static Task<JsonElement> Introspect(HttpClient client, string token) => Post(client, "/connect/introspect", new()
  {
    ["token"] = token,
    ["client_id"] = "resource-server",
    ["client_secret"] = "secret",
  });

  private static Task<HttpResponseMessage> CallApi(HttpClient client, string accessToken)
  {
    var request = new HttpRequestMessage(HttpMethod.Get, "/api");
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    return client.SendAsync(request);
  }
}
