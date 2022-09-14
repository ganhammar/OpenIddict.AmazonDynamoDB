using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace OpenIddict.DynamoDB;

public class OpenIddictDynamoDbOptions
{
    public string ApplicationsTableName { get; set; } = Constants.DefaultApplicationTableName;
    public string ApplicationRedirectTableName { get; set; } = Constants.DefaultApplicationRedirectTableName;
    public string AuthorizationsTableName { get; set; } = Constants.DefaultAuthorizationTableName;
    public string ScopesTableName { get; set; } = Constants.DefaultScopeTableName;
    public string TokensTableName { get; set; } = Constants.DefaultTokenTableName;
    public IAmazonDynamoDB? Database { get; set; }
    public ProvisionedThroughput ProvisionedThroughput { get; set; } = new ProvisionedThroughput
    {
        ReadCapacityUnits = 1,
        WriteCapacityUnits = 1,
    };
    public BillingMode BillingMode { get; set; } = BillingMode.PAY_PER_REQUEST;
}