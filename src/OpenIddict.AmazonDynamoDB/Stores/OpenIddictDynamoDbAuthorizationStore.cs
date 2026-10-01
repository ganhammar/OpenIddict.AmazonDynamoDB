using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OpenIddict.AmazonDynamoDB;

public class OpenIddictDynamoDbAuthorizationStore<TAuthorization> : IOpenIddictAuthorizationStore<TAuthorization>
    where TAuthorization : OpenIddictDynamoDbAuthorization, new()
{
  private readonly IAmazonDynamoDB _client;
  private readonly DynamoDBContext _context;
  private readonly string _tableName;
  private const string PartitionKeyPrefix = "AUTHORIZATION#";
  private const string SortKeyPrefix = "#AUTHORIZATION#";

  public OpenIddictDynamoDbAuthorizationStore(
    IOptionsMonitor<OpenIddictDynamoDbOptions> optionsMonitor,
    IAmazonDynamoDB? database = default)
  {
    ArgumentNullException.ThrowIfNull(optionsMonitor);

    var options = optionsMonitor.CurrentValue;
    DynamoDbTableSetup.EnsureAliasCreated(options);

    if (database == default)
    {
      ArgumentNullException.ThrowIfNull(options.Database);
    }

    _client = database ?? options.Database!;
    _context = new DynamoDBContextBuilder()
      .WithDynamoDBClient(() => _client)
      .Build();
    _tableName = options.DefaultTableName ?? Constants.DefaultTableName;
  }

  public async ValueTask<long> CountAsync(CancellationToken cancellationToken)
  {
    var count = new CountModel(CountType.Authorization);
    count = await _context.LoadAsync<CountModel>(count.PartitionKey, count.SortKey, cancellationToken);

    return count?.Count ?? 0;
  }

  public ValueTask<long> CountAsync<TResult>(Func<IQueryable<TAuthorization>, IQueryable<TResult>> query, CancellationToken cancellationToken)
  {
    throw new NotSupportedException();
  }

  public async ValueTask CreateAsync(TAuthorization authorization, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    await _context.SaveAsync(authorization, cancellationToken);

    await DynamoDbUtils.UpdateCountAsync(_client, _tableName, CountType.Authorization, 1, cancellationToken);
  }

  public async ValueTask DeleteAsync(TAuthorization authorization, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    await _context.DeleteAsync(authorization, cancellationToken);

    await DynamoDbUtils.UpdateCountAsync(_client, _tableName, CountType.Authorization, -1, cancellationToken);
  }

  private IAsyncEnumerable<TAuthorization> FindBySubjectAndSearchKey(string subject, string searchKey, CancellationToken cancellationToken)
  {
    return ExecuteAsync(cancellationToken);

    async IAsyncEnumerable<TAuthorization> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
      var search = _context.FromQueryAsync<TAuthorization>(new()
      {
        IndexName = "Subject-index",
        KeyExpression = new()
        {
          ExpressionStatement = "Subject = :subject and begins_with(SearchKey, :searchKey)",
          ExpressionAttributeValues = new()
          {
            { ":subject", subject },
            { ":searchKey", searchKey },
          }
        },
        // Tokens are also stored in the Subject-index
        FilterExpression = DynamoDbUtils.GetPartitionKeyFilter(PartitionKeyPrefix),
      });

      var authorizations = await search.GetRemainingAsync(cancellationToken);

      foreach (var authorization in authorizations)
      {
        yield return authorization;
      }
    }
  }

  public IAsyncEnumerable<TAuthorization> FindAsync(
    string? subject, string? client,
    string? status, string? type,
    ImmutableArray<string>? scopes, CancellationToken cancellationToken)
  {
    return ExecuteAsync(cancellationToken);

    async IAsyncEnumerable<TAuthorization> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
      // Query by the most selective parameter, the search key prefix can only be used for
      // leading parameters, so the parameters are always matched exactly afterwards
      var authorizations = string.IsNullOrEmpty(subject) == false
        ? FindBySubjectAndSearchKey(subject, DynamoDbUtils.GetSearchKeyPrefix(client, status, type), cancellationToken)
        : string.IsNullOrEmpty(client) == false
          ? FindByApplicationIdAsync(client, cancellationToken)
          : ListAsync(null, null, cancellationToken);

      await foreach (var authorization in authorizations)
      {
        if (DynamoDbUtils.IsMatch(authorization.ApplicationId, client)
          && DynamoDbUtils.IsMatch(authorization.Status, status)
          && DynamoDbUtils.IsMatch(authorization.Type, type)
          && (scopes == null || scopes.Value.All(x => authorization.Scopes?.Contains(x) == true)))
        {
          yield return authorization;
        }
      }
    }
  }

  public IAsyncEnumerable<TAuthorization> FindByApplicationIdAsync(string identifier, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(identifier);

    return ExecuteAsync(cancellationToken);

    async IAsyncEnumerable<TAuthorization> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
      var search = _context.FromQueryAsync<TAuthorization>(new()
      {
        IndexName = "ApplicationId-index",
        KeyExpression = new()
        {
          // Tokens and redirects are also stored in the ApplicationId-index
          ExpressionStatement = "ApplicationId = :applicationId and begins_with(SortKey, :sortKey)",
          ExpressionAttributeValues = new()
          {
            { ":applicationId", identifier },
            { ":sortKey", SortKeyPrefix },
          }
        },
      });

      var authorizations = await search.GetRemainingAsync(cancellationToken);

      foreach (var authorization in authorizations)
      {
        yield return authorization;
      }
    }
  }

  public async ValueTask<TAuthorization?> FindByIdAsync(string identifier, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(identifier);

    return await GetByPartitionKey(new() { Id = identifier }, cancellationToken);
  }

  public IAsyncEnumerable<TAuthorization> FindBySubjectAsync(string subject, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(subject);

    return FindBySubjectAndSearchKey(subject, DynamoDbUtils.GetSearchKeyPrefix(null, null, null), cancellationToken);
  }

  public ValueTask<string?> GetApplicationIdAsync(TAuthorization authorization, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    return new(authorization.ApplicationId);
  }

  public ValueTask<TResult?> GetAsync<TState, TResult>(Func<IQueryable<TAuthorization>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
  {
    throw new NotSupportedException();
  }

  public ValueTask<DateTimeOffset?> GetCreationDateAsync(TAuthorization authorization, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    return new(authorization.CreationDate);
  }

  public ValueTask<string?> GetIdAsync(TAuthorization authorization, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    return new(authorization.Id);
  }

  public ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(TAuthorization authorization, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    if (string.IsNullOrEmpty(authorization.Properties))
    {
      return new(ImmutableDictionary.Create<string, JsonElement>());
    }

    using var document = JsonDocument.Parse(authorization.Properties);
    var properties = ImmutableDictionary.CreateBuilder<string, JsonElement>();

    foreach (var property in document.RootElement.EnumerateObject())
    {
      properties[property.Name] = property.Value.Clone();
    }

    return new(properties.ToImmutable());
  }

  public ValueTask<ImmutableArray<string>> GetScopesAsync(
    TAuthorization authorization, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    if (authorization.Scopes is not { Count: > 0 })
    {
      return new([]);
    }

    return new(authorization.Scopes.ToImmutableArray());
  }

  public ValueTask<string?> GetStatusAsync(
    TAuthorization authorization, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    return new(authorization.Status);
  }

  public ValueTask<string?> GetSubjectAsync(
    TAuthorization authorization, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    return new(authorization.Subject);
  }

  public ValueTask<string?> GetTypeAsync(
    TAuthorization authorization, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    return new(authorization.Type);
  }

  public ValueTask<TAuthorization> InstantiateAsync(
    CancellationToken cancellationToken)
  {
    try
    {
      return new(Activator.CreateInstance<TAuthorization>());
    }
    catch (MemberAccessException exception)
    {
      return new(Task.FromException<TAuthorization>(
          new InvalidOperationException(OpenIddictResources
            .GetResourceString(OpenIddictResources.ID0240), exception)));
    }
  }

  public ConcurrentDictionary<int, string?> ListCursors { get; set; }
    = new ConcurrentDictionary<int, string?>();
  public IAsyncEnumerable<TAuthorization> ListAsync(
    int? count, int? offset, CancellationToken cancellationToken)
  {
    string? initalToken = default;
    if (offset.HasValue)
    {
      ListCursors.TryGetValue(offset.Value, out initalToken);

      if (initalToken == default)
      {
        throw new NotSupportedException("Pagination support is very limited (see documentation)");
      }
    }

    return ExecuteAsync(cancellationToken);

    async IAsyncEnumerable<TAuthorization> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
      var (token, items) = await DynamoDbUtils.Paginate<TAuthorization>(
        _client, _tableName, PartitionKeyPrefix, SortKeyPrefix, count, initalToken, cancellationToken);

      if (count.HasValue)
      {
        ListCursors.TryAdd(count.Value + (offset ?? 0), token);
      }

      foreach (var item in items)
      {
        yield return item;
      }
    }
  }

  public IAsyncEnumerable<TResult> ListAsync<TState, TResult>(
    Func<IQueryable<TAuthorization>, TState, IQueryable<TResult>> query,
    TState state,
    CancellationToken cancellationToken)
  {
    throw new NotSupportedException();
  }

  // Should not be needed to run, TTL should handle the pruning
  public async ValueTask<long> PruneAsync(DateTimeOffset threshold, CancellationToken cancellationToken)
  {
    var deleteCount = 0;
    // Get all authorizations which is older than threshold
    var filter = new ScanFilter();
    filter.AddCondition("CreationDate", ScanOperator.LessThan, new List<AttributeValue>
    {
      new(threshold.UtcDateTime.ToString("o")),
    });
    var search = _context.FromScanAsync<TAuthorization>(new ScanOperationConfig
    {
      Filter = filter,
    });
    var authorizations = await search.GetRemainingAsync(cancellationToken);
    var remainingAdHocAuthorizations = new List<TAuthorization>();

    var batchDelete = _context.CreateBatchWrite<TAuthorization>();

    foreach (var authorization in authorizations)
    {
      // Add authorizations which is not Valid
      if (authorization.Status != Statuses.Valid)
      {
        batchDelete.AddDeleteItem(authorization);
        deleteCount++;
      }
      else if (authorization.Type == AuthorizationTypes.AdHoc)
      {
        remainingAdHocAuthorizations.Add(authorization);
      }
    }

    // Add authorizations which is ad hoc and has no tokens
    foreach (var authorization in remainingAdHocAuthorizations)
    {
      var tokensQuery = _context.FromQueryAsync<OpenIddictDynamoDbToken>(new()
      {
        IndexName = "AuthorizationId-index",
        KeyExpression = new()
        {
          ExpressionStatement = "AuthorizationId = :authorizationId",
          ExpressionAttributeValues = new()
          {
            { ":authorizationId", authorization.Id },
          }
        },
      });
      var tokens = await tokensQuery.GetRemainingAsync(cancellationToken);

      if (tokens.Count != 0 == false)
      {
        batchDelete.AddDeleteItem(authorization);
        deleteCount++;
      }
    }

    await batchDelete.ExecuteAsync(cancellationToken);

    await DynamoDbUtils.UpdateCountAsync(_client, _tableName, CountType.Authorization, -deleteCount, cancellationToken);

    return deleteCount;
  }

  public ValueTask SetApplicationIdAsync(TAuthorization authorization, string? identifier, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    authorization.ApplicationId = identifier;

    return default;
  }

  public ValueTask SetCreationDateAsync(
    TAuthorization authorization, DateTimeOffset? date, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    authorization.CreationDate = date?.UtcDateTime;

    return default;
  }

  public ValueTask SetPropertiesAsync(
    TAuthorization authorization,
    ImmutableDictionary<string, JsonElement> properties,
    CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    if (properties is not { Count: > 0 })
    {
      authorization.Properties = null;

      return default;
    }

    using var stream = new MemoryStream();
    using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
    {
      Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
      Indented = false
    });

    writer.WriteStartObject();

    foreach (var property in properties)
    {
      writer.WritePropertyName(property.Key);
      property.Value.WriteTo(writer);
    }

    writer.WriteEndObject();
    writer.Flush();

    authorization.Properties = Encoding.UTF8.GetString(stream.ToArray());

    return default;
  }

  public ValueTask SetScopesAsync(
    TAuthorization authorization, ImmutableArray<string> scopes, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    if (scopes.IsDefaultOrEmpty)
    {
      authorization.Scopes = null;

      return default;
    }

    authorization.Scopes = [.. scopes];

    return default;
  }

  public ValueTask SetStatusAsync(
    TAuthorization authorization, string? status, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    authorization.Status = status;

    return default;
  }

  public ValueTask SetSubjectAsync(
    TAuthorization authorization, string? subject, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    authorization.Subject = subject;

    return default;
  }

  public ValueTask SetTypeAsync(
    TAuthorization authorization, string? type, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    authorization.Type = type;

    return default;
  }

  public async ValueTask UpdateAsync(
    TAuthorization authorization, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    var concurrencyToken = authorization.ConcurrencyToken;
    var ttl = authorization.TTL;
    authorization.ConcurrencyToken = Guid.NewGuid().ToString();

    if (authorization.Status != Statuses.Valid)
    {
      authorization.TTL = DateTime.UtcNow.AddMinutes(5);
    }

    try
    {
      // Ensure no one else has updated the authorization since it was loaded
      await _context.SaveAsync(authorization, GetSaveConfig(concurrencyToken), cancellationToken);
    }
    catch (ConditionalCheckFailedException exception)
    {
      authorization.ConcurrencyToken = concurrencyToken;
      authorization.TTL = ttl;

      throw new OpenIddictExceptions.ConcurrencyException(
        OpenIddictResources.GetResourceString(OpenIddictResources.ID0241), exception);
    }
  }

  private async Task<TAuthorization?> GetByPartitionKey(TAuthorization token, CancellationToken cancellationToken)
  {
    var search = _context.FromQueryAsync<TAuthorization>(new()
    {
      // A stale authorization would fail to update, and could still look valid after being revoked
      ConsistentRead = true,
      KeyExpression = new()
      {
        ExpressionStatement = "PartitionKey = :partitionKey",
        ExpressionAttributeValues = new()
        {
          { ":partitionKey", token.PartitionKey },
        }
      },
      Limit = 1,
    });
    var result = await search.GetNextSetAsync(cancellationToken);

    return result.Count != 0 ? result.First() : default;
  }

  public ValueTask<long> RevokeAsync(string? subject, string? client, string? status, string? type, CancellationToken cancellationToken)
  {
    var authorizations = FindAsync(subject, client, status, type, null, cancellationToken);
    return RevokeAsync(authorizations, cancellationToken);
  }

  public ValueTask<long> RevokeByApplicationIdAsync(string identifier, CancellationToken cancellationToken)
  {
    var authorizations = FindByApplicationIdAsync(identifier, cancellationToken);
    return RevokeAsync(authorizations, cancellationToken);
  }

  public ValueTask<long> RevokeBySubjectAsync(string subject, CancellationToken cancellationToken)
  {
    var authorizations = FindBySubjectAsync(subject, cancellationToken);
    return RevokeAsync(authorizations, cancellationToken);
  }

  private async ValueTask<long> RevokeAsync(IAsyncEnumerable<TAuthorization> authorizations, CancellationToken cancellationToken)
  {
    var result = 0L;
    var batch = _context.CreateBatchWrite<TAuthorization>();

    await foreach (var authorization in authorizations)
    {
      authorization.Status = Statuses.Revoked;
      authorization.TTL = DateTime.UtcNow.AddMinutes(5);

      batch.AddPutItem(authorization);
      result++;
    }

    await batch.ExecuteAsync(cancellationToken);

    return result;
  }

  private static SaveConfig GetSaveConfig(string? concurrencyToken)
  {
    // Only save when the authorization exists and still has the concurrency token it was loaded with
    var condition = new ContextExpression();

    if (concurrencyToken == default)
    {
      condition.SetFilter<TAuthorization>(x => ContextExpression.AttributeExists(x.PartitionKey)
        && ContextExpression.AttributeNotExists(x.ConcurrencyToken));
    }
    else
    {
      condition.SetFilter<TAuthorization>(x => ContextExpression.AttributeExists(x.PartitionKey)
        && x.ConcurrencyToken == concurrencyToken);
    }

    return new()
    {
      ConditionalExpression = condition,
    };
  }
}
