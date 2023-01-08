using Xunit;

namespace OpenIddict.AmazonDynamoDB.Tests;

[CollectionDefinition(Constants.DatabaseCollection)]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}
