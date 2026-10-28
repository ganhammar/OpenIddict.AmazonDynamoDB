using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;

namespace OpenIddict.AmazonDynamoDB;

[DynamoDBTable(Constants.DefaultTableName)]
public class OpenIddictDynamoDbScopeLookup : IDynamoDbItem
{
  public OpenIddictDynamoDbScopeLookup(string value, LookupType type, string? separator = default)
  {
    LookupValue = value;
    LookupType = type;
    LookupSeparator = separator;
  }

  public OpenIddictDynamoDbScopeLookup() { }

  [DynamoDBHashKey]
  public string PartitionKey
  {
    get => $"SCOPELOOKUP#{LookupValue}";
    set { }
  }
  [DynamoDBRangeKey]
  public string? SortKey
  {
    get => $"TYPE#{LookupType}#SEPARATOR#{LookupSeparator}";
    set { }
  }
  public string? LookupValue { get; set; }
  public LookupType LookupType { get; set; }
  public string? LookupSeparator { get; set; }
  public string? ScopeId { get; set; }

  void IDynamoDbItem.Write(Dictionary<string, AttributeValue> item)
  {
    item.Set("PartitionKey", PartitionKey);
    item.Set("SortKey", SortKey);
    item.Set("LookupValue", LookupValue);
    item.Set("LookupType", (long)LookupType);
    item.Set("LookupSeparator", LookupSeparator);
    item.Set("ScopeId", ScopeId);
  }

  void IDynamoDbItem.Read(Dictionary<string, AttributeValue> item)
  {
    LookupValue = item.GetString("LookupValue");
    LookupType = (LookupType)item.GetInt32("LookupType");
    LookupSeparator = item.GetString("LookupSeparator");
    ScopeId = item.GetString("ScopeId");
  }
}
