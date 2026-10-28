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

## Table Layout

Everything lives in one table with a `PartitionKey` and a `SortKey` and two global secondary indexes. Applications, authorizations, scopes and tokens each have a partition of their own, and an application's redirect URIs and a scope's lookups are items beside them. Two indexes serve the range queries: `ApplicationId-index` lists everything that belongs to an application, and `Subject-index` lists a subject's authorizations and tokens by application, status and type. Everything else is found through lookup items, which are one strongly consistent read each: `CLIENTID#{clientId}` and `REFERENCEID#{referenceId}` point at the application or token, the `SCOPELOOKUP#` items point at scopes by name and resource, the partition `REDIRECT#{type}#{uri}` lists the applications registered for a redirect URI, and a token is listed under its authorization's partition. An application and its client id lookup, a scope and its name lookup, and a token and its reference id lookup are written in one transaction, so a client id, scope name or reference id that another entity holds fails the write with an `OpenIddictExceptions.ValidationException`. Token lookups carry the token's time to live and expire with it.

## Upgrading from 3.x

Version 3 found entities through eight global secondary indexes. Version 4 reads and writes the same items in the same format, so no data changes, but it needs the lookup items described above, which an existing table does not have. Upgrade like this:

1. With version 4 installed, run `OpenIddictDynamoDbSetup.MigrateAsync(serviceProvider)` (or `DynamoDbTableSetup.MigrateAsync(options, client)`) against the table. It scans the table once and writes the lookup items for every application, redirect URI, scope and token. It is safe to run while version 3 instances are serving traffic, and safe to run again. The result says how many items it wrote lookups for.
2. Deploy version 4. `EnsureInitialized` logs a warning as long as the legacy indexes exist, and nothing else; it never scans an existing table or removes anything from it.
3. Run `MigrateAsync` once more, to cover anything a version 3 instance wrote during the rollout.
4. Run `OpenIddictDynamoDbSetup.RemoveLegacyIndexesAsync(serviceProvider)` to drop the six legacy indexes (and the `Resource-index` of very old tables), one at a time as DynamoDB requires. Until then the indexes are kept up to date by DynamoDB and cost storage and write capacity, but nothing reads them. This needs `dynamodb:UpdateTable`; without it a warning is logged and the indexes stay.

What changes for an application:

- Lookups by client id, reference id, scope name and redirect URI are strongly consistent reads instead of eventually consistent index queries, so an entity is found right after it was created. A lookup is two reads, the lookup item and the entity.
- Creating or updating an application, scope or token is a transaction of the entity and its lookups, which DynamoDB bills at twice the write units of a plain write. Duplicate client ids, scope names and reference ids fail with a `ValidationException` instead of relying on the managers' checks.
- Custom properties on a subclass of an entity are stored only when the subclass overrides `WriteAttributes` and `ReadAttributes`, see Native AOT below.
- `DynamoDbTableSetup.EnsureAliasCreated` and the `UpdateScopeIndexes` migration are gone: the stores no longer use the SDK's object persistence model, and the old `Resource-index` is removed by `RemoveLegacyIndexesAsync`.

## Native AOT

The package is trim and Native AOT compatible: the stores read and write items through the low-level DynamoDB client, with no reflection. To store properties of your own on an entity, extend `OpenIddictDynamoDbApplication`, `OpenIddictDynamoDbAuthorization`, `OpenIddictDynamoDbScope` or `OpenIddictDynamoDbToken`, override `WriteAttributes` and `ReadAttributes`, and register the type as the default entity on the core builder. The `AttributeValues` extension methods convert strings, booleans, numbers, dates, lists and maps the way the stores do.

```c#
public class CustomApplication : OpenIddictDynamoDbApplication
{
    public string? Owner { get; set; }

    protected override void WriteAttributes(Dictionary<string, AttributeValue> item)
    {
        base.WriteAttributes(item);
        item.Set("Owner", Owner);
    }

    protected override void ReadAttributes(Dictionary<string, AttributeValue> item)
    {
        base.ReadAttributes(item);
        Owner = item.GetString("Owner");
    }
}
```

## Concurrency

Updates to applications, authorizations, scopes and tokens only succeed if the item hasn't been changed since it was loaded, otherwise an `OpenIddictExceptions.ConcurrencyException` is thrown, as with the Entity Framework Core stores. This is what makes sure that authorization codes and refresh tokens can only be redeemed once when several requests redeem them at the same time.

## Lifetime

Tokens and authorizations are removed by DynamoDB's time to live. Tokens are kept until they expire, and until five minutes after they have been redeemed or revoked. Ad-hoc authorizations are kept until the last of their tokens expires, and permanent authorizations until they are revoked. DynamoDB usually removes expired items within a few days.


## Tests

In order to run the tests, you need to have DynamoDB running locally on `localhost:8000`. This can easily be done using [Docker](https://www.docker.com/) and the following command:

```
docker run -p 8000:8000 amazon/dynamodb-local
```
