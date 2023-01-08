using Amazon.DynamoDBv2.DataModel;

namespace OpenIddict.AmazonDynamoDB;

[DynamoDBTable(Constants.DefaultTableName)]
internal class CountModel
{
  public CountModel() { }
  public CountModel(CountType _type, long? count = default) => Type = _type;

  [DynamoDBHashKey]
  public string PartitionKey
  {
    get => $"COUNT#{Type.ToString()}";
    private set { }
  }
  [DynamoDBRangeKey]
  public string? SortKey
  {
    get => $"#COUNT#{Type.ToString()}";
    set { }
  }

  public CountType Type { get; set; }
  public long Count { get; set; }
}
