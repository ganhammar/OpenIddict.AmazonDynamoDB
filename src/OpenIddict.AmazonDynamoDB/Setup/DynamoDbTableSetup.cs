using System.Net;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenIddict.AmazonDynamoDB.Migration;

namespace OpenIddict.AmazonDynamoDB;

public static class DynamoDbTableSetup
{
  public static Task EnsureInitializedAsync(
    OpenIddictDynamoDbOptions options,
    IAmazonDynamoDB? database = default,
    CancellationToken cancellationToken = default)
    => EnsureInitializedAsync(options, database, NullLogger.Instance, cancellationToken);

  internal static async Task EnsureInitializedAsync(
    OpenIddictDynamoDbOptions options,
    IAmazonDynamoDB? database,
    ILogger logger,
    CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(options);

    var dynamoDb = database ?? options.Database;

    ArgumentNullException.ThrowIfNull(dynamoDb);

    EnsureAliasCreated(options);

    if (await SetupTable(options, dynamoDb, logger, cancellationToken))
    {
      await UpdateScopeIndexes.RemoveResourceIndex(options, dynamoDb, logger, cancellationToken);
      await AddMissingGlobalSecondaryIndexes(options, dynamoDb, logger, cancellationToken);
    }

    await EnableTimeToLive.Migrate(options, dynamoDb, logger, cancellationToken);
  }

  public static void EnsureAliasCreated(OpenIddictDynamoDbOptions options)
  {
    if (options.DefaultTableName != Constants.DefaultTableName)
    {
      AWSConfigsDynamoDB.Context.TableAliases
        .TryAdd(Constants.DefaultTableName, options.DefaultTableName);
    }
  }

  // Returns false when the table can't be described, so that its indexes can't be verified
  private static async Task<bool> SetupTable(
    OpenIddictDynamoDbOptions options,
    IAmazonDynamoDB database,
    ILogger logger,
    CancellationToken cancellationToken)
  {
    TableDescription? table;

    try
    {
      table = await DescribeTable(database, options.DefaultTableName, cancellationToken);
    }
    catch (AmazonDynamoDBException exception) when (DynamoDbUtils.IsAccessDenied(exception))
    {
      // Without dynamodb:DescribeTable the indexes can't be verified, only make sure the table exists
      logger.LogWarning(
        exception,
        "Unable to describe table {TableName}, missing global secondary indexes will not be added",
        options.DefaultTableName);

      if (await TableExists(database, options.DefaultTableName, cancellationToken))
      {
        return false;
      }

      table = default;
    }

    if (table == default)
    {
      if (await TryCreateTable(options, database, logger, cancellationToken))
      {
        return true;
      }

      // The table was created by someone else after it was described
      table = await DescribeTable(database, options.DefaultTableName, cancellationToken)
        ?? throw new Exception($"Couldn't create table {options.DefaultTableName}");
    }

    if (Equals(table.TableStatus, TableStatus.ACTIVE) == false)
    {
      // The table is still being created or updated, possibly by another instance
      await DynamoDbUtils.WaitForActiveTableAsync(
        database,
        options.DefaultTableName,
        logger,
        cancellationToken);
    }

    return true;
  }

  private static async Task<bool> TryCreateTable(
    OpenIddictDynamoDbOptions options,
    IAmazonDynamoDB database,
    ILogger logger,
    CancellationToken cancellationToken)
  {
    var provisionedThroughput = options.BillingMode != BillingMode.PAY_PER_REQUEST
      ? options.ProvisionedThroughput : default;

    logger.LogInformation("Creating table {TableName}", options.DefaultTableName);

    CreateTableResponse response;
    try
    {
      response = await database.CreateTableAsync(new()
      {
        TableName = options.DefaultTableName,
        ProvisionedThroughput = provisionedThroughput,
        BillingMode = options.BillingMode,
        GlobalSecondaryIndexes = GetGlobalSecondaryIndexes(provisionedThroughput),
        KeySchema = new List<KeySchemaElement>
        {
          new("PartitionKey", KeyType.HASH),
          new("SortKey", KeyType.RANGE),
        },
        AttributeDefinitions = AttributeDefinitions,
      }, cancellationToken);
    }
    catch (ResourceInUseException)
    {
      return false;
    }

    if (response.HttpStatusCode != HttpStatusCode.OK)
    {
      throw new Exception($"Couldn't create table {options.DefaultTableName}");
    }

    await DynamoDbUtils.WaitForActiveTableAsync(
      database,
      options.DefaultTableName,
      logger,
      cancellationToken);

    return true;
  }

  internal static async Task AddMissingGlobalSecondaryIndexes(
    OpenIddictDynamoDbOptions options,
    IAmazonDynamoDB database,
    ILogger logger,
    CancellationToken cancellationToken)
  {
    var table = await DescribeTable(database, options.DefaultTableName, cancellationToken)
      ?? throw new Exception($"Couldn't find table {options.DefaultTableName}");

    // The billing mode of the existing table decides whether the index needs provisioned
    // throughput, the table might not have been created with the current options
    var isProvisioned = (table.BillingModeSummary?.BillingMode ?? BillingMode.PROVISIONED) == BillingMode.PROVISIONED;
    var existingIndexNames = (table.GlobalSecondaryIndexes ?? new())
      .Select(x => x.IndexName)
      .ToHashSet();
    var missingIndexes = GetGlobalSecondaryIndexes(isProvisioned ? options.ProvisionedThroughput : default)
      .Where(x => existingIndexNames.Contains(x.IndexName) == false)
      .ToList();

    foreach (var index in missingIndexes)
    {
      // DynamoDB accepts one new global secondary index per UpdateTable request, and it can't
      // be sent while the table or another index is still being created
      await DynamoDbUtils.WaitForActiveTableAsync(
        database,
        options.DefaultTableName,
        logger,
        cancellationToken);

      logger.LogInformation(
        "Adding global secondary index {IndexName} to table {TableName}",
        index.IndexName,
        options.DefaultTableName);

      var keyAttributeNames = index.KeySchema.Select(x => x.AttributeName).ToHashSet();

      try
      {
        var response = await database.UpdateTableAsync(new UpdateTableRequest
        {
          TableName = options.DefaultTableName,
          AttributeDefinitions = AttributeDefinitions
            .Where(x => keyAttributeNames.Contains(x.AttributeName))
            .ToList(),
          GlobalSecondaryIndexUpdates = new()
          {
            new()
            {
              Create = new()
              {
                IndexName = index.IndexName,
                KeySchema = index.KeySchema,
                Projection = index.Projection,
                ProvisionedThroughput = index.ProvisionedThroughput,
              },
            },
          },
        }, cancellationToken);

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
          throw new Exception($"Couldn't create index {index.IndexName} on table {options.DefaultTableName}");
        }
      }
      catch (AmazonDynamoDBException exception) when (DynamoDbUtils.IsAccessDenied(exception))
      {
        logger.LogWarning(
          exception,
          "Unable to add global secondary index {IndexName} to table {TableName}, add it manually or allow dynamodb:UpdateTable",
          index.IndexName,
          options.DefaultTableName);
        return;
      }
      catch (AmazonDynamoDBException)
      {
        // Another instance might have added the index at the same time
        if (await IndexExists(database, options.DefaultTableName, index.IndexName, cancellationToken) == false)
        {
          throw;
        }
      }
    }

    if (missingIndexes.Count > 0)
    {
      await DynamoDbUtils.WaitForActiveTableAsync(
        database,
        options.DefaultTableName,
        logger,
        cancellationToken);
    }
  }

  internal static async Task<TableDescription?> DescribeTable(
    IAmazonDynamoDB database,
    string tableName,
    CancellationToken cancellationToken)
  {
    try
    {
      var response = await database.DescribeTableAsync(new DescribeTableRequest
      {
        TableName = tableName,
      }, cancellationToken);

      return response.Table;
    }
    catch (ResourceNotFoundException)
    {
      return default;
    }
  }

  private static async Task<bool> TableExists(
    IAmazonDynamoDB database,
    string tableName,
    CancellationToken cancellationToken)
  {
    string? lastEvaluatedTableName = default;

    do
    {
      var response = await database.ListTablesAsync(new ListTablesRequest
      {
        ExclusiveStartTableName = lastEvaluatedTableName,
      }, cancellationToken);

      if (response.TableNames?.Contains(tableName) == true)
      {
        return true;
      }

      lastEvaluatedTableName = response.LastEvaluatedTableName;
    } while (lastEvaluatedTableName != default);

    return false;
  }

  private static async Task<bool> IndexExists(
    IAmazonDynamoDB database,
    string tableName,
    string indexName,
    CancellationToken cancellationToken)
  {
    var table = await DescribeTable(database, tableName, cancellationToken);
    return table?.GlobalSecondaryIndexes?.Exists(x => x.IndexName == indexName) == true;
  }

  private static List<AttributeDefinition> AttributeDefinitions => new()
  {
    new("PartitionKey", ScalarAttributeType.S),
    new("SortKey", ScalarAttributeType.S),
    // Application
    new("ClientId", ScalarAttributeType.S),
    new("ApplicationId", ScalarAttributeType.S),
    new("RedirectUri", ScalarAttributeType.S),
    new("RedirectType", ScalarAttributeType.N),
    // Authorization
    new("Subject", ScalarAttributeType.S),
    new("SearchKey", ScalarAttributeType.S),
    // Scope
    new("ScopeName", ScalarAttributeType.S),
    // Scope Lookup
    new("ScopeId", ScalarAttributeType.S),
    // Token
    new("AuthorizationId", ScalarAttributeType.S),
    new("ReferenceId", ScalarAttributeType.S),
  };

  private static List<GlobalSecondaryIndex> GetGlobalSecondaryIndexes(
    ProvisionedThroughput? provisionedThroughput) => new()
  {
    // Shared
    new()
    {
      IndexName = "ApplicationId-index",
      KeySchema = new List<KeySchemaElement>
      {
        new("ApplicationId", KeyType.HASH),
        new("SortKey", KeyType.RANGE),
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
        new("Subject", KeyType.HASH),
        new("SearchKey", KeyType.RANGE),
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
        new("ClientId", KeyType.HASH),
      },
      ProvisionedThroughput = provisionedThroughput,
      Projection = new Projection
      {
        ProjectionType = ProjectionType.ALL,
      },
    },
    new()
    {
      IndexName = "RedirectUri-RedirectType-index",
      KeySchema = new List<KeySchemaElement>
      {
        new("RedirectUri", KeyType.HASH),
        new("RedirectType", KeyType.RANGE),
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
        new("ScopeName", KeyType.HASH),
      },
      ProvisionedThroughput = provisionedThroughput,
      Projection = new Projection
      {
        ProjectionType = ProjectionType.ALL,
      },
    },
    new()
    {
      IndexName = "ScopeId-index",
      KeySchema = new List<KeySchemaElement>
      {
        new("ScopeId", KeyType.HASH),
        new("PartitionKey", KeyType.RANGE),
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
        new("AuthorizationId", KeyType.HASH),
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
        new("ReferenceId", KeyType.HASH),
      },
      ProvisionedThroughput = provisionedThroughput,
      Projection = new Projection
      {
        ProjectionType = ProjectionType.ALL,
      },
    },
  };
}
