using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OpenIddict.AmazonDynamoDB;

public class OpenIddictDynamoDbAuthorizationStore<TAuthorization> : IOpenIddictAuthorizationStore<TAuthorization>
    where TAuthorization : OpenIddictDynamoDbAuthorization, new()
{
  private readonly IAmazonDynamoDB _client;
  private readonly string _tableName;
  private const string PartitionKeyPrefix = "AUTHORIZATION#";
  private const string SortKeyPrefix = "#AUTHORIZATION#";

  public OpenIddictDynamoDbAuthorizationStore(
    IOptionsMonitor<OpenIddictDynamoDbOptions> optionsMonitor,
    IAmazonDynamoDB? database = default)
  {
    ArgumentNullException.ThrowIfNull(optionsMonitor);

    var options = optionsMonitor.CurrentValue;

    if (database == default)
    {
      ArgumentNullException.ThrowIfNull(options.Database);
    }

    _client = database ?? options.Database!;
    _tableName = options.DefaultTableName ?? Constants.DefaultTableName;
  }

  public async ValueTask<long> CountAsync(CancellationToken cancellationToken)
    => await DynamoDbUtils.GetCountAsync(_client, _tableName, CountType.Authorization, cancellationToken);

  public ValueTask<long> CountAsync<TResult>(Func<IQueryable<TAuthorization>, IQueryable<TResult>> query, CancellationToken cancellationToken)
  {
    throw new NotSupportedException();
  }

  public async ValueTask CreateAsync(TAuthorization authorization, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    await _client.PutItemAsync(new PutItemRequest
    {
      TableName = _tableName,
      Item = DynamoDbItems.ToItem(authorization),
    }, cancellationToken);

    await DynamoDbUtils.UpdateCountAsync(_client, _tableName, CountType.Authorization, 1, cancellationToken);
  }

  public async ValueTask DeleteAsync(TAuthorization authorization, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    await _client.DeleteItemAsync(new DeleteItemRequest
    {
      TableName = _tableName,
      Key = DynamoDbItems.Key(authorization),
    }, cancellationToken);

    await DynamoDbUtils.UpdateCountAsync(_client, _tableName, CountType.Authorization, -1, cancellationToken);
  }

  private IAsyncEnumerable<TAuthorization> FindBySubjectAndSearchKey(string subject, string searchKey, CancellationToken cancellationToken)
  {
    return ExecuteAsync(cancellationToken);

    async IAsyncEnumerable<TAuthorization> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
      var authorizations = await DynamoDbItems.QueryAsync<TAuthorization>(_client, new QueryRequest
      {
        TableName = _tableName,
        IndexName = "Subject-index",
        KeyConditionExpression = "Subject = :subject and begins_with(SearchKey, :searchKey)",
        // Tokens are also stored in the Subject-index
        FilterExpression = DynamoDbUtils.PartitionKeyPrefixFilter,
        ExpressionAttributeValues = new()
        {
          { ":subject", new AttributeValue { S = subject } },
          { ":searchKey", new AttributeValue { S = searchKey } },
          { DynamoDbUtils.PartitionKeyPrefixValue, new AttributeValue { S = PartitionKeyPrefix } },
        },
      }, cancellationToken);

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
      var authorizations = await DynamoDbItems.QueryAsync<TAuthorization>(_client, new QueryRequest
      {
        TableName = _tableName,
        IndexName = "ApplicationId-index",
        // Tokens and redirects are also stored in the ApplicationId-index
        KeyConditionExpression = "ApplicationId = :applicationId and begins_with(SortKey, :sortKey)",
        ExpressionAttributeValues = new()
        {
          { ":applicationId", new AttributeValue { S = identifier } },
          { ":sortKey", new AttributeValue { S = SortKeyPrefix } },
        },
      }, cancellationToken);

      foreach (var authorization in authorizations)
      {
        yield return authorization;
      }
    }
  }

  public async ValueTask<TAuthorization?> FindByIdAsync(string identifier, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(identifier);

    var authorization = new TAuthorization { Id = identifier };
    // A stale authorization would fail to update, and could still look valid after being revoked
    return await DynamoDbItems.GetAsync<TAuthorization>(
      _client, _tableName, authorization.PartitionKey, authorization.SortKey, cancellationToken, consistentRead: true);
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
    => new(new TAuthorization());

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
    var authorizations = await DynamoDbItems.ScanAsync<TAuthorization>(_client, new ScanRequest
    {
      TableName = _tableName,
      FilterExpression = "begins_with(PartitionKey, :partitionKey) and CreationDate < :threshold",
      ExpressionAttributeValues = new()
      {
        { ":partitionKey", new AttributeValue { S = PartitionKeyPrefix } },
        { ":threshold", new AttributeValue { S = AttributeValues.ToDateString(threshold.UtcDateTime) } },
      },
    }, cancellationToken);
    var remainingAdHocAuthorizations = new List<TAuthorization>();
    var toBeDeleted = new List<WriteRequest>();

    foreach (var authorization in authorizations)
    {
      // Add authorizations which is not Valid
      if (authorization.Status != Statuses.Valid)
      {
        toBeDeleted.Add(DynamoDbItems.Delete(authorization));
        deleteCount++;
      }
      else if (authorization.Type == AuthorizationTypes.AdHoc)
      {
        remainingAdHocAuthorizations.Add(authorization);
      }
    }

    // Add authorizations which is ad hoc and has no tokens, which are listed in the authorization's partition
    foreach (var authorization in remainingAdHocAuthorizations)
    {
      var members = await DynamoDbItems.QueryPartitionAsync<MemberItem>(
        _client, _tableName, authorization.PartitionKey, Lookups.TokenMemberPrefix, cancellationToken, consistentRead: true);
      var tokens = await DynamoDbItems.BatchGetAsync<OpenIddictDynamoDbToken>(
        _client, _tableName, members.Select(x => new OpenIddictDynamoDbToken { Id = x.Id }).Select(DynamoDbItems.Key), cancellationToken);

      if (tokens.Count == 0)
      {
        toBeDeleted.Add(DynamoDbItems.Delete(authorization));
        deleteCount++;
      }
    }

    await DynamoDbUtils.BatchWriteAsync(_client, _tableName, toBeDeleted, cancellationToken);

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
      await DynamoDbItems.PutIfUnchangedAsync(
        _client, _tableName, DynamoDbItems.ToItem(authorization), "ConcurrencyToken", concurrencyToken, cancellationToken);
    }
    catch (ConditionalCheckFailedException exception)
    {
      authorization.ConcurrencyToken = concurrencyToken;
      authorization.TTL = ttl;

      throw new OpenIddictExceptions.ConcurrencyException(
        OpenIddictResources.GetResourceString(OpenIddictResources.ID0241), exception);
    }
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
    var requests = new List<WriteRequest>();

    await foreach (var authorization in authorizations)
    {
      authorization.Status = Statuses.Revoked;
      authorization.TTL = DateTime.UtcNow.AddMinutes(5);

      requests.Add(DynamoDbItems.Put(authorization));
    }

    await DynamoDbUtils.BatchWriteAsync(_client, _tableName, requests, cancellationToken);

    return requests.Count;
  }
}
