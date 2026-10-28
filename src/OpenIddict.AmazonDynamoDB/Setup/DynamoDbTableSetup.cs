using System.Net;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenIddict.AmazonDynamoDB.Migration;

namespace OpenIddict.AmazonDynamoDB;

/// <summary>What <see cref="DynamoDbTableSetup.MigrateAsync(OpenIddictDynamoDbOptions, IAmazonDynamoDB?, CancellationToken)"/> wrote lookups for.</summary>
public sealed record MigrationResult(int Applications, int Redirects, int Scopes, int Tokens)
{
  public int Total => Applications + Redirects + Scopes + Tokens;
}

public static class DynamoDbTableSetup
{
  /// <summary>
  /// The global secondary indexes of tables created by version 3, and the Resource-index of tables
  /// created before scopes had lookups. Lookup items replace them, which
  /// <see cref="MigrateAsync(OpenIddictDynamoDbOptions, IAmazonDynamoDB?, CancellationToken)"/> writes for an
  /// existing table, and <see cref="RemoveLegacyIndexesAsync(OpenIddictDynamoDbOptions, IAmazonDynamoDB?, CancellationToken)"/> removes them.
  /// </summary>
  public static readonly string[] LegacyIndexNames =
  [
    "ClientId-index",
    "RedirectUri-RedirectType-index",
    "Name-index",
    "ScopeId-index",
    "AuthorizationId-index",
    "ReferenceId-index",
    "Resource-index",
  ];

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

    if (await SetupTable(options, dynamoDb, logger, cancellationToken))
    {
      await AddMissingGlobalSecondaryIndexes(options, dynamoDb, logger, cancellationToken);

      var table = await DescribeTable(dynamoDb, options.DefaultTableName, cancellationToken);
      var legacyIndexes = LegacyIndexes(table);

      if (legacyIndexes.Count > 0)
      {
        logger.LogWarning(
          "Table {TableName} has the global secondary indexes of version 3 ({IndexNames}), which the stores no longer use. Run MigrateAsync, deploy, run it again, then RemoveLegacyIndexesAsync",
          options.DefaultTableName,
          string.Join(", ", legacyIndexes));
      }
    }

    await EnableTimeToLive.Migrate(options, dynamoDb, logger, cancellationToken);
  }

  /// <summary>
  /// Writes the lookup and listing items for every application, redirect URI, scope and token in an
  /// existing table, which version 3 found through global secondary indexes. Safe to run more than
  /// once, and while the table is in use.
  /// </summary>
  public static Task<MigrationResult> MigrateAsync(
    OpenIddictDynamoDbOptions options,
    IAmazonDynamoDB? database = default,
    CancellationToken cancellationToken = default)
    => MigrateAsync(options, database, NullLogger.Instance, cancellationToken);

  internal static async Task<MigrationResult> MigrateAsync(
    OpenIddictDynamoDbOptions options,
    IAmazonDynamoDB? database,
    ILogger logger,
    CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(options);

    var dynamoDb = database ?? options.Database;

    ArgumentNullException.ThrowIfNull(dynamoDb);

    var tableName = options.DefaultTableName;
    var applications = 0;
    var redirects = 0;
    var scopes = 0;
    var tokens = 0;
    Dictionary<string, AttributeValue>? exclusiveStartKey = default;

    logger.LogInformation("Writing lookup items for the items in table {TableName}", tableName);

    do
    {
      var response = await dynamoDb.ScanAsync(new ScanRequest
      {
        TableName = tableName,
        ExclusiveStartKey = exclusiveStartKey,
        ConsistentRead = true,
      }, cancellationToken);

      var requests = new List<WriteRequest>();
      var lookups = new Dictionary<string, string>();

      // Version 3 enforced no uniqueness, so two applications may share a client id, or two tokens
      // a reference id, in which case the lookup points at one of them and the other is reported
      void AddLookup(LookupItem lookup, string what)
      {
        if (lookups.TryGetValue(lookup.PartitionKey, out var holder) && holder != lookup.Id)
        {
          logger.LogWarning(
            "{What} {Holder} and {Other} share the value of {LookupKey} in table {TableName}; the lookup points at the first",
            what, holder, lookup.Id, lookup.PartitionKey, tableName);
          return;
        }

        lookups[lookup.PartitionKey] = lookup.Id;
        requests.Add(DynamoDbItems.Put(lookup));
      }

      foreach (var item in response.Items ?? new())
      {
        var partitionKey = item.GetString("PartitionKey") ?? "";
        var sortKey = item.GetString("SortKey") ?? "";

        if (partitionKey.StartsWith("APPLICATION#", StringComparison.Ordinal) && sortKey.StartsWith("#USER#", StringComparison.Ordinal))
        {
          var application = DynamoDbItems.FromItem<OpenIddictDynamoDbApplication>(item);
          if (application.ClientId is { } clientId)
          {
            AddLookup(new LookupItem(Lookups.ClientId(clientId), application.Id), "Applications");
          }

          applications++;
        }
        else if (partitionKey.StartsWith("APPLICATION#", StringComparison.Ordinal) && sortKey.StartsWith("REDIRECT#", StringComparison.Ordinal))
        {
          var redirect = DynamoDbItems.FromItem<OpenIddictDynamoDbApplicationRedirect>(item);
          if (redirect is { ApplicationId: not null, RedirectUri: not null })
          {
            requests.Add(DynamoDbItems.Put(new MemberItem(
              Lookups.Redirect(redirect.RedirectType, redirect.RedirectUri), Lookups.ApplicationMember(redirect.ApplicationId), redirect.ApplicationId)));
            redirects++;
          }
        }
        else if (partitionKey.StartsWith("SCOPE#", StringComparison.Ordinal) && sortKey.StartsWith("#SCOPE#", StringComparison.Ordinal))
        {
          var scope = DynamoDbItems.FromItem<OpenIddictDynamoDbScope>(item);
          if (scope.Name is { } name)
          {
            var lookup = Lookups.ScopeName(name, scope.Id);
            requests.Add(DynamoDbItems.Put(lookup));
          }

          requests.AddRange((scope.Resources ?? []).Select(x => DynamoDbItems.Put(Lookups.ScopeResource(x, scope.Id))));
          scopes++;
        }
        else if (partitionKey.StartsWith("TOKEN#", StringComparison.Ordinal) && sortKey.StartsWith("#TOKEN#", StringComparison.Ordinal))
        {
          var token = DynamoDbItems.FromItem<OpenIddictDynamoDbToken>(item);
          if (token.ReferenceId is { } referenceId)
          {
            AddLookup(new LookupItem(Lookups.ReferenceId(referenceId), token.Id, token.TTL), "Tokens");
          }

          if (token.AuthorizationId is { } authorizationId)
          {
            requests.Add(DynamoDbItems.Put(new MemberItem(
              new OpenIddictDynamoDbAuthorization { Id = authorizationId }.PartitionKey, Lookups.TokenMember(token.Id), token.Id, token.TTL)));
          }

          tokens++;
        }
      }

      await DynamoDbUtils.BatchWriteAsync(dynamoDb, tableName, requests, cancellationToken);
      exclusiveStartKey = response.LastEvaluatedKey;
    } while (exclusiveStartKey?.Count > 0);

    var result = new MigrationResult(applications, redirects, scopes, tokens);

    logger.LogInformation(
      "Wrote lookup items in table {TableName} for {Applications} applications, {Redirects} redirect URIs, {Scopes} scopes and {Tokens} tokens",
      tableName, applications, redirects, scopes, tokens);

    return result;
  }

  /// <summary>
  /// Removes the global secondary indexes of version 3 from an existing table, one at a time as
  /// DynamoDB requires, once every instance runs version 4 and
  /// <see cref="MigrateAsync(OpenIddictDynamoDbOptions, IAmazonDynamoDB?, CancellationToken)"/> has run.
  /// </summary>
  public static Task RemoveLegacyIndexesAsync(
    OpenIddictDynamoDbOptions options,
    IAmazonDynamoDB? database = default,
    CancellationToken cancellationToken = default)
    => RemoveLegacyIndexesAsync(options, database, NullLogger.Instance, cancellationToken);

  internal static async Task RemoveLegacyIndexesAsync(
    OpenIddictDynamoDbOptions options,
    IAmazonDynamoDB? database,
    ILogger logger,
    CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(options);

    var dynamoDb = database ?? options.Database;

    ArgumentNullException.ThrowIfNull(dynamoDb);

    var table = await DescribeTable(dynamoDb, options.DefaultTableName, cancellationToken)
      ?? throw new Exception($"Table {options.DefaultTableName} does not exist");

    foreach (var indexName in LegacyIndexes(table))
    {
      // DynamoDB removes one index per UpdateTable request, and not while the table or another
      // index is being changed
      await DynamoDbUtils.WaitForActiveTableAsync(dynamoDb, options.DefaultTableName, logger, cancellationToken);

      logger.LogInformation(
        "Removing global secondary index {IndexName} from table {TableName}",
        indexName,
        options.DefaultTableName);

      try
      {
        await dynamoDb.UpdateTableAsync(new UpdateTableRequest
        {
          TableName = options.DefaultTableName,
          GlobalSecondaryIndexUpdates = new()
          {
            new()
            {
              Delete = new()
              {
                IndexName = indexName,
              },
            },
          },
        }, cancellationToken);
      }
      catch (AmazonDynamoDBException exception) when (DynamoDbUtils.IsAccessDenied(exception))
      {
        logger.LogWarning(
          exception,
          "Unable to remove global secondary index {IndexName} from table {TableName}, remove it manually or allow dynamodb:UpdateTable",
          indexName,
          options.DefaultTableName);
        return;
      }
      catch (AmazonDynamoDBException)
      {
        // Another instance might be removing the index at the same time
        var current = await DescribeTable(dynamoDb, options.DefaultTableName, cancellationToken);
        var index = current?.GlobalSecondaryIndexes?.Find(x => x.IndexName == indexName);

        if (index != default && Equals(index.IndexStatus, IndexStatus.DELETING) == false)
        {
          throw;
        }
      }
    }

    await DynamoDbUtils.WaitForActiveTableAsync(dynamoDb, options.DefaultTableName, logger, cancellationToken);
  }

  private static List<string> LegacyIndexes(TableDescription? table) => (table?.GlobalSecondaryIndexes ?? new())
    .Select(x => x.IndexName)
    .Where(LegacyIndexNames.Contains)
    .ToList();

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
    new("ApplicationId", ScalarAttributeType.S),
    new("Subject", ScalarAttributeType.S),
    new("SearchKey", ScalarAttributeType.S),
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
  };
}
