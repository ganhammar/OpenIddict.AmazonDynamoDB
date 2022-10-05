using Amazon.DynamoDBv2;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace OpenIddict.AmazonDynamoDB.Tests;

[Collection("Sequential")]
public class OpenIddictDynamoDbSetupTests
{
    [Fact]
    public async Task Should_SetupTables_When_CalledSynchronously()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new()
            {
                Database = database.Client,
            });

            // Act
            OpenIddictDynamoDbSetup.EnsureInitialized(options);

            // Assert
            var tableNames = await database.Client.ListTablesAsync();
            Assert.Contains(Constants.DefaultApplicationRedirectsTableName, tableNames.TableNames);
            Assert.Contains(Constants.DefaultApplicationTableName, tableNames.TableNames);
            Assert.Contains(Constants.DefaultAuthorizationTableName, tableNames.TableNames);
            Assert.Contains(Constants.DefaultScopeResourceTableName, tableNames.TableNames);
            Assert.Contains(Constants.DefaultScopeTableName, tableNames.TableNames);
            Assert.Contains(Constants.DefaultTokenTableName, tableNames.TableNames);
        }
    }

    [Fact]
    public async Task Should_SetupTables_When_CalledSynchronouslyWithServiceProvider()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var services = new ServiceCollection();
            CreateBuilder(services).UseDatabase(database.Client);

            // Act
            OpenIddictDynamoDbSetup.EnsureInitialized(services.BuildServiceProvider());

            // Assert
            var tableNames = await database.Client.ListTablesAsync();
            Assert.Contains(Constants.DefaultApplicationRedirectsTableName, tableNames.TableNames);
            Assert.Contains(Constants.DefaultApplicationTableName, tableNames.TableNames);
            Assert.Contains(Constants.DefaultAuthorizationTableName, tableNames.TableNames);
            Assert.Contains(Constants.DefaultScopeResourceTableName, tableNames.TableNames);
            Assert.Contains(Constants.DefaultScopeTableName, tableNames.TableNames);
            Assert.Contains(Constants.DefaultTokenTableName, tableNames.TableNames);
        }
    }

    [Fact]
    public async Task Should_SetupTables_When_CalledAsynchronously()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var options = TestUtils.GetOptions(new()
            {
                Database = database.Client,
            });

            // Act
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Assert
            var tableNames = await database.Client.ListTablesAsync();
            Assert.Contains(Constants.DefaultApplicationRedirectsTableName, tableNames.TableNames);
            Assert.Contains(Constants.DefaultApplicationTableName, tableNames.TableNames);
            Assert.Contains(Constants.DefaultAuthorizationTableName, tableNames.TableNames);
            Assert.Contains(Constants.DefaultScopeResourceTableName, tableNames.TableNames);
            Assert.Contains(Constants.DefaultScopeTableName, tableNames.TableNames);
            Assert.Contains(Constants.DefaultTokenTableName, tableNames.TableNames);
        }
    }

    [Fact]
    public async Task Should_SetupTables_When_CalledAsynchronouslyWithServiceProvider()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var services = new ServiceCollection();
            CreateBuilder(services).UseDatabase(database.Client);

            // Act
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(services.BuildServiceProvider());

            // Assert
            var tableNames = await database.Client.ListTablesAsync();
            Assert.Contains(Constants.DefaultApplicationRedirectsTableName, tableNames.TableNames);
            Assert.Contains(Constants.DefaultApplicationTableName, tableNames.TableNames);
            Assert.Contains(Constants.DefaultAuthorizationTableName, tableNames.TableNames);
            Assert.Contains(Constants.DefaultScopeResourceTableName, tableNames.TableNames);
            Assert.Contains(Constants.DefaultScopeTableName, tableNames.TableNames);
            Assert.Contains(Constants.DefaultTokenTableName, tableNames.TableNames);
        }
    }

    [Fact]
    public async Task Should_SetupTables_When_CalledAsynchronouslyWithDatbaseInServiceProvider()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IAmazonDynamoDB>(database.Client);
            CreateBuilder(services);

            // Act
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(services.BuildServiceProvider());

            // Assert
            var tableNames = await database.Client.ListTablesAsync();
            Assert.Contains(Constants.DefaultApplicationRedirectsTableName, tableNames.TableNames);
            Assert.Contains(Constants.DefaultApplicationTableName, tableNames.TableNames);
            Assert.Contains(Constants.DefaultAuthorizationTableName, tableNames.TableNames);
            Assert.Contains(Constants.DefaultScopeResourceTableName, tableNames.TableNames);
            Assert.Contains(Constants.DefaultScopeTableName, tableNames.TableNames);
            Assert.Contains(Constants.DefaultTokenTableName, tableNames.TableNames);
        }
    }

    [Fact]
    public async Task Should_SetupTables_When_CalledSynchronouslyWithDatbaseInServiceProvider()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IAmazonDynamoDB>(database.Client);
            CreateBuilder(services);

            // Act
            OpenIddictDynamoDbSetup.EnsureInitialized(services.BuildServiceProvider());

            // Assert
            var tableNames = await database.Client.ListTablesAsync();
            Assert.Contains(Constants.DefaultApplicationRedirectsTableName, tableNames.TableNames);
            Assert.Contains(Constants.DefaultApplicationTableName, tableNames.TableNames);
            Assert.Contains(Constants.DefaultAuthorizationTableName, tableNames.TableNames);
            Assert.Contains(Constants.DefaultScopeResourceTableName, tableNames.TableNames);
            Assert.Contains(Constants.DefaultScopeTableName, tableNames.TableNames);
            Assert.Contains(Constants.DefaultTokenTableName, tableNames.TableNames);
        }
    }

    [Fact]
    public async Task Should_SetupTablesWithDifferentNames_When_OtherIsSpecified()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var applicationRedirectsTableName = "applikation_omdirigering";
            var authorizationsTableName = "auktoriseringar";
            var scopeResourcesTableName = "ansprak_resurser";
            var scopesTableName = "ansprak";
            var tokensTableName = "bevis";
            var applicationsTableName = "applikationer";
            var options = TestUtils.GetOptions(new()
            {
                Database = database.Client,
                ApplicationRedirectsTableName = applicationRedirectsTableName,
                AuthorizationsTableName = authorizationsTableName,
                ScopeResourcesTableName = scopeResourcesTableName,
                ScopesTableName = scopesTableName,
                TokensTableName = tokensTableName,
                ApplicationsTableName = applicationsTableName,
            });

            // Act
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

            // Assert
            var tableNames = await database.Client.ListTablesAsync();
            Assert.Contains(applicationRedirectsTableName, tableNames.TableNames);
            Assert.Contains(authorizationsTableName, tableNames.TableNames);
            Assert.Contains(scopeResourcesTableName, tableNames.TableNames);
            Assert.Contains(scopesTableName, tableNames.TableNames);
            Assert.Contains(tokensTableName, tableNames.TableNames);
            Assert.Contains(applicationsTableName, tableNames.TableNames);
        }
    }

    private static OpenIddictDynamoDbBuilder CreateBuilder(IServiceCollection services)
        => services.AddOpenIddict().AddCore().UseDynamoDb();
}