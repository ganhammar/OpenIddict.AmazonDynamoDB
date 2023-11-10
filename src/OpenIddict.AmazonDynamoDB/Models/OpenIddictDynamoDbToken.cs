using Amazon.DynamoDBv2.DataModel;

namespace OpenIddict.AmazonDynamoDB;

[DynamoDBTable(Constants.DefaultTableName)]
public class OpenIddictDynamoDbToken
{
  [DynamoDBHashKey]
  public string PartitionKey
  {
    get => $"TOKEN#{Id}";
    private set { }
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
  [DynamoDBProperty("ttl", storeAsEpoch: true)]
  public DateTime? TTL { get; set; }
}
