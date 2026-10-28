using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;

namespace OpenIddict.AmazonDynamoDB;

[DynamoDBTable(Constants.DefaultTableName)]
public class OpenIddictDynamoDbToken : IDynamoDbItem
{
  [DynamoDBHashKey]
  public string PartitionKey
  {
    get => $"TOKEN#{Id}";
    set { }
  }
  [DynamoDBRangeKey]
  public string? SortKey
  {
    get => $"#TOKEN#{Id}";
    set { }
  }
  public virtual string? ApplicationId { get; set; }
  public virtual string? AuthorizationId { get; set; }
  public virtual string? ConcurrencyToken { get; set; }
    = Guid.NewGuid().ToString();
  public virtual DateTime? CreationDate { get; set; }
  public virtual DateTime? ExpirationDate { get; set; }
  public virtual string Id { get; set; } = Guid.NewGuid().ToString();
  public virtual string? Payload { get; set; }
  public virtual string? Properties { get; set; }
  public virtual DateTime? RedemptionDate { get; set; }
  public virtual string? ReferenceId { get; set; }
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

  /// <summary>The reference id as the store last read it, whose lookup is removed when it changes.</summary>
  internal string? StoredReferenceId { get; set; }

  /// <summary>The authorization id as the store last read it, whose token listing is removed when it changes.</summary>
  internal string? StoredAuthorizationId { get; set; }

  /// <summary>
  /// Writes the token's attributes to the item the store saves. A derived type with properties of
  /// its own overrides this, calls the base implementation and adds them, using the
  /// <see cref="AttributeValues"/> extension methods.
  /// </summary>
  protected virtual void WriteAttributes(Dictionary<string, AttributeValue> item)
  {
    ArgumentNullException.ThrowIfNull(item);

    item.Set("PartitionKey", PartitionKey);
    item.Set("SortKey", SortKey);
    item.Set("ApplicationId", ApplicationId);
    item.Set("AuthorizationId", AuthorizationId);
    item.Set("ConcurrencyToken", ConcurrencyToken);
    item.Set("CreationDate", CreationDate);
    item.Set("ExpirationDate", ExpirationDate);
    item.Set("Id", Id);
    item.Set("Payload", Payload);
    item.Set("Properties", Properties);
    item.Set("RedemptionDate", RedemptionDate);
    item.Set("ReferenceId", ReferenceId);
    item.Set("Status", Status);
    item.Set("Subject", Subject);
    item.Set("Type", Type);
    item.Set("SearchKey", SearchKey);
    item.SetEpochSeconds("ttl", TTL);
  }

  /// <summary>
  /// Reads the token's attributes from the item the store loaded. A derived type with properties of
  /// its own overrides this, calls the base implementation and reads them, using the
  /// <see cref="AttributeValues"/> extension methods.
  /// </summary>
  protected virtual void ReadAttributes(Dictionary<string, AttributeValue> item)
  {
    ArgumentNullException.ThrowIfNull(item);

    ApplicationId = item.GetString("ApplicationId");
    AuthorizationId = item.GetString("AuthorizationId");
    ConcurrencyToken = item.GetString("ConcurrencyToken") ?? ConcurrencyToken;
    CreationDate = item.GetDateTime("CreationDate");
    ExpirationDate = item.GetDateTime("ExpirationDate");
    Id = item.GetString("Id") ?? Id;
    Payload = item.GetString("Payload");
    Properties = item.GetString("Properties");
    RedemptionDate = item.GetDateTime("RedemptionDate");
    ReferenceId = item.GetString("ReferenceId");
    Status = item.GetString("Status");
    Subject = item.GetString("Subject");
    Type = item.GetString("Type");
    TTL = item.GetEpochSeconds("ttl");
    StoredReferenceId = ReferenceId;
    StoredAuthorizationId = AuthorizationId;
  }

  void IDynamoDbItem.Write(Dictionary<string, AttributeValue> item) => WriteAttributes(item);

  void IDynamoDbItem.Read(Dictionary<string, AttributeValue> item) => ReadAttributes(item);
}
