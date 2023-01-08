using Amazon.DynamoDBv2.DataModel;

namespace OpenIddict.AmazonDynamoDB;

[DynamoDBTable(Constants.DefaultTableName)]
public class OpenIddictDynamoDbScopeResource
{
  [DynamoDBHashKey]
  public string PartitionKey
  {
    get => $"SCOPE#{ScopeId}";
    private set { }
  }
  [DynamoDBRangeKey]
  public string? SortKey
  {
    get => $"RESOURCE#{ScopeResource}";
    set { }
  }
  public string? ScopeId { get; set; }
  public string? ScopeResource { get; set; }
}
