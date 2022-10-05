using System.Net;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Util;

namespace OpenIddict.AmazonDynamoDB;

public static class OpenIddictDynamoDbScopeSetup
{
    public static Task EnsureInitializedAsync(
        OpenIddictDynamoDbOptions options,
        IAmazonDynamoDB? database = default,
        CancellationToken cancellationToken = default)
    {
        var dynamoDb = database ?? options.Database;

        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(dynamoDb);

        if (options.ScopesTableName != Constants.DefaultScopeTableName)
        {
            AWSConfigsDynamoDB.Context.AddAlias(new TableAlias(
                options.ScopesTableName, Constants.DefaultScopeTableName));
        }

        return SetupTables(options, dynamoDb, cancellationToken);
    }

    private static async Task SetupTables(
        OpenIddictDynamoDbOptions options,
        IAmazonDynamoDB database,
        CancellationToken cancellationToken)
    {
        var scopeGlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
        {
            new GlobalSecondaryIndex
            {
                IndexName = "Name-index",
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement("ScopeName", KeyType.HASH),
                },
                ProvisionedThroughput = options.ProvisionedThroughput,
                Projection = new Projection
                {
                    ProjectionType = ProjectionType.ALL,
                },
            },
        };
        var scopeResourceGlobalSecondaryIndex = new List<GlobalSecondaryIndex>
        {
            new GlobalSecondaryIndex
            {
                IndexName = "Resource-index",
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement("ScopeResource", KeyType.HASH),
                },
                ProvisionedThroughput = options.ProvisionedThroughput,
                Projection = new Projection
                {
                    ProjectionType = ProjectionType.ALL,
                },
            },
        };

        var tableNames = await database.ListTablesAsync(cancellationToken);

        if (!tableNames.TableNames.Contains(options.ScopesTableName))
        {
            await CreateScopeTableAsync(
                options,
                database,
                scopeGlobalSecondaryIndexes,
                cancellationToken);
        }
        else
        {
            await DynamoDbUtils.UpdateSecondaryIndexes(
                database,
                options.ScopesTableName,
                scopeGlobalSecondaryIndexes,
                cancellationToken);
        }

        if (!tableNames.TableNames.Contains(options.ScopeResourcesTableName))
        {
            await CreateScopeResourceTableAsync(
                options,
                database,
                scopeResourceGlobalSecondaryIndex,
                cancellationToken);
        }
        else
        {
            await DynamoDbUtils.UpdateSecondaryIndexes(
                database,
                options.ScopeResourcesTableName,
                scopeResourceGlobalSecondaryIndex,
                cancellationToken);
        }
    }

    private static async Task CreateScopeTableAsync(
        OpenIddictDynamoDbOptions options,
        IAmazonDynamoDB database,
        List<GlobalSecondaryIndex>? globalSecondaryIndexes,
        CancellationToken cancellationToken)
    {
        var response = await database.CreateTableAsync(new CreateTableRequest
        {
            TableName = options.ScopesTableName,
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
                    AttributeName = "ScopeName",
                    AttributeType = ScalarAttributeType.S,
                },
            },
            GlobalSecondaryIndexes = globalSecondaryIndexes,
        }, cancellationToken);

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            throw new Exception($"Couldn't create table {options.ScopesTableName}");
        }

        await DynamoDbUtils.WaitForActiveTableAsync(
            database,
            options.ScopesTableName,
            cancellationToken);
    }

    private static async Task CreateScopeResourceTableAsync(
        OpenIddictDynamoDbOptions options,
        IAmazonDynamoDB database,
        List<GlobalSecondaryIndex>? globalSecondaryIndexes,
        CancellationToken cancellationToken)
    {
        var response = await database.CreateTableAsync(new CreateTableRequest
        {
            TableName = options.ScopeResourcesTableName,
            ProvisionedThroughput = options.ProvisionedThroughput,
            BillingMode = options.BillingMode,
            KeySchema = new List<KeySchemaElement>
            {
                new KeySchemaElement
                {
                    AttributeName = "ScopeId",
                    KeyType = KeyType.HASH,
                },
                new KeySchemaElement
                {
                    AttributeName = "ScopeResource",
                    KeyType = KeyType.RANGE,
                },
            },
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new AttributeDefinition
                {
                    AttributeName = "ScopeId",
                    AttributeType = ScalarAttributeType.S,
                },
                new AttributeDefinition
                {
                    AttributeName = "ScopeResource",
                    AttributeType = ScalarAttributeType.S,
                },
            },
            GlobalSecondaryIndexes = globalSecondaryIndexes,
        }, cancellationToken);

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            throw new Exception($"Couldn't create table {options.ScopeResourcesTableName}");
        }

        await DynamoDbUtils.WaitForActiveTableAsync(
            database,
            options.ScopeResourcesTableName,
            cancellationToken);
    }
}