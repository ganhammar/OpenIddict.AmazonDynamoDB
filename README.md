# OpenIddict.AmazonDynamoDB

![Build Status](https://github.com/ganhammar/OpenIddict.AmazonDynamoDB/actions/workflows/ci.yml/badge.svg) [![codecov](https://codecov.io/gh/ganhammar/OpenIddict.AmazonDynamoDB/branch/main/graph/badge.svg?token=S4M1VCX8J6)](https://codecov.io/gh/ganhammar/OpenIddict.AmazonDynamoDB) [![NuGet](https://img.shields.io/nuget/v/Community.OpenIddict.AmazonDynamoDB)](https://www.nuget.org/packages/Community.OpenIddict.AmazonDynamoDB)

A [DynamoDB](https://aws.amazon.com/dynamodb/) integration for [OpenIddict](https://github.com/openiddict/openiddict-core).

## Getting Started

You can install the latest version via [Nuget](https://www.nuget.org/packages/OpenIddict.AmazonDynamoDB):

```
> dotnet add package Community.OpenIddict.AmazonDynamoDB
```

Then you use the stores by calling `AddDynamoDbStores` on `OpenIddictBuilder`:

```c#
services
    .AddOpenIddict()
    .AddCore()
    .UseDynamoDb()
    .Configure(options =>
    {
        options.BillingMode = BillingMode.PROVISIONED; // Default is BillingMode.PAY_PER_REQUEST
        options.ProvisionedThroughput = new ProvisionedThroughput
        {
            ReadCapacityUnits = 5, // Default is 1
            WriteCapacityUnits = 5, // Default is 1
        };
        options.UsersTableName = "CustomIdentityUserTable"; // Default is identity.users
    });
```

Finally you need to ensure that tables and indexes has been added:

```c#
OpenIddictDynamoDbSetup.EnsureInitialized(serviceProvider);
```

Or asynchronously:

```c#
await OpenIddictDynamoDbSetup.EnsureInitializedAsync(serviceProvider);
```


## Tests

In order to run the tests, you need to have DynamoDB running locally on `localhost:8000`. This can easily be done using [Docker](https://www.docker.com/) and the following command:

```
docker run -p 8000:8000 amazon/dynamodb-local
```