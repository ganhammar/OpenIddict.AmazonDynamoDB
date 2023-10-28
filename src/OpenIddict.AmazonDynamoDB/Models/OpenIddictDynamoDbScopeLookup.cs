using Amazon.DynamoDBv2.DataModel;

namespace OpenIddict.AmazonDynamoDB;

[DynamoDBTable(Constants.DefaultTableName)]
public class OpenIddictDynamoDbScopeLookup
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
    private set { }
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
}

public enum LookupType
{
  Resource,
  Name
}
