using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace OpenIddict.AmazonDynamoDB.Tests;

[Collection(Constants.DatabaseCollection)]
public class OpenIddictDynamoDbSetupTests(DatabaseFixture fixture)
{
  public readonly IAmazonDynamoDB _client = fixture.Client;

  [Fact]
  public async Task Should_SetupTables_When_CalledSynchronously()
  {
    // Arrange
    var options = TestUtils.GetOptions(new()
    {
      Database = _client,
    });

    // Act
    OpenIddictDynamoDbSetup.EnsureInitialized(options);

    // Assert
    var tableNames = await _client.ListTablesAsync();
    Assert.Contains(DatabaseFixture.TableName, tableNames.TableNames);
  }

  [Fact]
  public async Task Should_SetupTables_When_CalledSynchronouslyWithServiceProvider()
  {
    // Arrange
    var services = new ServiceCollection();
    CreateBuilder(services).UseDatabase(_client);

    // Act
    OpenIddictDynamoDbSetup.EnsureInitialized(services.BuildServiceProvider());

    // Assert
    var tableNames = await _client.ListTablesAsync();
    Assert.Contains(DatabaseFixture.TableName, tableNames.TableNames);
  }

  [Fact]
  public async Task Should_SetupTables_When_CalledAsynchronously()
  {
    // Arrange
    var options = TestUtils.GetOptions(new()
    {
      Database = _client,
    });

    // Act
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

    // Assert
    var tableNames = await _client.ListTablesAsync();
    Assert.Contains(DatabaseFixture.TableName, tableNames.TableNames);
  }

  [Fact]
  public async Task Should_SetupTables_When_CalledAsynchronouslyWithServiceProvider()
  {
    // Arrange
    var services = new ServiceCollection();
    CreateBuilder(services).UseDatabase(_client);

    // Act
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(services.BuildServiceProvider());

    // Assert
    var tableNames = await _client.ListTablesAsync();
    Assert.Contains(DatabaseFixture.TableName, tableNames.TableNames);
  }

  [Fact]
  public async Task Should_SetupTables_When_CalledAsynchronouslyWithDatbaseInServiceProvider()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddSingleton(_client);
    CreateBuilder(services);

    // Act
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(services.BuildServiceProvider());

    // Assert
    var tableNames = await _client.ListTablesAsync();
    Assert.Contains(DatabaseFixture.TableName, tableNames.TableNames);
  }

  [Fact]
  public async Task Should_SetupTables_When_CalledSynchronouslyWithDatbaseInServiceProvider()
  {
    // Arrange
    var services = new ServiceCollection();
    services.AddSingleton(_client);
    CreateBuilder(services);

    // Act
    OpenIddictDynamoDbSetup.EnsureInitialized(services.BuildServiceProvider());

    // Assert
    var tableNames = await _client.ListTablesAsync();
    Assert.Contains(DatabaseFixture.TableName, tableNames.TableNames);
  }

  [Fact]
  public async Task Should_ReplaceResourceIndex_When_TableHasTheLayoutFromBeforeScopeLookups()
  {
    // Arrange
    var tableName = Guid.NewGuid().ToString();
    var options = TestUtils.GetOptions(new()
    {
      Database = _client,
      DefaultTableName = tableName,
    });
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);
    await RemoveIndex(tableName, "ScopeId-index");
    await _client.UpdateTableAsync(new UpdateTableRequest
    {
      TableName = tableName,
      AttributeDefinitions = [new("ScopeResource", ScalarAttributeType.S)],
      GlobalSecondaryIndexUpdates =
      [
        new()
        {
          Create = new()
          {
            IndexName = "Resource-index",
            KeySchema = [new("ScopeResource", KeyType.HASH)],
            Projection = new() { ProjectionType = ProjectionType.ALL },
          },
        },
      ],
    });
    await WaitForActiveTable(tableName);

    try
    {
      // Act
      await Task.WhenAll(Enumerable.Range(0, 3)
        .Select(_ => OpenIddictDynamoDbSetup.EnsureInitializedAsync(options)));

      // Assert
      var table = await _client.DescribeTableAsync(tableName);
      Assert.DoesNotContain(table.Table.GlobalSecondaryIndexes, x => x.IndexName == "Resource-index");
      var index = Assert.Single(table.Table.GlobalSecondaryIndexes, x => x.IndexName == "ScopeId-index");
      Assert.Equal(IndexStatus.ACTIVE, index.IndexStatus);
    }
    finally
    {
      await _client.DeleteTableAsync(tableName);
    }
  }

  [Fact]
  public async Task Should_AddMissingIndex_When_TableIsProvisionedButOptionsAreNot()
  {
    // Arrange
    var tableName = Guid.NewGuid().ToString();
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(TestUtils.GetOptions(new()
    {
      Database = _client,
      DefaultTableName = tableName,
      BillingMode = BillingMode.PROVISIONED,
    }));
    await RemoveIndex(tableName, "ReferenceId-index");

    try
    {
      // Act
      await OpenIddictDynamoDbSetup.EnsureInitializedAsync(TestUtils.GetOptions(new()
      {
        Database = _client,
        DefaultTableName = tableName,
      }));

      // Assert
      var index = await GetIndex(tableName, "ReferenceId-index");
      Assert.Equal(IndexStatus.ACTIVE, index.IndexStatus);
      Assert.Equal(1, index.ProvisionedThroughput.ReadCapacityUnits);
      Assert.Equal(1, index.ProvisionedThroughput.WriteCapacityUnits);
    }
    finally
    {
      await _client.DeleteTableAsync(tableName);
    }
  }

  [Fact]
  public async Task Should_AddMissingIndex_When_TableIsOnDemandButOptionsAreProvisioned()
  {
    // Arrange
    var tableName = Guid.NewGuid().ToString();
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(TestUtils.GetOptions(new()
    {
      Database = _client,
      DefaultTableName = tableName,
    }));
    await RemoveIndex(tableName, "ReferenceId-index");

    try
    {
      // Act
      await OpenIddictDynamoDbSetup.EnsureInitializedAsync(TestUtils.GetOptions(new()
      {
        Database = _client,
        DefaultTableName = tableName,
        BillingMode = BillingMode.PROVISIONED,
      }));

      // Assert
      var index = await GetIndex(tableName, "ReferenceId-index");
      Assert.Equal(IndexStatus.ACTIVE, index.IndexStatus);
      Assert.Equal(0, index.ProvisionedThroughput?.ReadCapacityUnits ?? 0);
    }
    finally
    {
      await _client.DeleteTableAsync(tableName);
    }
  }

  [Fact]
  public async Task Should_CreateTable_When_SetupRunsConcurrently()
  {
    // Arrange
    var tableName = Guid.NewGuid().ToString();
    var options = TestUtils.GetOptions(new()
    {
      Database = _client,
      DefaultTableName = tableName,
    });

    try
    {
      // Act
      await Task.WhenAll(Enumerable.Range(0, 3)
        .Select(_ => OpenIddictDynamoDbSetup.EnsureInitializedAsync(options)));

      // Assert
      var table = await _client.DescribeTableAsync(tableName);
      Assert.Equal(TableStatus.ACTIVE, table.Table.TableStatus);
      Assert.Equal(8, table.Table.GlobalSecondaryIndexes.Count);
      var timeToLive = await _client.DescribeTimeToLiveAsync(tableName);
      Assert.Equal(TimeToLiveStatus.ENABLED, timeToLive.TimeToLiveDescription.TimeToLiveStatus);
      Assert.Equal("ttl", timeToLive.TimeToLiveDescription.AttributeName);
    }
    finally
    {
      await _client.DeleteTableAsync(tableName);
    }
  }

  [Fact]
  public async Task Should_UseExistingTable_When_ItIsNotOnTheFirstPageOfTables()
  {
    // Arrange, table names are listed in alphabetical order, 100 per page
    var prefix = $"zz-{Guid.NewGuid():N}";
    var tableName = $"{prefix}-z";
    var fillerTableNames = Enumerable.Range(0, 101).Select(x => $"{prefix}-{x:D3}").ToList();
    var options = TestUtils.GetOptions(new()
    {
      Database = _client,
      DefaultTableName = tableName,
    });

    try
    {
      await Task.WhenAll(fillerTableNames.Select(x => _client.CreateTableAsync(new CreateTableRequest
      {
        TableName = x,
        BillingMode = BillingMode.PAY_PER_REQUEST,
        KeySchema = [new("PartitionKey", KeyType.HASH)],
        AttributeDefinitions = [new("PartitionKey", ScalarAttributeType.S)],
      })));
      await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

      // Act
      await OpenIddictDynamoDbSetup.EnsureInitializedAsync(options);

      // Assert
      var table = await _client.DescribeTableAsync(tableName);
      Assert.Equal(TableStatus.ACTIVE, table.Table.TableStatus);
    }
    finally
    {
      await Task.WhenAll(fillerTableNames.Append(tableName).Select(async x =>
      {
        try
        {
          await _client.DeleteTableAsync(x);
        }
        catch (ResourceNotFoundException)
        {
        }
      }));
    }
  }

  [Fact]
  public async Task Should_LogProgress_When_CreatingTable()
  {
    // Arrange
    var tableName = Guid.NewGuid().ToString();
    var loggerProvider = new TestLoggerProvider();
    var services = new ServiceCollection();
    services.AddLogging(x => x.AddProvider(loggerProvider));
    services.AddOpenIddict().AddCore().UseDynamoDb().UseDatabase(_client).SetDefaultTableName(tableName);

    try
    {
      // Act
      await OpenIddictDynamoDbSetup.EnsureInitializedAsync(services.BuildServiceProvider());

      // Assert
      Assert.Contains(loggerProvider.Entries, x =>
        x.Level == LogLevel.Information && x.Message == $"Creating table {tableName}");
      Assert.Contains(loggerProvider.Entries, x =>
        x.Level == LogLevel.Information && x.Message == $"Enabling time to live on table {tableName}");
    }
    finally
    {
      await _client.DeleteTableAsync(tableName);
    }
  }

  [Fact]
  public async Task Should_OnlyVerifyThatTableExists_When_DescribeTableIsDenied()
  {
    // Arrange
    var database = new Mock<IAmazonDynamoDB>();
    database
      .Setup(x => x.DescribeTableAsync(It.IsAny<DescribeTableRequest>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new AmazonDynamoDBException("Access denied") { ErrorCode = "AccessDeniedException" });
    database
      .Setup(x => x.DescribeTimeToLiveAsync(It.IsAny<DescribeTimeToLiveRequest>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new AmazonDynamoDBException("Access denied") { ErrorCode = "AccessDeniedException" });
    database
      .Setup(x => x.ListTablesAsync(
        It.Is<ListTablesRequest>(y => y.ExclusiveStartTableName == null), It.IsAny<CancellationToken>()))
      .ReturnsAsync(new ListTablesResponse { TableNames = ["another-table"], LastEvaluatedTableName = "another-table" });
    database
      .Setup(x => x.ListTablesAsync(
        It.Is<ListTablesRequest>(y => y.ExclusiveStartTableName == "another-table"), It.IsAny<CancellationToken>()))
      .ReturnsAsync(new ListTablesResponse { TableNames = [DatabaseFixture.TableName] });
    var loggerProvider = new TestLoggerProvider();

    // Act
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(CreateServiceProvider(database.Object, loggerProvider));

    // Assert
    database.Verify(x => x.CreateTableAsync(It.IsAny<CreateTableRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    database.Verify(x => x.UpdateTableAsync(It.IsAny<UpdateTableRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    Assert.Contains(loggerProvider.Entries, x =>
      x.Level == LogLevel.Warning && x.Message.Contains($"Unable to describe table {DatabaseFixture.TableName}"));
    Assert.Contains(loggerProvider.Entries, x =>
      x.Level == LogLevel.Warning && x.Message.Contains($"Unable to enable time to live on table {DatabaseFixture.TableName}"));
  }

  [Fact]
  public async Task Should_LogWarning_When_UpdateTableIsDenied()
  {
    // Arrange
    var database = new Mock<IAmazonDynamoDB>();
    database
      .Setup(x => x.DescribeTableAsync(It.IsAny<DescribeTableRequest>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(CreateDescribeTableResponse(TableStatus.ACTIVE, IndexNames.Where(x => x != "ScopeId-index")));
    database
      .Setup(x => x.UpdateTableAsync(It.IsAny<UpdateTableRequest>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new AmazonDynamoDBException("Access denied") { ErrorCode = "AccessDeniedException" });
    SetupTimeToLive(database);
    var loggerProvider = new TestLoggerProvider();

    // Act
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(CreateServiceProvider(database.Object, loggerProvider));

    // Assert
    database.Verify(x => x.UpdateTableAsync(
      It.Is<UpdateTableRequest>(y => y.GlobalSecondaryIndexUpdates[0].Create.IndexName == "ScopeId-index"),
      It.IsAny<CancellationToken>()), Times.Once);
    Assert.Contains(loggerProvider.Entries, x =>
      x.Level == LogLevel.Warning && x.Message.Contains("Unable to add global secondary index ScopeId-index"));
  }

  [Fact]
  public async Task Should_WaitForTable_When_ItIsBeingCreatedElsewhere()
  {
    // Arrange
    var database = new Mock<IAmazonDynamoDB>();
    database
      .SetupSequence(x => x.DescribeTableAsync(It.IsAny<DescribeTableRequest>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(CreateDescribeTableResponse(TableStatus.CREATING, IndexNames))
      .ReturnsAsync(CreateDescribeTableResponse(TableStatus.CREATING, IndexNames))
      .ReturnsAsync(CreateDescribeTableResponse(TableStatus.ACTIVE, IndexNames))
      .ReturnsAsync(CreateDescribeTableResponse(TableStatus.ACTIVE, IndexNames))
      .ReturnsAsync(CreateDescribeTableResponse(TableStatus.ACTIVE, IndexNames));
    SetupTimeToLive(database);

    // Act
    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(
      CreateServiceProvider(database.Object, new TestLoggerProvider()));

    // Assert
    database.Verify(x => x.CreateTableAsync(It.IsAny<CreateTableRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    database.Verify(x => x.UpdateTableAsync(It.IsAny<UpdateTableRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    database.Verify(x => x.DescribeTableAsync(It.IsAny<DescribeTableRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(5));
  }

  private static readonly string[] IndexNames =
  [
    "ApplicationId-index",
    "Subject-index",
    "ClientId-index",
    "RedirectUri-RedirectType-index",
    "Name-index",
    "ScopeId-index",
    "AuthorizationId-index",
    "ReferenceId-index",
  ];

  private static DescribeTableResponse CreateDescribeTableResponse(
    TableStatus tableStatus, IEnumerable<string> indexNames) => new()
    {
      Table = new()
      {
        TableName = DatabaseFixture.TableName,
        TableStatus = tableStatus,
        BillingModeSummary = new() { BillingMode = BillingMode.PAY_PER_REQUEST },
        GlobalSecondaryIndexes = indexNames.Select(x => new GlobalSecondaryIndexDescription
        {
          IndexName = x,
          IndexStatus = Equals(tableStatus, TableStatus.ACTIVE) ? IndexStatus.ACTIVE : IndexStatus.CREATING,
        }).ToList(),
      },
    };

  private static void SetupTimeToLive(Mock<IAmazonDynamoDB> database) => database
    .Setup(x => x.DescribeTimeToLiveAsync(It.IsAny<DescribeTimeToLiveRequest>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(new DescribeTimeToLiveResponse
    {
      TimeToLiveDescription = new() { TimeToLiveStatus = TimeToLiveStatus.ENABLED },
    });

  private static IServiceProvider CreateServiceProvider(IAmazonDynamoDB database, ILoggerProvider loggerProvider)
  {
    var services = new ServiceCollection();
    services.AddLogging(x => x.AddProvider(loggerProvider));
    CreateBuilder(services).UseDatabase(database);
    return services.BuildServiceProvider();
  }

  private async Task RemoveIndex(string tableName, string indexName)
  {
    await _client.UpdateTableAsync(new UpdateTableRequest
    {
      TableName = tableName,
      GlobalSecondaryIndexUpdates = [new() { Delete = new() { IndexName = indexName } }],
    });
    await WaitForActiveTable(tableName);
  }

  private async Task WaitForActiveTable(string tableName)
  {
    for (var attempt = 0; attempt < 30; attempt++)
    {
      var table = await _client.DescribeTableAsync(tableName);
      if (Equals(table.Table.TableStatus, TableStatus.ACTIVE)
        && table.Table.GlobalSecondaryIndexes.TrueForAll(x => Equals(x.IndexStatus, IndexStatus.ACTIVE)))
      {
        return;
      }

      await Task.Delay(TimeSpan.FromSeconds(1));
    }

    throw new TimeoutException($"Table {tableName} did not become active");
  }

  private async Task<GlobalSecondaryIndexDescription> GetIndex(string tableName, string indexName)
  {
    var table = await _client.DescribeTableAsync(tableName);
    return Assert.Single(table.Table.GlobalSecondaryIndexes, x => x.IndexName == indexName);
  }

  private static OpenIddictDynamoDbBuilder CreateBuilder(IServiceCollection services)
    => services.AddOpenIddict().AddCore().UseDynamoDb().SetDefaultTableName(DatabaseFixture.TableName);
}
