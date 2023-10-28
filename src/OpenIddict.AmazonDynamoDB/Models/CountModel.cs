using Amazon.DynamoDBv2.DataModel;

namespace OpenIddict.AmazonDynamoDB;

[DynamoDBTable(Constants.DefaultTableName)]
internal class CountModel
{
  public CountModel() { }
  public CountModel(CountType type, long? count = default)
  {
    Type = type;
    Count = count ?? 0;
  }

  [DynamoDBHashKey]
  public string PartitionKey
  {
    get => $"COUNT#{Type}";
    private set { }
  }
  [DynamoDBRangeKey]
  public string? SortKey
  {
    get => $"#COUNT#{Type}";
    set { }
  }

  public CountType Type { get; set; }
  public long Count { get; set; }
}
