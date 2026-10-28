using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;

namespace OpenIddict.AmazonDynamoDB;

[DynamoDBTable(Constants.DefaultTableName)]
internal class CountModel : IDynamoDbItem
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
    set { }
  }
  [DynamoDBRangeKey]
  public string? SortKey
  {
    get => $"#COUNT#{Type}";
    set { }
  }

  public CountType Type { get; set; }
  public long Count { get; set; }

  void IDynamoDbItem.Write(Dictionary<string, AttributeValue> item)
  {
    item.Set("PartitionKey", PartitionKey);
    item.Set("SortKey", SortKey);
    item.Set("Type", (long)Type);
    item.Set("Count", Count);
  }

  void IDynamoDbItem.Read(Dictionary<string, AttributeValue> item)
  {
    Type = (CountType)item.GetInt32("Type");
    Count = item.GetInt64("Count");
  }
}
