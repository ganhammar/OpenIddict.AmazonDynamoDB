using Amazon.DynamoDBv2.DataModel;

namespace OpenIddict.AmazonDynamoDB;

[DynamoDBTable(Constants.DefaultApplicationRedirectsTableName)]
public class OpenIddictDynamoDbApplicationRedirect
{
    [DynamoDBHashKey]
    public string? RedirectUri { get; set; }
    [DynamoDBRangeKey]
    public RedirectType RedirectType { get; set; }
    public string? ApplicationId { get; set; }
}