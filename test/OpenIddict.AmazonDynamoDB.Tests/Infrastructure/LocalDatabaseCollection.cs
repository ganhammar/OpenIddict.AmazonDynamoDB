using Xunit;

namespace OpenIddict.AmazonDynamoDB.Tests;

[CollectionDefinition(Constants.LocalDatabaseCollection)]
public class LocalDatabaseCollection : ICollectionFixture<LocalDatabaseFixture>
{
}
