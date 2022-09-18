﻿using System.ComponentModel;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Core;

namespace OpenIddict.DynamoDB;
public class OpenIddictDynamoDbBuilder
{
    public OpenIddictDynamoDbBuilder(IServiceCollection services)
        => Services = services ?? throw new ArgumentNullException(nameof(services));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public IServiceCollection Services { get; }

    public OpenIddictDynamoDbBuilder Configure(Action<OpenIddictDynamoDbOptions> configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

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
        ArgumentNullException.ThrowIfNull(name);

        return Configure(options => options.ApplicationsTableName = name);
    }

    public OpenIddictDynamoDbBuilder SetApplicationRedirectsTableName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return Configure(options => options.ApplicationRedirectsTableName = name);
    }

    public OpenIddictDynamoDbBuilder SetAuthorizationsTableName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return Configure(options => options.AuthorizationsTableName = name);
    }

    public OpenIddictDynamoDbBuilder SetScopesTableName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return Configure(options => options.ScopesTableName = name);
    }

    public OpenIddictDynamoDbBuilder SetScopeResourcesTableName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return Configure(options => options.ScopeResourcesTableName = name);
    }

    public OpenIddictDynamoDbBuilder SetTokensTableName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return Configure(options => options.TokensTableName = name);
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
