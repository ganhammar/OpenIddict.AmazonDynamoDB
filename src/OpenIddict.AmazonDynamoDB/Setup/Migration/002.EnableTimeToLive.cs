using Amazon.DynamoDBv2;

namespace OpenIddict.AmazonDynamoDB.Migration;

public static class EnableTimeToLive
{
  public static async Task Migrate(
    OpenIddictDynamoDbOptions options,
    IAmazonDynamoDB database,
    CancellationToken cancellationToken)
  {
    var ttlSettings = await database.DescribeTimeToLiveAsync(options.DefaultTableName, cancellationToken);

    if (new[] { TimeToLiveStatus.ENABLED, TimeToLiveStatus.ENABLING }.Contains(ttlSettings.TimeToLiveDescription.TimeToLiveStatus) == false)
    {
      return;
    }

    await database.UpdateTimeToLiveAsync(new()
    {
      TableName = options.DefaultTableName,
      TimeToLiveSpecification = new()
      {
        Enabled = true,
        AttributeName = "ttl",
      },
    }, cancellationToken);
  }
}
