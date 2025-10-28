using System.ComponentModel;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.DependencyInjection;

namespace OpenIddict.AmazonDynamoDB;
public class OpenIddictDynamoDbBuilder(IServiceCollection services)
{
  [EditorBrowsable(EditorBrowsableState.Never)]
  public IServiceCollection Services { get; } = services ?? throw new ArgumentNullException(nameof(services));

  public OpenIddictDynamoDbBuilder Configure(Action<OpenIddictDynamoDbOptions> configuration)
  {
    ArgumentNullException.ThrowIfNull(configuration);

    Services.Configure(configuration);

    return this;
  }

  public OpenIddictDynamoDbBuilder SetDefaultTableName(string name)
  {
    ArgumentNullException.ThrowIfNull(name);

    return Configure(options => options.DefaultTableName = name);
  }

  public OpenIddictDynamoDbBuilder UseDatabase(IAmazonDynamoDB database)
  {
    ArgumentNullException.ThrowIfNull(database);

    return Configure(options => options.Database = database);
  }

  public OpenIddictDynamoDbBuilder SetBillingMode(BillingMode billingMode)
  {
    ArgumentNullException.ThrowIfNull(billingMode);

    return Configure(options => options.BillingMode = billingMode);
  }

  public OpenIddictDynamoDbBuilder SetProvisionedThroughput(ProvisionedThroughput provisionedThroughput)
  {
    ArgumentNullException.ThrowIfNull(provisionedThroughput);

    return Configure(options => options.ProvisionedThroughput = provisionedThroughput);
  }
}
