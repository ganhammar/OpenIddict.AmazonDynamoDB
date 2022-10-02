using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace OpenIddict.AmazonDynamoDB.Tests;

public class OpenIddictDynamoDbExtensionsTests
{
    [Fact]
    public void Should_ThrowException_When_CallingUseDynamoDbAndBuilderIsNull()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            OpenIddictDynamoDbExtensions.UseDynamoDb(null!));

        Assert.Equal("builder", exception.ParamName);
    }
}