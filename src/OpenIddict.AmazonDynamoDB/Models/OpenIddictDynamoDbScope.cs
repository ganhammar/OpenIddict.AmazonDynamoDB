using Amazon.DynamoDBv2.DataModel;

namespace OpenIddict.AmazonDynamoDB;

[DynamoDBTable(Constants.DefaultTableName)]
public class OpenIddictDynamoDbScope
{
  [DynamoDBHashKey]
  public string PartitionKey
  {
    get => $"SCOPE#{Id}";
    set { }
  }
  [DynamoDBRangeKey]
  public string? SortKey
  {
    get => $"#SCOPE#{Id}";
    set { }
  }
  public virtual string Id { get; set; } = Guid.NewGuid().ToString();
  public virtual string? ConcurrencyToken { get; set; } = Guid.NewGuid().ToString();
  public virtual string? Description { get; set; }
  public virtual Dictionary<string, string>? Descriptions { get; set; } = [];
  public virtual string? DisplayName { get; set; }
  public virtual Dictionary<string, string>? DisplayNames { get; set; } = [];
  [DynamoDBProperty("ScopeName")]
  public virtual string? Name { get; set; }
  public virtual string? Properties { get; set; }
  public virtual List<string>? Resources { get; set; } = [];
}
