using System.ComponentModel;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Core;

namespace OpenIddict.DynamoDB;
public class OpenIddictDynamoDbBuilder
{
    private static string ExceptionMessage = "The table name cannot be null or empty";

    public OpenIddictDynamoDbBuilder(IServiceCollection services)
        => Services = services ?? throw new ArgumentNullException(nameof(services));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public IServiceCollection Services { get; }

    public OpenIddictDynamoDbBuilder Configure(Action<OpenIddictDynamoDbOptions> configuration)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        Services.Configure(configuration);

        return this;
    }

    public OpenIddictDynamoDbBuilder ReplaceDefaultApplicationEntity<TApplication>()
        where TApplication : OpenIddictDynamoDbApplication
    {
        Services.Configure<OpenIddictCoreOptions>(options => options.DefaultApplicationType = typeof(TApplication));

        return this;
    }

    public OpenIddictDynamoDbBuilder ReplaceDefaultAuthorizationEntity<TAuthorization>()
        where TAuthorization : OpenIddictDynamoDbAuthorization
    {
        Services.Configure<OpenIddictCoreOptions>(options => options.DefaultAuthorizationType = typeof(TAuthorization));

        return this;
    }

    public OpenIddictDynamoDbBuilder ReplaceDefaultScopeEntity<TScope>()
        where TScope : OpenIddictDynamoDbScope
    {
        Services.Configure<OpenIddictCoreOptions>(options => options.DefaultScopeType = typeof(TScope));

        return this;
    }

    public OpenIddictDynamoDbBuilder ReplaceDefaultTokenEntity<TToken>()
        where TToken : OpenIddictDynamoDbToken
    {
        Services.Configure<OpenIddictCoreOptions>(options => options.DefaultTokenType = typeof(TToken));

        return this;
    }

    public OpenIddictDynamoDbBuilder SetApplicationsTableName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException(ExceptionMessage, nameof(name));
        }

        return Configure(options => options.ApplicationsTableName = name);
    }

    public OpenIddictDynamoDbBuilder SetApplicationRedirectsTableName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException(ExceptionMessage, nameof(name));
        }

        return Configure(options => options.ApplicationRedirectsTableName = name);
    }

    public OpenIddictDynamoDbBuilder SetAuthorizationsTableName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException(ExceptionMessage, nameof(name));
        }

        return Configure(options => options.AuthorizationsTableName = name);
    }

    public OpenIddictDynamoDbBuilder SetScopesTableName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException(ExceptionMessage, nameof(name));
        }

        return Configure(options => options.ScopesTableName = name);
    }

    public OpenIddictDynamoDbBuilder SetTokensTableName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException(ExceptionMessage, nameof(name));
        }

        return Configure(options => options.TokensTableName = name);
    }

    public OpenIddictDynamoDbBuilder UseDatabase(IAmazonDynamoDB database)
    {
        if (database is null)
        {
            throw new ArgumentNullException(nameof(database));
        }

        return Configure(options => options.Database = database);
    }

    public OpenIddictDynamoDbBuilder SetBillingMode(BillingMode billingMode)
    {
        if (billingMode is null)
        {
            throw new ArgumentNullException(nameof(billingMode));
        }

        return Configure(options => options.BillingMode = billingMode);
    }

    public OpenIddictDynamoDbBuilder SetProvisionedThroughput(ProvisionedThroughput provisionedThroughput)
    {
        if (provisionedThroughput is null)
        {
            throw new ArgumentNullException(nameof(provisionedThroughput));
        }

        return Configure(options => options.ProvisionedThroughput = provisionedThroughput);
    }
}
