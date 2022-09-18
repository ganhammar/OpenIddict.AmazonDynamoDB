using System.Net;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Util;

namespace OpenIddict.DynamoDB;

public static class OpenIddictDynamoDbScopeSetup
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

        if (openIddictDynamoDbOptions.ScopesTableName != Constants.DefaultScopeTableName)
        {
            AWSConfigsDynamoDB.Context.AddAlias(new TableAlias(
                openIddictDynamoDbOptions.ScopesTableName, Constants.DefaultScopeTableName));
        }

        return SetupTable(openIddictDynamoDbOptions, cancellationToken);
    }

    private static async Task SetupTable(
        OpenIddictDynamoDbOptions openIddictDynamoDbOptions,
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
                ProvisionedThroughput = openIddictDynamoDbOptions.ProvisionedThroughput,
                Projection = new Projection
                {
                    ProjectionType = ProjectionType.ALL,
                },
            },
        };

        var tableNames = await openIddictDynamoDbOptions.Database!.ListTablesAsync(cancellationToken);

        if (!tableNames.TableNames.Contains(openIddictDynamoDbOptions.ScopesTableName))
        {
            await CreateScopeTableAsync(
                openIddictDynamoDbOptions,
                scopeGlobalSecondaryIndexes,
                cancellationToken);
        }
        else
        {
            await DynamoDbUtils.UpdateSecondaryIndexes(
                openIddictDynamoDbOptions.Database,
                openIddictDynamoDbOptions.ScopesTableName,
                scopeGlobalSecondaryIndexes,
                cancellationToken);
        }
    }

    private static async Task CreateScopeTableAsync(
        OpenIddictDynamoDbOptions openIddictDynamoDbOptions,
        List<GlobalSecondaryIndex>? globalSecondaryIndexes,
        CancellationToken cancellationToken)
    {
        var response = await openIddictDynamoDbOptions.Database!.CreateTableAsync(new CreateTableRequest
        {
            TableName = openIddictDynamoDbOptions.ScopesTableName,
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
                    AttributeName = "ScopeName",
                    AttributeType = ScalarAttributeType.S,
                },
            },
            GlobalSecondaryIndexes = globalSecondaryIndexes,
        }, cancellationToken);

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            throw new Exception($"Couldn't create table {openIddictDynamoDbOptions.ScopesTableName}");
        }

        await DynamoDbUtils.WaitForActiveTableAsync(
            openIddictDynamoDbOptions.Database,
            openIddictDynamoDbOptions.ScopesTableName,
            cancellationToken);
    }
}