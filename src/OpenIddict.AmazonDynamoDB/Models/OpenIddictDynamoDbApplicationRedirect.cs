using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;

namespace OpenIddict.AmazonDynamoDB;

[DynamoDBTable(Constants.DefaultTableName)]
public class OpenIddictDynamoDbApplicationRedirect : IDynamoDbItem
{
  [DynamoDBHashKey]
  public string PartitionKey
  {
    get => $"APPLICATION#{ApplicationId}";
    set { }
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

  void IDynamoDbItem.Write(Dictionary<string, AttributeValue> item)
  {
    item.Set("PartitionKey", PartitionKey);
    item.Set("SortKey", SortKey);
    item.Set("RedirectUri", RedirectUri);
    item.Set("RedirectType", (long)RedirectType);
    item.Set("ApplicationId", ApplicationId);
  }

  void IDynamoDbItem.Read(Dictionary<string, AttributeValue> item)
  {
    RedirectUri = item.GetString("RedirectUri");
    RedirectType = (RedirectType)item.GetInt32("RedirectType");
    ApplicationId = item.GetString("ApplicationId");
  }
}
