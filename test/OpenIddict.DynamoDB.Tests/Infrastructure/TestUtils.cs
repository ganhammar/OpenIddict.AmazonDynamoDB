using Microsoft.Extensions.Options;
using Moq;

namespace OpenIddict.DynamoDB.Tests;

public static class TestUtils
{
    public static IOptionsMonitor<OpenIddictDynamoDbOptions> GetOptions(OpenIddictDynamoDbOptions options)
    {
        var mock = new Mock<IOptionsMonitor<OpenIddictDynamoDbOptions>>();
        mock.Setup(x => x.CurrentValue).Returns(options);
        return mock.Object;
    }
}