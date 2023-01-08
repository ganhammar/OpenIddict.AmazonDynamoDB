namespace OpenIddict.AmazonDynamoDB.Tests;

public class LocalDatabaseFixture : DatabaseFixture
{
  public LocalDatabaseFixture()
    : base(new() { ServiceURL = "http://localhost:8000" }) { }
}
