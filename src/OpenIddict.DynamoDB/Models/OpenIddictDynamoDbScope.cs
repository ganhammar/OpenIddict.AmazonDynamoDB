using Amazon.DynamoDBv2.DataModel;

namespace OpenIddict.DynamoDB;

[DynamoDBTable(Constants.DefaultScopeTableName)]
public class OpenIddictDynamoDbScope
{
    [DynamoDBHashKey]
    public virtual string Id { get; set; } = Guid.NewGuid().ToString();
    public virtual string? ConcurrencyToken { get; set; } = Guid.NewGuid().ToString();
    public virtual string? Description { get; set; }
    public virtual Dictionary<string, string>? Descriptions { get; set; }
        = new Dictionary<string, string>();
    public virtual string? DisplayName { get; set; }
    public virtual Dictionary<string, string>? DisplayNames { get; set; }
        = new Dictionary<string, string>();
    [DynamoDBProperty("ScopeName")]
    public virtual string? Name { get; set; }
    public virtual string? Properties { get; set; }
    [DynamoDBIgnore]
    public virtual List<string>? Resources { get; set; } = new List<string>();
}