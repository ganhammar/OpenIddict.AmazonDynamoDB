using System.Net;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Util;

namespace OpenIddict.AmazonDynamoDB;

public static class OpenIddictDynamoDbApplicationSetup
{
    public static Task EnsureInitializedAsync(
        OpenIddictDynamoDbOptions options,
        IAmazonDynamoDB? database = default,
        CancellationToken cancellationToken = default)
    {
        var dynamoDb = database ?? options.Database;

        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(dynamoDb);

        if (options.ApplicationsTableName != Constants.DefaultApplicationTableName)
        {
            AWSConfigsDynamoDB.Context.AddAlias(new TableAlias(
                options.ApplicationsTableName,
                Constants.DefaultApplicationTableName));
        }

        if (options.ApplicationRedirectsTableName != Constants.DefaultApplicationRedirectsTableName)
        {
            AWSConfigsDynamoDB.Context.AddAlias(new TableAlias(
                options.ApplicationRedirectsTableName,
                Constants.DefaultApplicationRedirectsTableName));
        }

        return SetupTables(options, dynamoDb, cancellationToken);
    }

    private static async Task SetupTables(
        OpenIddictDynamoDbOptions options,
        IAmazonDynamoDB database,
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
                ProvisionedThroughput = options.ProvisionedThroughput,
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
                ProvisionedThroughput = options.ProvisionedThroughput,
                Projection = new Projection
                {
                    ProjectionType = ProjectionType.ALL,
                },
            },
        };

        var tableNames = await database.ListTablesAsync(cancellationToken);

        if (!tableNames.TableNames.Contains(options.ApplicationsTableName))
        {
            await CreateApplicationTableAsync(
                options, database, applicationGlobalSecondaryIndexes, cancellationToken);
        }
        else
        {
            await DynamoDbUtils.UpdateSecondaryIndexes(
                database,
                options.ApplicationsTableName,
                applicationGlobalSecondaryIndexes,
                cancellationToken);
        }

        if (!tableNames.TableNames.Contains(options.ApplicationRedirectsTableName))
        {
            await CreateApplicationRedirectTableAsync(
                options,
                database,
                applicationRedirectGlobalSecondaryIndexes,
                cancellationToken);
        }
        else
        {
            await DynamoDbUtils.UpdateSecondaryIndexes(
                database,
                options.ApplicationRedirectsTableName,
                applicationRedirectGlobalSecondaryIndexes,
                cancellationToken);
        }
    }

    private static async Task CreateApplicationTableAsync(
        OpenIddictDynamoDbOptions options,
        IAmazonDynamoDB database,
        List<GlobalSecondaryIndex>? globalSecondaryIndexes,
        CancellationToken cancellationToken)
    {
        var response = await database.CreateTableAsync(new CreateTableRequest
        {
            TableName = options.ApplicationsTableName,
            ProvisionedThroughput = options.ProvisionedThroughput,
            BillingMode = options.BillingMode,
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
            throw new Exception($"Couldn't create table {options.ApplicationsTableName}");
        }

        await DynamoDbUtils.WaitForActiveTableAsync(
            database,
            options.ApplicationsTableName,
            cancellationToken);
    }

    private static async Task CreateApplicationRedirectTableAsync(
        OpenIddictDynamoDbOptions options,
        IAmazonDynamoDB database,
        List<GlobalSecondaryIndex>? globalSecondaryIndexes,
        CancellationToken cancellationToken)
    {
        var response = await database.CreateTableAsync(new CreateTableRequest
        {
            TableName = options.ApplicationRedirectsTableName,
            ProvisionedThroughput = options.ProvisionedThroughput,
            BillingMode = options.BillingMode,
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
            throw new Exception($"Couldn't create table {options.ApplicationRedirectsTableName}");
        }

        await DynamoDbUtils.WaitForActiveTableAsync(
            database,
            options.ApplicationRedirectsTableName,
            cancellationToken);
    }
}