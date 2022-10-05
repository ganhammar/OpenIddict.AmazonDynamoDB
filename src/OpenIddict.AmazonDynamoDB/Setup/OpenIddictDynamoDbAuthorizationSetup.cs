using System.Net;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Util;

namespace OpenIddict.AmazonDynamoDB;

public static class OpenIddictDynamoDbAuthorizationSetup
{
    public static Task EnsureInitializedAsync(
        OpenIddictDynamoDbOptions options,
        IAmazonDynamoDB? database = default,
        CancellationToken cancellationToken = default)
    {
        var dynamoDb = database ?? options.Database;

        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(dynamoDb);

        if (options.AuthorizationsTableName != Constants.DefaultAuthorizationTableName)
        {
            AWSConfigsDynamoDB.Context.AddAlias(new TableAlias(
                options.AuthorizationsTableName, Constants.DefaultAuthorizationTableName));
        }

        return SetupTable(options, dynamoDb, cancellationToken);
    }

    private static async Task SetupTable(
        OpenIddictDynamoDbOptions options,
        IAmazonDynamoDB database,
        CancellationToken cancellationToken)
    {
        var authorizationGlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
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
            new GlobalSecondaryIndex
            {
                IndexName = "Subject-index",
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement("Subject", KeyType.HASH),
                },
                ProvisionedThroughput = options.ProvisionedThroughput,
                Projection = new Projection
                {
                    ProjectionType = ProjectionType.ALL,
                },
            },
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
        };

        var tableNames = await database.ListTablesAsync(cancellationToken);

        if (!tableNames.TableNames.Contains(options.AuthorizationsTableName))
        {
            await CreateAuthorizationTableAsync(
                options,
                database,
                authorizationGlobalSecondaryIndexes,
                cancellationToken);
        }
        else
        {
            await DynamoDbUtils.UpdateSecondaryIndexes(
                database,
                options.AuthorizationsTableName,
                authorizationGlobalSecondaryIndexes,
                cancellationToken);
        }
    }

    private static async Task CreateAuthorizationTableAsync(
        OpenIddictDynamoDbOptions options,
        IAmazonDynamoDB database,
        List<GlobalSecondaryIndex>? globalSecondaryIndexes,
        CancellationToken cancellationToken)
    {
        var response = await database.CreateTableAsync(new CreateTableRequest
        {
            TableName = options.AuthorizationsTableName,
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
                    AttributeName = "SearchKey",
                    AttributeType = ScalarAttributeType.S,
                },
            },
            GlobalSecondaryIndexes = globalSecondaryIndexes,
        }, cancellationToken);

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            throw new Exception($"Couldn't create table {options.AuthorizationsTableName}");
        }

        await DynamoDbUtils.WaitForActiveTableAsync(
            database,
            options.AuthorizationsTableName,
            cancellationToken);
    }
}