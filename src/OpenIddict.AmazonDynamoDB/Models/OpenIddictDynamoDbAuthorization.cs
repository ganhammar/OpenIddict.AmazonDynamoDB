using Amazon.DynamoDBv2.DataModel;

namespace OpenIddict.AmazonDynamoDB;

[DynamoDBTable(Constants.DefaultAuthorizationTableName)]
public class OpenIddictDynamoDbAuthorization
{
    [DynamoDBHashKey]
    public virtual string Id { get; set; } = Guid.NewGuid().ToString();
    public virtual string? ApplicationId { get; set; }
    public virtual string? ConcurrencyToken { get; set; } = Guid.NewGuid().ToString();
    public virtual DateTime? CreationDate { get; set; }
    public virtual string? Properties { get; set; }
    public virtual List<string>? Scopes { get; set; } = new List<string>();
    public virtual string? Status { get; set; }
    public virtual string? Subject { get; set; }
    public virtual string? Type { get; set; }
    public string? SearchKey
    {
        get => $"{ApplicationId}#{Status}#{Type}";
        set { }
    }
}