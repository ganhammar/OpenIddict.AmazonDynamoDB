using Amazon.DynamoDBv2.Model;

namespace OpenIddict.AmazonDynamoDB;

/// <summary>An item read as its attributes, for partitions that hold several kinds of items.</summary>
internal sealed class RawItem : IDynamoDbItem
{
  public string PartitionKey => Attributes.GetString("PartitionKey") ?? "";

  public string? SortKey => Attributes.GetString("SortKey");

  public Dictionary<string, AttributeValue> Attributes { get; private set; } = new();

  void IDynamoDbItem.Write(Dictionary<string, AttributeValue> item)
  {
    foreach (var (name, value) in Attributes)
    {
      item[name] = value;
    }
  }

  void IDynamoDbItem.Read(Dictionary<string, AttributeValue> item) => Attributes = item;
}
