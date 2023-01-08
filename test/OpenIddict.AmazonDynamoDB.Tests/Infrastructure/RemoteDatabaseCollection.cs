using Xunit;

namespace OpenIddict.AmazonDynamoDB.Tests;

[CollectionDefinition(Constants.RemoteDatabaseCollection)]
public class RemoteDatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}
