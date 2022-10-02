using Amazon.DynamoDBv2.DataModel;

namespace OpenIddict.AmazonDynamoDB;

[DynamoDBTable(Constants.DefaultTokenTableName)]
public class OpenIddictDynamoDbToken
{
    public virtual string? ApplicationId { get; set; }
    public virtual string? AuthorizationId { get; set; }
    public virtual string? ConcurrencyToken { get; set; } = Guid.NewGuid().ToString();
    public virtual DateTime? CreationDate { get; set; }
    public virtual DateTime? ExpirationDate { get; set; }
    [DynamoDBHashKey]
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
        get => $"{ApplicationId}#{Status}#{Type}";
        set { }
    }
}