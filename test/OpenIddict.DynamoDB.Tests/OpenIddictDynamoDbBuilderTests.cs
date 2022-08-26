using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace OpenIddict.DynamoDB.Tests;

public class OpenIddictDynamoDbBuilderTests
{
    [Fact]
    public void Should_ThrowException_When_ServicesIsNullInConstructor()
    {
        // Arrange
        var services = (IServiceCollection) null!;

        // Act
        var exception = Assert.Throws<ArgumentNullException>(() => new OpenIddictDynamoDbBuilder(services));

        // Assert
        Assert.Equal("services", exception.ParamName);
    }
}