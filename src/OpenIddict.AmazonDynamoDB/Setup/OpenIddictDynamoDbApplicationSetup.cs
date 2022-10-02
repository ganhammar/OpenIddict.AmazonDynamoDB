using System.Net;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Util;

namespace OpenIddict.AmazonDynamoDB;

public static class OpenIddictDynamoDbApplicationSetup
{
    public static Task EnsureInitializedAsync(
        OpenIddictDynamoDbOptions openIddictDynamoDbOptions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(openIddictDynamoDbOptions);
        ArgumentNullException.ThrowIfNull(openIddictDynamoDbOptions.Database);

        if (openIddictDynamoDbOptions.ApplicationsTableName != Constants.DefaultApplicationTableName)
        {
            AWSConfigsDynamoDB.Context.AddAlias(new TableAlias(
                openIddictDynamoDbOptions.ApplicationsTableName,
                Constants.DefaultApplicationTableName));
        }

        if (openIddictDynamoDbOptions.ApplicationRedirectsTableName != Constants.DefaultApplicationRedirectsTableName)
        {
            AWSConfigsDynamoDB.Context.AddAlias(new TableAlias(
                openIddictDynamoDbOptions.ApplicationRedirectsTableName,
                Constants.DefaultApplicationRedirectsTableName));
        }

        return SetupTables(openIddictDynamoDbOptions, cancellationToken);
    }

    private static async Task SetupTables(
        OpenIddictDynamoDbOptions openIddictDynamoDbOptions,
        CancellationToken cancellationToken)
    {
        var applicationGlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
        {
            new GlobalSecondaryIndex
            {
                IndexName = "ClientId-index",
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement("ClientId", KeyType.HASH),
                },
                ProvisionedThroughput = openIddictDynamoDbOptions.ProvisionedThroughput,
                Projection = new Projection
                {
                    ProjectionType = ProjectionType.ALL,
                },
            },
        };
        var applicationRedirectGlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
        {
            new GlobalSecondaryIndex
            {
                IndexName = "ApplicationId-index",
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement("ApplicationId", KeyType.HASH),
                },
                ProvisionedThroughput = openIddictDynamoDbOptions.ProvisionedThroughput,
                Projection = new Projection
                {
                    ProjectionType = ProjectionType.ALL,
                },
            },
        };

        var tableNames = await openIddictDynamoDbOptions.Database!.ListTablesAsync(cancellationToken);

        if (!tableNames.TableNames.Contains(openIddictDynamoDbOptions.ApplicationsTableName))
        {
            await CreateApplicationTableAsync(
                openIddictDynamoDbOptions, applicationGlobalSecondaryIndexes, cancellationToken);
        }
        else
        {
            await DynamoDbUtils.UpdateSecondaryIndexes(
                openIddictDynamoDbOptions.Database!,
                openIddictDynamoDbOptions.ApplicationsTableName,
                applicationGlobalSecondaryIndexes,
                cancellationToken);
        }

        if (!tableNames.TableNames.Contains(openIddictDynamoDbOptions.ApplicationRedirectsTableName))
        {
            await CreateApplicationRedirectTableAsync(
                openIddictDynamoDbOptions,
                applicationRedirectGlobalSecondaryIndexes,
                cancellationToken);
        }
        else
        {
            await DynamoDbUtils.UpdateSecondaryIndexes(
                openIddictDynamoDbOptions.Database!,
                openIddictDynamoDbOptions.ApplicationRedirectsTableName,
                applicationRedirectGlobalSecondaryIndexes,
                cancellationToken);
        }
    }

    private static async Task CreateApplicationTableAsync(
        OpenIddictDynamoDbOptions openIddictDynamoDbOptions,
        List<GlobalSecondaryIndex>? globalSecondaryIndexes,
        CancellationToken cancellationToken)
    {
        var response = await openIddictDynamoDbOptions.Database!.CreateTableAsync(new CreateTableRequest
        {
            TableName = openIddictDynamoDbOptions.ApplicationsTableName,
            ProvisionedThroughput = openIddictDynamoDbOptions.ProvisionedThroughput,
            BillingMode = openIddictDynamoDbOptions.BillingMode,
            KeySchema = new List<KeySchemaElement>
            {
                new KeySchemaElement
                {
                    AttributeName = "Id",
                    KeyType = KeyType.HASH,
                },
            },
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new AttributeDefinition
                {
                    AttributeName = "Id",
                    AttributeType = ScalarAttributeType.S,
                },
                new AttributeDefinition
                {
                    AttributeName = "ClientId",
                    AttributeType = ScalarAttributeType.S,
                },
            },
            GlobalSecondaryIndexes = globalSecondaryIndexes,
        }, cancellationToken);

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            throw new Exception($"Couldn't create table {openIddictDynamoDbOptions.ApplicationsTableName}");
        }

        await DynamoDbUtils.WaitForActiveTableAsync(
            openIddictDynamoDbOptions.Database!,
            openIddictDynamoDbOptions.ApplicationsTableName,
            cancellationToken);
    }

    private static async Task CreateApplicationRedirectTableAsync(
        OpenIddictDynamoDbOptions openIddictDynamoDbOptions,
        List<GlobalSecondaryIndex>? globalSecondaryIndexes,
        CancellationToken cancellationToken)
    {
        var response = await openIddictDynamoDbOptions.Database!.CreateTableAsync(new CreateTableRequest
        {
            TableName = openIddictDynamoDbOptions.ApplicationRedirectsTableName,
            ProvisionedThroughput = openIddictDynamoDbOptions.ProvisionedThroughput,
            BillingMode = openIddictDynamoDbOptions.BillingMode,
            KeySchema = new List<KeySchemaElement>
            {
                new KeySchemaElement
                {
                    AttributeName = "RedirectUri",
                    KeyType = KeyType.HASH,
                },
                new KeySchemaElement
                {
                    AttributeName = "RedirectType",
                    KeyType = KeyType.RANGE,
                },
            },
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new AttributeDefinition
                {
                    AttributeName = "RedirectUri",
                    AttributeType = ScalarAttributeType.S,
                },
                new AttributeDefinition
                {
                    AttributeName = "RedirectType",
                    AttributeType = ScalarAttributeType.N,
                },
                new AttributeDefinition
                {
                    AttributeName = "ApplicationId",
                    AttributeType = ScalarAttributeType.S,
                },
            },
            GlobalSecondaryIndexes = globalSecondaryIndexes,
        }, cancellationToken);

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            throw new Exception($"Couldn't create table {openIddictDynamoDbOptions.ApplicationRedirectsTableName}");
        }

        await DynamoDbUtils.WaitForActiveTableAsync(
            openIddictDynamoDbOptions.Database!,
            openIddictDynamoDbOptions.ApplicationRedirectsTableName,
            cancellationToken);
    }
}