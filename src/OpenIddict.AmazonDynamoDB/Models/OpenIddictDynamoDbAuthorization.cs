using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;

namespace OpenIddict.AmazonDynamoDB;

[DynamoDBTable(Constants.DefaultTableName)]
public class OpenIddictDynamoDbAuthorization : IDynamoDbItem
{
  [DynamoDBHashKey]
  public string PartitionKey
  {
    get => $"AUTHORIZATION#{Id}";
    set { }
  }
  [DynamoDBRangeKey]
  public string? SortKey
  {
    get => $"#AUTHORIZATION#{Id}";
    set { }
  }
  public virtual string Id { get; set; }
    = Guid.NewGuid().ToString();
  public virtual string? ApplicationId { get; set; }
  public virtual string? ConcurrencyToken { get; set; }
    = Guid.NewGuid().ToString();
  public virtual DateTime? CreationDate { get; set; }
  public virtual string? Properties { get; set; }
  public virtual List<string>? Scopes { get; set; } = [];
  public virtual string? Status { get; set; }
  public virtual string? Subject { get; set; }
  public virtual string? Type { get; set; }
  public string? SearchKey
  {
    get => $"APPLICATION#{ApplicationId}#STATUS#{Status}#TYPE#{Type}";
    set { }
  }
  [DynamoDBProperty("ttl", StoreAsEpochLong = true)]
  public DateTime? TTL { get; set; }

  /// <summary>
  /// Writes the authorization's attributes to the item the store saves. A derived type with
  /// properties of its own overrides this, calls the base implementation and adds them, using the
  /// <see cref="AttributeValues"/> extension methods.
  /// </summary>
  protected virtual void WriteAttributes(Dictionary<string, AttributeValue> item)
  {
    ArgumentNullException.ThrowIfNull(item);

    item.Set("PartitionKey", PartitionKey);
    item.Set("SortKey", SortKey);
    item.Set("Id", Id);
    item.Set("ApplicationId", ApplicationId);
    item.Set("ConcurrencyToken", ConcurrencyToken);
    item.Set("CreationDate", CreationDate);
    item.Set("Properties", Properties);
    item.SetStringSet("Scopes", Scopes);
    item.Set("Status", Status);
    item.Set("Subject", Subject);
    item.Set("Type", Type);
    item.Set("SearchKey", SearchKey);
    item.SetEpochSeconds("ttl", TTL);
  }

  /// <summary>
  /// Reads the authorization's attributes from the item the store loaded. A derived type with
  /// properties of its own overrides this, calls the base implementation and reads them, using the
  /// <see cref="AttributeValues"/> extension methods.
  /// </summary>
  protected virtual void ReadAttributes(Dictionary<string, AttributeValue> item)
  {
    ArgumentNullException.ThrowIfNull(item);

    Id = item.GetString("Id") ?? Id;
    ApplicationId = item.GetString("ApplicationId");
    ConcurrencyToken = item.GetString("ConcurrencyToken") ?? ConcurrencyToken;
    CreationDate = item.GetDateTime("CreationDate");
    Properties = item.GetString("Properties");
    Scopes = item.GetStringList("Scopes") ?? Scopes;
    Status = item.GetString("Status");
    Subject = item.GetString("Subject");
    Type = item.GetString("Type");
    TTL = item.GetEpochSeconds("ttl");
  }

  void IDynamoDbItem.Write(Dictionary<string, AttributeValue> item) => WriteAttributes(item);

  void IDynamoDbItem.Read(Dictionary<string, AttributeValue> item) => ReadAttributes(item);
}
