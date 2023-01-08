using Microsoft.Extensions.Options;
using Moq;

namespace OpenIddict.AmazonDynamoDB.Tests;

public static class TestUtils
{
  public static IOptionsMonitor<OpenIddictDynamoDbOptions> GetOptions(OpenIddictDynamoDbOptions options)
  {
    options.DefaultTableName = options.DefaultTableName == "openiddict"
      ? DatabaseFixture.TableName : options.DefaultTableName;
    var mock = new Mock<IOptionsMonitor<OpenIddictDynamoDbOptions>>();
    mock.Setup(x => x.CurrentValue).Returns(options);
    return mock.Object;
  }
}
