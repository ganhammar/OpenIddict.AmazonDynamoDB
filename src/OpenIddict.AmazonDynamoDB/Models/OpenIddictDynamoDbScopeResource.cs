using Amazon.DynamoDBv2.DataModel;

namespace OpenIddict.AmazonDynamoDB;

[DynamoDBTable(Constants.DefaultScopeResourceTableName)]
public class OpenIddictDynamoDbScopeResource
{
    [DynamoDBHashKey]
    public string? ScopeId { get; set; }
    [DynamoDBRangeKey]
    public string? ScopeResource { get; set; }
}