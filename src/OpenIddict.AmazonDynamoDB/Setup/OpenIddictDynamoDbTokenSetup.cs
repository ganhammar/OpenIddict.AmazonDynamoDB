using System.Net;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Util;

namespace OpenIddict.AmazonDynamoDB;

public static class OpenIddictDynamoDbTokenSetup
{
    public static Task EnsureInitializedAsync(
        OpenIddictDynamoDbOptions openIddictDynamoDbOptions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(openIddictDynamoDbOptions);
        ArgumentNullException.ThrowIfNull(openIddictDynamoDbOptions.Database);

        if (openIddictDynamoDbOptions.TokensTableName != Constants.DefaultTokenTableName)
        {
            AWSConfigsDynamoDB.Context.AddAlias(new TableAlias(
                openIddictDynamoDbOptions.TokensTableName, Constants.DefaultTokenTableName));
        }

        return SetupTable(openIddictDynamoDbOptions, cancellationToken);
    }

    private static async Task SetupTable(
        OpenIddictDynamoDbOptions openIddictDynamoDbOptions,
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
                ProvisionedThroughput = openIddictDynamoDbOptions.ProvisionedThroughput,
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
                ProvisionedThroughput = openIddictDynamoDbOptions.ProvisionedThroughput,
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
                ProvisionedThroughput = openIddictDynamoDbOptions.ProvisionedThroughput,
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
                ProvisionedThroughput = openIddictDynamoDbOptions.ProvisionedThroughput,
                Projection = new Projection
                {
                    ProjectionType = ProjectionType.ALL,
                },
            },
        };

        var tableNames = await openIddictDynamoDbOptions.Database!.ListTablesAsync(cancellationToken);

        if (!tableNames.TableNames.Contains(openIddictDynamoDbOptions.TokensTableName))
        {
            await CreateTokenTableAsync(
                openIddictDynamoDbOptions,
                tokenGlobalSecondaryIndexes,
                cancellationToken);
        }
        else
        {
            await DynamoDbUtils.UpdateSecondaryIndexes(
                openIddictDynamoDbOptions.Database,
                openIddictDynamoDbOptions.TokensTableName,
                tokenGlobalSecondaryIndexes,
                cancellationToken);
        }
    }

    private static async Task CreateTokenTableAsync(
        OpenIddictDynamoDbOptions openIddictDynamoDbOptions,
        List<GlobalSecondaryIndex>? globalSecondaryIndexes,
        CancellationToken cancellationToken)
    {
        var response = await openIddictDynamoDbOptions.Database!.CreateTableAsync(new CreateTableRequest
        {
            TableName = openIddictDynamoDbOptions.TokensTableName,
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
            throw new Exception($"Couldn't create table {openIddictDynamoDbOptions.TokensTableName}");
        }

        await DynamoDbUtils.WaitForActiveTableAsync(
            openIddictDynamoDbOptions.Database,
            openIddictDynamoDbOptions.TokensTableName,
            cancellationToken);
    }
}