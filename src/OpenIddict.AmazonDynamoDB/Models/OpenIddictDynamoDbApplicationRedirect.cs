using Amazon.DynamoDBv2.DataModel;

namespace OpenIddict.AmazonDynamoDB;

[DynamoDBTable(Constants.DefaultTableName)]
public class OpenIddictDynamoDbApplicationRedirect
{
  [DynamoDBHashKey]
  public string PartitionKey
  {
    get => $"APPLICATION#{ApplicationId}";
    private set { }
  }
  [DynamoDBRangeKey]
  public string? SortKey
  {
    get => $"REDIRECT#{RedirectUri}#{RedirectType}";
    set { }
  }
  public string? RedirectUri { get; set; }
  public RedirectType RedirectType { get; set; }
  public string? ApplicationId { get; set; }
}
