using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace OpenIddict.AmazonDynamoDB;

public class OpenIddictDynamoDbOptions
{
  public string DefaultTableName { get; set; } = Constants.DefaultTableName;
  public IAmazonDynamoDB? Database { get; set; }
  public ProvisionedThroughput ProvisionedThroughput { get; set; } = new ProvisionedThroughput
  {
    ReadCapacityUnits = 1,
    WriteCapacityUnits = 1,
  };
  public BillingMode BillingMode { get; set; } = BillingMode.PAY_PER_REQUEST;
}
