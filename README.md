# OpenIddict.AmazonDynamoDB

![Build Status](https://github.com/ganhammar/OpenIddict.AmazonDynamoDB/actions/workflows/ci-cd.yml/badge.svg) [![codecov](https://codecov.io/gh/ganhammar/OpenIddict.AmazonDynamoDB/branch/main/graph/badge.svg?token=S4M1VCX8J6)](https://codecov.io/gh/ganhammar/OpenIddict.AmazonDynamoDB) [![NuGet](https://img.shields.io/nuget/v/Community.OpenIddict.AmazonDynamoDB)](https://www.nuget.org/packages/Community.OpenIddict.AmazonDynamoDB)

A [DynamoDB](https://aws.amazon.com/dynamodb/) integration for [OpenIddict](https://github.com/openiddict/openiddict-core), targeting .NET 8, 9 and 10.

## Getting Started

You can install the latest version via [Nuget](https://www.nuget.org/packages/OpenIddict.AmazonDynamoDB):

```
> dotnet add package Community.OpenIddict.AmazonDynamoDB
```

Then you use the stores by calling `UseDynamoDb` on `OpenIddictCoreBuilder`:

```c#
services
    .AddOpenIddict()
    .AddCore(options => {
        options.UseDynamoDb()
            .Configure(options =>
            {
                options.BillingMode = BillingMode.PROVISIONED; // Default is BillingMode.PAY_PER_REQUEST
                options.ProvisionedThroughput = new ProvisionedThroughput
                {
                    ReadCapacityUnits = 5, // Default is 1
                    WriteCapacityUnits = 5, // Default is 1
                };
                options.DefaultTableName = "CustomOpenIddictTable"; // Default is openiddict
            });
    }); 
```

Finally, you need to ensure that tables and indexes have been added:

```c#
OpenIddictDynamoDbSetup.EnsureInitialized(serviceProvider);
```

Or asynchronously:

```c#
await OpenIddictDynamoDbSetup.EnsureInitializedAsync(serviceProvider);
```

The table is created if it doesn't exist, and time to live is enabled on the `ttl` attribute so that expired and revoked tokens and authorizations are removed. If the table already exists, any global secondary index that is missing from it is added (and the `Resource-index` from tables created before scopes were stored with lookups is replaced by `ScopeId-index`), and the call waits until the indexes are active. New indexes use provisioned throughput from the options when the existing table is provisioned, and on-demand capacity otherwise. It's safe to call from several instances at the same time.

The call needs the `dynamodb:DescribeTable` and `dynamodb:DescribeTimeToLive` permissions on the table, plus `dynamodb:CreateTable` to create it, `dynamodb:UpdateTable` to change its indexes and `dynamodb:UpdateTimeToLive` to enable time to live. Without `dynamodb:DescribeTable` it falls back to `dynamodb:ListTables` and only checks that the table exists. If an index or time to live can't be changed because a permission is missing, a warning is logged instead of failing.

The `IServiceProvider` overloads log progress and warnings through the registered `ILoggerFactory`.

## Concurrency

Updates to applications, authorizations, scopes and tokens only succeed if the item hasn't been changed since it was loaded, otherwise an `OpenIddictExceptions.ConcurrencyException` is thrown, as with the Entity Framework Core stores. This is what makes sure that authorization codes and refresh tokens can only be redeemed once when several requests redeem them at the same time.


## Tests

In order to run the tests, you need to have DynamoDB running locally on `localhost:8000`. This can easily be done using [Docker](https://www.docker.com/) and the following command:

```
docker run -p 8000:8000 amazon/dynamodb-local
```
