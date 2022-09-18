using System.Net;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Util;

namespace OpenIddict.DynamoDB;

public static class OpenIddictDynamoDbAuthorizationSetup
{
    public static Task EnsureInitializedAsync(
        OpenIddictDynamoDbOptions openIddictDynamoDbOptions,
        CancellationToken cancellationToken = default)
    {
        if (openIddictDynamoDbOptions == null)
        {
            throw new ArgumentNullException(nameof(openIddictDynamoDbOptions));
        }

        if (openIddictDynamoDbOptions.Database == null)
        {
            throw new ArgumentNullException(nameof(openIddictDynamoDbOptions.Database));
        }

        if (openIddictDynamoDbOptions.AuthorizationsTableName != Constants.DefaultAuthorizationTableName)
        {
            AWSConfigsDynamoDB.Context.AddAlias(new TableAlias(
                openIddictDynamoDbOptions.AuthorizationsTableName, Constants.DefaultAuthorizationTableName));
        }

        return SetupTable(openIddictDynamoDbOptions, cancellationToken);
    }

    private static async Task SetupTable(
        OpenIddictDynamoDbOptions openIddictDynamoDbOptions,
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
                ProvisionedThroughput = openIddictDynamoDbOptions.ProvisionedThroughput,
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
                ProvisionedThroughput = openIddictDynamoDbOptions.ProvisionedThroughput,
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
                ProvisionedThroughput = openIddictDynamoDbOptions.ProvisionedThroughput,
                Projection = new Projection
                {
                    ProjectionType = ProjectionType.ALL,
                },
            },
        };

        var tableNames = await openIddictDynamoDbOptions.Database!.ListTablesAsync(cancellationToken);

        if (!tableNames.TableNames.Contains(openIddictDynamoDbOptions.AuthorizationsTableName))
        {
            await CreateAuthorizationTableAsync(
                openIddictDynamoDbOptions,
                authorizationGlobalSecondaryIndexes,
                cancellationToken);
        }
        else
        {
            await DynamoDbUtils.UpdateSecondaryIndexes(
                openIddictDynamoDbOptions.Database,
                openIddictDynamoDbOptions.AuthorizationsTableName,
                authorizationGlobalSecondaryIndexes,
                cancellationToken);
        }
    }

    private static async Task CreateAuthorizationTableAsync(
        OpenIddictDynamoDbOptions openIddictDynamoDbOptions,
        List<GlobalSecondaryIndex>? globalSecondaryIndexes,
        CancellationToken cancellationToken)
    {
        var response = await openIddictDynamoDbOptions.Database!.CreateTableAsync(new CreateTableRequest
        {
            TableName = openIddictDynamoDbOptions.AuthorizationsTableName,
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
                    AttributeName = "SearchKey",
                    AttributeType = ScalarAttributeType.S,
                },
            },
            GlobalSecondaryIndexes = globalSecondaryIndexes,
        }, cancellationToken);

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            throw new Exception($"Couldn't create table {openIddictDynamoDbOptions.AuthorizationsTableName}");
        }

        await DynamoDbUtils.WaitForActiveTableAsync(
            openIddictDynamoDbOptions.Database,
            openIddictDynamoDbOptions.AuthorizationsTableName,
            cancellationToken);
    }
}