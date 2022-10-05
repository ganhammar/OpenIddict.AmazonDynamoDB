using System.Net;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Util;

namespace OpenIddict.AmazonDynamoDB;

public static class OpenIddictDynamoDbTokenSetup
{
    public static Task EnsureInitializedAsync(
        OpenIddictDynamoDbOptions options,
        IAmazonDynamoDB? database = default,
        CancellationToken cancellationToken = default)
    {
        var dynamoDb = database ?? options.Database;

        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(dynamoDb);

        if (options.TokensTableName != Constants.DefaultTokenTableName)
        {
            AWSConfigsDynamoDB.Context.AddAlias(new TableAlias(
                options.TokensTableName, Constants.DefaultTokenTableName));
        }

        return SetupTable(options, dynamoDb, cancellationToken);
    }

    private static async Task SetupTable(
        OpenIddictDynamoDbOptions options,
        IAmazonDynamoDB database,
        CancellationToken cancellationToken)
    {
        var tokenGlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
        {
            new GlobalSecondaryIndex
            {
                IndexName = "Subject-SearchKey-index",
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement("Subject", KeyType.HASH),
                    new KeySchemaElement("SearchKey", KeyType.RANGE),
                },
                ProvisionedThroughput = options.ProvisionedThroughput,
                Projection = new Projection
                {
                    ProjectionType = ProjectionType.ALL,
                },
            },
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
            new GlobalSecondaryIndex
            {
                IndexName = "AuthorizationId-index",
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement("AuthorizationId", KeyType.HASH),
                },
                ProvisionedThroughput = options.ProvisionedThroughput,
                Projection = new Projection
                {
                    ProjectionType = ProjectionType.ALL,
                },
            },
            new GlobalSecondaryIndex
            {
                IndexName = "ReferenceId-index",
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement("ReferenceId", KeyType.HASH),
                },
                ProvisionedThroughput = options.ProvisionedThroughput,
                Projection = new Projection
                {
                    ProjectionType = ProjectionType.ALL,
                },
            },
        };

        var tableNames = await database.ListTablesAsync(cancellationToken);

        if (!tableNames.TableNames.Contains(options.TokensTableName))
        {
            await CreateTokenTableAsync(
                options,
                database,
                tokenGlobalSecondaryIndexes,
                cancellationToken);
        }
        else
        {
            await DynamoDbUtils.UpdateSecondaryIndexes(
                database,
                options.TokensTableName,
                tokenGlobalSecondaryIndexes,
                cancellationToken);
        }
    }

    private static async Task CreateTokenTableAsync(
        OpenIddictDynamoDbOptions options,
        IAmazonDynamoDB database,
        List<GlobalSecondaryIndex>? globalSecondaryIndexes,
        CancellationToken cancellationToken)
    {
        var response = await database.CreateTableAsync(new CreateTableRequest
        {
            TableName = options.TokensTableName,
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
                    AttributeName = "ApplicationId",
                    AttributeType = ScalarAttributeType.S,
                },
                new AttributeDefinition
                {
                    AttributeName = "Subject",
                    AttributeType = ScalarAttributeType.S,
                },
                new AttributeDefinition
                {
                    AttributeName = "AuthorizationId",
                    AttributeType = ScalarAttributeType.S,
                },
                new AttributeDefinition
                {
                    AttributeName = "ReferenceId",
                    AttributeType = ScalarAttributeType.S,
                },
                new AttributeDefinition
                {
                    AttributeName = "SearchKey",
                    AttributeType = ScalarAttributeType.S,
                },
            },
            GlobalSecondaryIndexes = globalSecondaryIndexes,
        }, cancellationToken);

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            throw new Exception($"Couldn't create table {options.TokensTableName}");
        }

        await DynamoDbUtils.WaitForActiveTableAsync(
            database,
            options.TokensTableName,
            cancellationToken);
    }
}