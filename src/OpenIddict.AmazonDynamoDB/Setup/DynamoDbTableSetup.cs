using System.Net;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Util;

namespace OpenIddict.AmazonDynamoDB;

public static class DynamoDbTableSetup
{
  public static Task EnsureInitializedAsync(
    OpenIddictDynamoDbOptions options,
    IAmazonDynamoDB? database = default,
    CancellationToken cancellationToken = default)
  {
    var dynamoDb = database ?? options.Database;

    ArgumentNullException.ThrowIfNull(options);
    ArgumentNullException.ThrowIfNull(dynamoDb);

    if (options.DefaultTableName != Constants.DefaultTableName &&
      AWSConfigsDynamoDB.Context.TableAliases.Any(x => x.Key == Constants.DefaultTableName) == false)
    {
      AWSConfigsDynamoDB.Context.AddAlias(new TableAlias(
        Constants.DefaultTableName, options.DefaultTableName));
    }

    return SetupTable(options, dynamoDb, cancellationToken);
  }

  private static async Task SetupTable(
    OpenIddictDynamoDbOptions options,
    IAmazonDynamoDB database,
    CancellationToken cancellationToken)
  {
    var tableNames = await database.ListTablesAsync(cancellationToken);
    if (tableNames.TableNames.Contains(options.DefaultTableName))
    {
      return;
    }

    var provisionedThroughput = options.BillingMode != BillingMode.PAY_PER_REQUEST
      ? options.ProvisionedThroughput : default;
    var response = await database.CreateTableAsync(new CreateTableRequest
    {
      TableName = options.DefaultTableName,
      ProvisionedThroughput = provisionedThroughput,
      BillingMode = options.BillingMode,
      GlobalSecondaryIndexes = new()
      {
        // Shared
        new()
        {
          IndexName = "ApplicationId-index",
          KeySchema = new List<KeySchemaElement>
          {
            new KeySchemaElement("ApplicationId", KeyType.HASH),
            new KeySchemaElement("SortKey", KeyType.RANGE),
          },
          ProvisionedThroughput = provisionedThroughput,
          Projection = new Projection
          {
            ProjectionType = ProjectionType.ALL,
          },
        },
        new()
        {
          IndexName = "Subject-index",
          KeySchema = new List<KeySchemaElement>
          {
            new KeySchemaElement("Subject", KeyType.HASH),
            new KeySchemaElement("SortKey", KeyType.RANGE),
          },
          ProvisionedThroughput = provisionedThroughput,
          Projection = new Projection
          {
            ProjectionType = ProjectionType.ALL,
          },
        },
        // Application
        new()
        {
          IndexName = "ClientId-index",
          KeySchema = new List<KeySchemaElement>
          {
            new KeySchemaElement("ClientId", KeyType.HASH),
          },
          ProvisionedThroughput = provisionedThroughput,
          Projection = new Projection
          {
            ProjectionType = ProjectionType.ALL,
          },
        },
        // Scope
        new()
        {
          IndexName = "Name-index",
          KeySchema = new List<KeySchemaElement>
          {
            new KeySchemaElement("ScopeName", KeyType.HASH),
          },
          ProvisionedThroughput = provisionedThroughput,
          Projection = new Projection
          {
            ProjectionType = ProjectionType.ALL,
          },
        },
        new()
        {
          IndexName = "Resource-index",
          KeySchema = new List<KeySchemaElement>
          {
            new KeySchemaElement("ScopeResource", KeyType.HASH),
          },
          ProvisionedThroughput = provisionedThroughput,
          Projection = new Projection
          {
            ProjectionType = ProjectionType.ALL,
          },
        },
        // Token
        new()
        {
          IndexName = "AuthorizationId-index",
          KeySchema = new List<KeySchemaElement>
          {
            new KeySchemaElement("AuthorizationId", KeyType.HASH),
          },
          ProvisionedThroughput = provisionedThroughput,
          Projection = new Projection
          {
            ProjectionType = ProjectionType.ALL,
          },
        },
        new()
        {
          IndexName = "ReferenceId-index",
          KeySchema = new List<KeySchemaElement>
          {
            new KeySchemaElement("ReferenceId", KeyType.HASH),
          },
          ProvisionedThroughput = provisionedThroughput,
          Projection = new Projection
          {
            ProjectionType = ProjectionType.ALL,
          },
        },
      },
      KeySchema = new List<KeySchemaElement>
      {
        new("PartitionKey", KeyType.HASH),
        new("SortKey", KeyType.RANGE),
      },
      AttributeDefinitions = new List<AttributeDefinition>
      {
        new("PartitionKey", ScalarAttributeType.S),
        new("SortKey", ScalarAttributeType.S),
        // Application
        new("ClientId", ScalarAttributeType.S),
        new("ApplicationId", ScalarAttributeType.S),
        // Authorization
        new("Subject", ScalarAttributeType.S),
        // Scope
        new("ScopeName", ScalarAttributeType.S),
        new("ScopeResource", ScalarAttributeType.S),
        // Token
        new("AuthorizationId", ScalarAttributeType.S),
        new("ReferenceId", ScalarAttributeType.S),
      },
    }, cancellationToken);

    if (response.HttpStatusCode != HttpStatusCode.OK)
    {
      throw new Exception($"Couldn't create table {options.DefaultTableName}");
    }

    await DynamoDbUtils.WaitForActiveTableAsync(
      database,
      options.DefaultTableName,
      cancellationToken);
  }
}
