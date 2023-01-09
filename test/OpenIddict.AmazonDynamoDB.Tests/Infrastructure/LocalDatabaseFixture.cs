namespace OpenIddict.AmazonDynamoDB.Tests;

public class LocalDatabaseFixture : DatabaseFixture
{
  public LocalDatabaseFixture()
    : base(new("test", "test"), new() { ServiceURL = "http://localhost:8000" }) { }
}
