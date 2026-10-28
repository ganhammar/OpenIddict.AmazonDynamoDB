using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;

namespace OpenIddict.AmazonDynamoDB;

[DynamoDBTable(Constants.DefaultTableName)]
public class OpenIddictDynamoDbScope : IDynamoDbItem
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

  /// <summary>The name as the store last read it, whose lookup is removed when it changes.</summary>
  internal string? StoredName { get; set; }

  /// <summary>The resources as the store last read them, whose lookups are removed when they change.</summary>
  internal List<string>? StoredResources { get; set; }

  /// <summary>
  /// Writes the scope's attributes to the item the store saves. A derived type with properties of
  /// its own overrides this, calls the base implementation and adds them, using the
  /// <see cref="AttributeValues"/> extension methods.
  /// </summary>
  protected virtual void WriteAttributes(Dictionary<string, AttributeValue> item)
  {
    ArgumentNullException.ThrowIfNull(item);

    item.Set("PartitionKey", PartitionKey);
    item.Set("SortKey", SortKey);
    item.Set("Id", Id);
    item.Set("ConcurrencyToken", ConcurrencyToken);
    item.Set("Description", Description);
    item.SetStringMap("Descriptions", Descriptions);
    item.Set("DisplayName", DisplayName);
    item.SetStringMap("DisplayNames", DisplayNames);
    item.Set("ScopeName", Name);
    item.Set("Properties", Properties);
    item.SetStringSet("Resources", Resources);
  }

  /// <summary>
  /// Reads the scope's attributes from the item the store loaded. A derived type with properties of
  /// its own overrides this, calls the base implementation and reads them, using the
  /// <see cref="AttributeValues"/> extension methods.
  /// </summary>
  protected virtual void ReadAttributes(Dictionary<string, AttributeValue> item)
  {
    ArgumentNullException.ThrowIfNull(item);

    Id = item.GetString("Id") ?? Id;
    ConcurrencyToken = item.GetString("ConcurrencyToken") ?? ConcurrencyToken;
    Description = item.GetString("Description");
    Descriptions = item.GetStringMap("Descriptions") ?? Descriptions;
    DisplayName = item.GetString("DisplayName");
    DisplayNames = item.GetStringMap("DisplayNames") ?? DisplayNames;
    Name = item.GetString("ScopeName");
    Properties = item.GetString("Properties");
    Resources = item.GetStringList("Resources") ?? Resources;
    StoredName = Name;
    StoredResources = Resources?.ToList();
  }

  void IDynamoDbItem.Write(Dictionary<string, AttributeValue> item) => WriteAttributes(item);

  void IDynamoDbItem.Read(Dictionary<string, AttributeValue> item) => ReadAttributes(item);
}
