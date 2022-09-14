using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace OpenIddict.DynamoDB.Tests;

public class OpenIddictDynamoDbBuilderTests
{
    [Fact]
    public void Should_ThrowException_When_ServicesIsNullInConstructor()
    {
        // Arrange
        var services = (IServiceCollection) null!;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new OpenIddictDynamoDbBuilder(services));
        Assert.Equal("services", exception.ParamName);
    }

    [Fact]
    public void Should_ThrowException_When_TryingToConfigureAndActionIsNull()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            CreateBuilder(services).Configure(null!));
        Assert.Equal("configuration", exception.ParamName);
    }

    [Fact]
    public void Should_ThrowException_When_SetApplicationsTableNameAndNameIsNull()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            CreateBuilder(services).SetApplicationsTableName(null!));
        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Should_SetTableName_When_CallingSetApplicationsTableName()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        CreateBuilder(services).SetApplicationsTableName("test");

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<OpenIddictDynamoDbOptions>>().CurrentValue;
        Assert.Equal("test", options.ApplicationsTableName);
    }

    [Fact]
    public void Should_ThrowException_When_SetApplicationRedirectsTableNameAndNameIsNull()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            CreateBuilder(services).SetApplicationRedirectsTableName(null!));
        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Should_SetTableName_When_CallingSetApplicationRedirectsTableName()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        CreateBuilder(services).SetApplicationRedirectsTableName("test");

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<OpenIddictDynamoDbOptions>>().CurrentValue;
        Assert.Equal("test", options.ApplicationRedirectsTableName);
    }

    [Fact]
    public void Should_ThrowException_When_SetAuthorizationsTableNameAndNameIsNull()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            CreateBuilder(services).SetAuthorizationsTableName(null!));
        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Should_SetTableName_When_CallingSetAuthorizationsTableName()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        CreateBuilder(services).SetAuthorizationsTableName("test");

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<OpenIddictDynamoDbOptions>>().CurrentValue;
        Assert.Equal("test", options.AuthorizationsTableName);
    }

    [Fact]
    public void Should_ThrowException_When_SetTokensTableNameAndNameIsNull()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            CreateBuilder(services).SetTokensTableName(null!));
        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Should_SetTableName_When_CallingSetTokensTableName()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        CreateBuilder(services).SetTokensTableName("test");

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<OpenIddictDynamoDbOptions>>().CurrentValue;
        Assert.Equal("test", options.TokensTableName);
    }

    [Fact]
    public void Should_ThrowException_When_SetScopesTableNameAndNameIsNull()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            CreateBuilder(services).SetScopesTableName(null!));
        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Should_SetTableName_When_CallingSetScopesTableName()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        CreateBuilder(services).SetScopesTableName("test");

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<OpenIddictDynamoDbOptions>>().CurrentValue;
        Assert.Equal("test", options.ScopesTableName);
    }

    private static OpenIddictDynamoDbBuilder CreateBuilder(IServiceCollection services)
        => services.AddOpenIddict().AddCore().UseDynamoDb();
}