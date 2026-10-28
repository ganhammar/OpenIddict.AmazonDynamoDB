using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Globalization;
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

public class OpenIddictDynamoDbTokenStore<TToken> : IOpenIddictTokenStore<TToken>
    where TToken : OpenIddictDynamoDbToken, new()
{
  private readonly IAmazonDynamoDB _client;
  private readonly string _tableName;
  private const string PartitionKeyPrefix = "TOKEN#";
  private const string SortKeyPrefix = "#TOKEN#";

  public OpenIddictDynamoDbTokenStore(
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
    => await DynamoDbUtils.GetCountAsync(_client, _tableName, CountType.Token, cancellationToken);

  public ValueTask<long> CountAsync<TResult>(Func<IQueryable<TToken>, IQueryable<TResult>> query, CancellationToken cancellationToken)
  {
    throw new NotSupportedException();
  }

  // The token is written together with the lookup of its reference id, so a reference id that
  // another token holds fails the whole write, and with its listing under its authorization
  public async ValueTask CreateAsync(TToken token, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    if (token.ExpirationDate != default)
    {
      token.TTL = token.ExpirationDate;
    }

    var items = new List<TransactWriteItem>
    {
      DynamoDbItems.TransactPut(_tableName, token, "attribute_not_exists(PartitionKey)"),
    };
    var lookupIndex = -1;

    if (token.ReferenceId is { } referenceId)
    {
      lookupIndex = items.Count;
      items.Add(DynamoDbItems.TransactPut(
        _tableName, new LookupItem(Lookups.ReferenceId(referenceId), token.Id, token.TTL), "attribute_not_exists(PartitionKey)"));
    }

    if (AuthorizationMember(token) is { } member)
    {
      items.Add(DynamoDbItems.TransactPut(_tableName, member));
    }

    try
    {
      await DynamoDbItems.TransactWriteAsync(_client, items, cancellationToken);
    }
    catch (TransactionCanceledException exception)
    {
      throw DynamoDbItems.FailedConditions(exception).Contains(lookupIndex)
        ? DuplicateReferenceId(exception)
        : new InvalidOperationException($"A token with the id {token.Id} already exists", exception);
    }

    token.StoredReferenceId = token.ReferenceId;
    token.StoredAuthorizationId = token.AuthorizationId;

    await DynamoDbUtils.UpdateCountAsync(_client, _tableName, CountType.Token, 1, cancellationToken);
    await ExtendAuthorizationLifetime(token, cancellationToken);
  }

  private static OpenIddictExceptions.ValidationException DuplicateReferenceId(Exception exception)
  {
    var message = OpenIddictResources.GetResourceString(OpenIddictResources.ID2085);
    return new OpenIddictExceptions.ValidationException(message, [new System.ComponentModel.DataAnnotations.ValidationResult(message)], exception);
  }

  // The token's listing in its authorization's partition, which expires with the token
  private static MemberItem? AuthorizationMember(TToken token) => token.AuthorizationId is { } authorizationId
    ? new MemberItem(new OpenIddictDynamoDbAuthorization { Id = authorizationId }.PartitionKey, Lookups.TokenMember(token.Id), token.Id, token.TTL)
    : default;

  // The lookup and listing items of a token, as they are stored now
  private static IEnumerable<IDynamoDbItem> LookupsOf(TToken token)
  {
    if (token.ReferenceId is { } referenceId)
    {
      yield return new LookupItem(Lookups.ReferenceId(referenceId), token.Id, token.TTL);
    }

    if (AuthorizationMember(token) is { } member)
    {
      yield return member;
    }
  }

  // Removes a reference id lookup that points at the token, or is already gone
  private TransactWriteItem DeleteReferenceLookup(string referenceId, string tokenId) => DynamoDbItems.TransactDelete(
    _tableName, Lookups.ReferenceId(referenceId), Lookups.SortKey,
    "attribute_not_exists(PartitionKey) or Id = :id", new() { { ":id", new AttributeValue { S = tokenId } } });

  public async ValueTask DeleteAsync(TToken token, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    var items = new List<TransactWriteItem>
    {
      DynamoDbItems.TransactDelete(_tableName, token.PartitionKey, token.SortKey),
    };

    if ((token.StoredReferenceId ?? token.ReferenceId) is { } referenceId)
    {
      items.Add(DeleteReferenceLookup(referenceId, token.Id));
    }

    if ((token.StoredAuthorizationId ?? token.AuthorizationId) is { } authorizationId)
    {
      items.Add(DynamoDbItems.TransactDelete(
        _tableName, new OpenIddictDynamoDbAuthorization { Id = authorizationId }.PartitionKey, Lookups.TokenMember(token.Id)));
    }

    await DynamoDbItems.TransactWriteAsync(_client, items, cancellationToken);

    await DynamoDbUtils.UpdateCountAsync(_client, _tableName, CountType.Token, -1, cancellationToken);
  }

  private IAsyncEnumerable<TToken> FindBySubjectAndSearchKey(string subject, string searchKey, CancellationToken cancellationToken)
  {
    return ExecuteAsync(cancellationToken);

    async IAsyncEnumerable<TToken> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
      var tokens = await DynamoDbItems.QueryAsync<TToken>(_client, new QueryRequest
      {
        TableName = _tableName,
        IndexName = "Subject-index",
        KeyConditionExpression = "Subject = :subject and begins_with(SearchKey, :searchKey)",
        // Authorizations are also stored in the Subject-index
        FilterExpression = DynamoDbUtils.PartitionKeyPrefixFilter,
        ExpressionAttributeValues = new()
        {
          { ":subject", new AttributeValue { S = subject } },
          { ":searchKey", new AttributeValue { S = searchKey } },
          { DynamoDbUtils.PartitionKeyPrefixValue, new AttributeValue { S = PartitionKeyPrefix } },
        },
      }, cancellationToken);

      foreach (var token in tokens)
      {
        yield return token;
      }
    }
  }

  public IAsyncEnumerable<TToken> FindAsync(
    string? subject, string? client,
    string? status, string? type, CancellationToken cancellationToken)
  {
    return ExecuteAsync(cancellationToken);

    async IAsyncEnumerable<TToken> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
      // Query by the most selective parameter, the search key prefix can only be used for
      // leading parameters, so the parameters are always matched exactly afterwards
      var tokens = string.IsNullOrEmpty(subject) == false
        ? FindBySubjectAndSearchKey(subject, DynamoDbUtils.GetSearchKeyPrefix(client, status, type), cancellationToken)
        : string.IsNullOrEmpty(client) == false
          ? FindByApplicationIdAsync(client, cancellationToken)
          : ListAsync(null, null, cancellationToken);

      await foreach (var token in tokens)
      {
        if (DynamoDbUtils.IsMatch(token.ApplicationId, client)
          && DynamoDbUtils.IsMatch(token.Status, status)
          && DynamoDbUtils.IsMatch(token.Type, type))
        {
          yield return token;
        }
      }
    }
  }

  public IAsyncEnumerable<TToken> FindByApplicationIdAsync(string identifier, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(identifier);

    return ExecuteAsync(cancellationToken);

    async IAsyncEnumerable<TToken> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
      var tokens = await DynamoDbItems.QueryAsync<TToken>(_client, new QueryRequest
      {
        TableName = _tableName,
        IndexName = "ApplicationId-index",
        // Authorizations and redirects are also stored in the ApplicationId-index
        KeyConditionExpression = "ApplicationId = :applicationId and begins_with(SortKey, :sortKey)",
        ExpressionAttributeValues = new()
        {
          { ":applicationId", new AttributeValue { S = identifier } },
          { ":sortKey", new AttributeValue { S = SortKeyPrefix } },
        },
      }, cancellationToken);

      foreach (var token in tokens)
      {
        yield return token;
      }
    }
  }

  public IAsyncEnumerable<TToken> FindByAuthorizationIdAsync(string identifier, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(identifier);

    return ExecuteAsync(cancellationToken);

    async IAsyncEnumerable<TToken> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
      // The tokens are listed in the authorization's partition
      var members = await DynamoDbItems.QueryPartitionAsync<MemberItem>(
        _client, _tableName, new OpenIddictDynamoDbAuthorization { Id = identifier }.PartitionKey, Lookups.TokenMemberPrefix, cancellationToken, consistentRead: true);
      var tokens = await DynamoDbItems.BatchGetAsync<TToken>(
        _client, _tableName, members.Select(x => new TToken { Id = x.Id }).Select(DynamoDbItems.Key), cancellationToken);

      foreach (var token in tokens)
      {
        yield return token;
      }
    }
  }

  public async ValueTask<TToken?> FindByIdAsync(string identifier, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(identifier);

    var token = new TToken { Id = identifier };
    // A stale token would fail to update, and could still look valid after being revoked
    return await DynamoDbItems.GetAsync<TToken>(
      _client, _tableName, token.PartitionKey, token.SortKey, cancellationToken, consistentRead: true);
  }

  public async ValueTask<TToken?> FindByReferenceIdAsync(string identifier, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(identifier);

    var lookup = await DynamoDbItems.GetAsync<LookupItem>(
      _client, _tableName, Lookups.ReferenceId(identifier), Lookups.SortKey, cancellationToken, consistentRead: true);

    return lookup is null ? default : await FindByIdAsync(lookup.Id, cancellationToken);
  }

  public IAsyncEnumerable<TToken> FindBySubjectAsync(string subject, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(subject);

    return FindBySubjectAndSearchKey(subject, DynamoDbUtils.GetSearchKeyPrefix(null, null, null), cancellationToken);
  }

  public ValueTask<string?> GetApplicationIdAsync(TToken token, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    return new(token.ApplicationId);
  }

  public ValueTask<TResult?> GetAsync<TState, TResult>(Func<IQueryable<TToken>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
  {
    throw new NotSupportedException();
  }

  public ValueTask<string?> GetAuthorizationIdAsync(TToken token, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    return new(token.AuthorizationId);
  }

  public ValueTask<DateTimeOffset?> GetCreationDateAsync(TToken token, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    return new(token.CreationDate);
  }

  public ValueTask<DateTimeOffset?> GetExpirationDateAsync(TToken token, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    return new(token.ExpirationDate);
  }

  public ValueTask<string?> GetIdAsync(TToken token, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    return new(token.Id);
  }

  public ValueTask<string?> GetPayloadAsync(TToken token, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    return new(token.Payload);
  }

  public ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(TToken token, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    if (string.IsNullOrEmpty(token.Properties))
    {
      return new(ImmutableDictionary.Create<string, JsonElement>());
    }

    using var document = JsonDocument.Parse(token.Properties);
    var properties = ImmutableDictionary.CreateBuilder<string, JsonElement>();

    foreach (var property in document.RootElement.EnumerateObject())
    {
      properties[property.Name] = property.Value.Clone();
    }

    return new(properties.ToImmutable());
  }

  public ValueTask<DateTimeOffset?> GetRedemptionDateAsync(TToken token, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    return new(token.RedemptionDate);
  }

  public ValueTask<string?> GetReferenceIdAsync(TToken token, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    return new(token.ReferenceId);
  }

  public ValueTask<string?> GetStatusAsync(TToken token, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    return new(token.Status);
  }

  public ValueTask<string?> GetSubjectAsync(TToken token, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    return new(token.Subject);
  }

  public ValueTask<string?> GetTypeAsync(TToken token, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    return new(token.Type);
  }

  public ValueTask<TToken> InstantiateAsync(CancellationToken cancellationToken)
    => new(new TToken());

  public ConcurrentDictionary<int, string?> ListCursors { get; set; } = new ConcurrentDictionary<int, string?>();
  public IAsyncEnumerable<TToken> ListAsync(int? count, int? offset, CancellationToken cancellationToken)
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

    async IAsyncEnumerable<TToken> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
      var (token, items) = await DynamoDbUtils.Paginate<TToken>(
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

  public IAsyncEnumerable<TResult> ListAsync<TState, TResult>(Func<IQueryable<TToken>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
  {
    throw new NotSupportedException();
  }

  // Should not be needed to run, TTL should handle the pruning
  public async ValueTask<long> PruneAsync(DateTimeOffset threshold, CancellationToken cancellationToken)
  {
    var deleteCount = 0;
    // Get all tokens which is older than threshold
    var tokens = await DynamoDbItems.ScanAsync<TToken>(_client, new ScanRequest
    {
      TableName = _tableName,
      FilterExpression = "begins_with(PartitionKey, :partitionKey) and CreationDate < :threshold",
      ExpressionAttributeValues = new()
      {
        { ":partitionKey", new AttributeValue { S = PartitionKeyPrefix } },
        { ":threshold", new AttributeValue { S = AttributeValues.ToDateString(threshold.UtcDateTime) } },
      },
    }, cancellationToken);
    var remainingTokens = new List<TToken>();
    var toBeDeleted = new List<WriteRequest>();

    // Add tokens which is not Inactive/Valid or where ExpirationDate has passed to delete batch
    foreach (var token in tokens)
    {
      if (new[] { Statuses.Inactive, Statuses.Valid }.Contains(token.Status) == false
        || token.ExpirationDate < DateTime.UtcNow)
      {
        toBeDeleted.AddRange(LookupsOf(token).Append(token).Select(DynamoDbItems.Delete));
        deleteCount++;
      }
      else
      {
        remainingTokens.Add(token);
      }
    }

    // Get all authorizations connected to the remaining tokens
    var authorizationKeys = remainingTokens
      .Select(x => x.AuthorizationId)
      .Where(x => x != default)
      .Distinct()
      .Select(x => new OpenIddictDynamoDbAuthorization { Id = x! })
      .Select(DynamoDbItems.Key);
    var authorizations = await DynamoDbItems.BatchGetAsync<OpenIddictDynamoDbAuthorization>(
      _client, _tableName, authorizationKeys, cancellationToken);

    // Add tokens which has invalid authorizations to delete batch
    foreach (var authorization in authorizations.Where(x => x.Status != Statuses.Valid))
    {
      var tokensToDelete = remainingTokens
        .Where(x => x.AuthorizationId == authorization.Id)
        .ToList();
      toBeDeleted.AddRange(tokensToDelete.SelectMany(x => LookupsOf(x).Append(x)).Select(DynamoDbItems.Delete));
      deleteCount += tokensToDelete.Count;
    }

    await DynamoDbUtils.BatchWriteAsync(_client, _tableName, toBeDeleted, cancellationToken);

    await DynamoDbUtils.UpdateCountAsync(_client, _tableName, CountType.Token, -deleteCount, cancellationToken);

    return deleteCount;
  }

  public ValueTask SetApplicationIdAsync(TToken token, string? identifier, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    token.ApplicationId = identifier;

    return default;
  }

  public ValueTask SetAuthorizationIdAsync(TToken token, string? identifier, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    token.AuthorizationId = identifier;

    return default;
  }

  public ValueTask SetCreationDateAsync(TToken token, DateTimeOffset? date, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    token.CreationDate = date?.UtcDateTime;

    return default;
  }

  public ValueTask SetExpirationDateAsync(TToken token, DateTimeOffset? date, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    token.ExpirationDate = date?.UtcDateTime;

    return default;
  }

  public ValueTask SetPayloadAsync(TToken token, string? payload, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    token.Payload = payload;

    return default;
  }

  public ValueTask SetPropertiesAsync(TToken token, ImmutableDictionary<string, JsonElement> properties, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    if (properties is not { Count: > 0 })
    {
      token.Properties = null;

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

    token.Properties = Encoding.UTF8.GetString(stream.ToArray());

    return default;
  }

  public ValueTask SetRedemptionDateAsync(TToken token, DateTimeOffset? date, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    token.RedemptionDate = date?.UtcDateTime;

    return default;
  }

  public ValueTask SetReferenceIdAsync(TToken token, string? identifier, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    token.ReferenceId = identifier;

    return default;
  }

  public ValueTask SetStatusAsync(TToken token, string? status, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    token.Status = status;

    return default;
  }

  public ValueTask SetSubjectAsync(TToken token, string? subject, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    token.Subject = subject;

    return default;
  }

  public ValueTask SetTypeAsync(TToken token, string? type, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    token.Type = type;

    return default;
  }

  public async ValueTask UpdateAsync(TToken token, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    var concurrencyToken = token.ConcurrencyToken;
    var ttl = token.TTL;
    token.ConcurrencyToken = Guid.NewGuid().ToString();

    if (new[] { Statuses.Inactive, Statuses.Valid }.Contains(token.Status) == false)
    {
      token.TTL = DateTime.UtcNow.AddMinutes(5);
    }
    else
    {
      token.TTL = token.ExpirationDate;
    }

    // The token is written only when it still has the concurrency token it was loaded with, which
    // is also what makes sure that authorization codes and refresh tokens are redeemed once. Its
    // reference id lookup and authorization listing move with changed values, and take its time to live
    var items = new List<TransactWriteItem>
    {
      DynamoDbItems.TransactPut(
        _tableName,
        token,
        concurrencyToken is null
          ? "attribute_exists(PartitionKey) and attribute_not_exists(ConcurrencyToken)"
          : "attribute_exists(PartitionKey) and ConcurrencyToken = :concurrencyToken",
        concurrencyToken is null ? default : new() { { ":concurrencyToken", new AttributeValue { S = concurrencyToken } } }),
    };
    var lookupIndex = -1;

    if (token.StoredReferenceId is { } previousReferenceId && previousReferenceId != token.ReferenceId)
    {
      items.Add(DeleteReferenceLookup(previousReferenceId, token.Id));
    }

    if (token.ReferenceId is { } referenceId)
    {
      lookupIndex = items.Count;
      items.Add(DynamoDbItems.TransactPut(
        _tableName, new LookupItem(Lookups.ReferenceId(referenceId), token.Id, token.TTL),
        "attribute_not_exists(PartitionKey) or Id = :id", new() { { ":id", new AttributeValue { S = token.Id } } }));
    }

    if (token.StoredAuthorizationId is { } previousAuthorizationId && previousAuthorizationId != token.AuthorizationId)
    {
      items.Add(DynamoDbItems.TransactDelete(
        _tableName, new OpenIddictDynamoDbAuthorization { Id = previousAuthorizationId }.PartitionKey, Lookups.TokenMember(token.Id)));
    }

    if (AuthorizationMember(token) is { } member)
    {
      items.Add(DynamoDbItems.TransactPut(_tableName, member));
    }

    try
    {
      await DynamoDbItems.TransactWriteAsync(_client, items, cancellationToken);
    }
    catch (TransactionCanceledException exception)
    {
      token.ConcurrencyToken = concurrencyToken;
      token.TTL = ttl;
      var failed = DynamoDbItems.FailedConditions(exception);

      throw failed.Contains(lookupIndex) && !failed.Contains(0)
        ? DuplicateReferenceId(exception)
        : new OpenIddictExceptions.ConcurrencyException(OpenIddictResources.GetResourceString(OpenIddictResources.ID0247), exception);
    }

    token.StoredReferenceId = token.ReferenceId;
    token.StoredAuthorizationId = token.AuthorizationId;

    await ExtendAuthorizationLifetime(token, cancellationToken);
  }

  // Ad-hoc authorizations are only needed while their tokens can be used, so they are kept
  // until the last of their tokens expires. Tokens never shorten the lifetime of an
  // authorization, redeeming an authorization code must not remove the authorization that
  // its refresh tokens belong to, and permanent authorizations are kept until revoked.
  private async Task ExtendAuthorizationLifetime(TToken token, CancellationToken cancellationToken)
  {
    if (string.IsNullOrEmpty(token.AuthorizationId) || token.ExpirationDate is not { } expirationDate)
    {
      return;
    }

    var authorization = new OpenIddictDynamoDbAuthorization
    {
      Id = token.AuthorizationId,
    };
    var expiresAt = new DateTimeOffset(expirationDate.Kind == DateTimeKind.Local
      ? expirationDate.ToUniversalTime()
      : DateTime.SpecifyKind(expirationDate, DateTimeKind.Utc)).ToUnixTimeSeconds();

    try
    {
      await _client.UpdateItemAsync(new UpdateItemRequest
      {
        TableName = _tableName,
        Key = DynamoDbItems.Key(authorization),
        UpdateExpression = "SET #ttl = :ttl",
        ConditionExpression = "attribute_exists(PartitionKey) and #type = :adHoc and #status = :valid "
          + "and (attribute_not_exists(#ttl) or #ttl < :ttl)",
        ExpressionAttributeNames = new()
        {
          { "#ttl", "ttl" },
          { "#type", "Type" },
          { "#status", "Status" },
        },
        ExpressionAttributeValues = new()
        {
          { ":ttl", new AttributeValue { N = expiresAt.ToString(CultureInfo.InvariantCulture) } },
          { ":adHoc", new AttributeValue { S = AuthorizationTypes.AdHoc } },
          { ":valid", new AttributeValue { S = Statuses.Valid } },
        },
      }, cancellationToken);
    }
    catch (ConditionalCheckFailedException)
    {
      // The authorization is permanent, no longer valid, missing or already kept long enough
    }
  }

  public async ValueTask<long> RevokeByAuthorizationIdAsync(string identifier, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(identifier);

    var requests = new List<WriteRequest>();

    await foreach (var token in FindByAuthorizationIdAsync(identifier, cancellationToken))
    {
      token.Status = Statuses.Inactive;
      token.TTL = DateTime.UtcNow.AddMinutes(5);
      requests.AddRange(LookupsOf(token).Append(token).Select(DynamoDbItems.Put));
    }

    await DynamoDbUtils.BatchWriteAsync(_client, _tableName, requests, cancellationToken);

    return requests.Count(x => x.PutRequest.Item["SortKey"].S.StartsWith(SortKeyPrefix, StringComparison.Ordinal));
  }

  public ValueTask<long> RevokeAsync(string? subject, string? client, string? status, string? type, CancellationToken cancellationToken)
  {
    var tokens = FindAsync(subject, client, status, type, cancellationToken);
    return RevokeAsync(tokens, cancellationToken);
  }

  public ValueTask<long> RevokeByApplicationIdAsync(string identifier, CancellationToken cancellationToken = default)
  {
    var tokens = FindByApplicationIdAsync(identifier, cancellationToken);
    return RevokeAsync(tokens, cancellationToken);
  }

  public ValueTask<long> RevokeBySubjectAsync(string subject, CancellationToken cancellationToken = default)
  {
    var tokens = FindBySubjectAsync(subject, cancellationToken);
    return RevokeAsync(tokens, cancellationToken);
  }

  private async ValueTask<long> RevokeAsync(IAsyncEnumerable<TToken> tokens, CancellationToken cancellationToken)
  {
    var requests = new List<WriteRequest>();

    var revoked = 0L;

    await foreach (var token in tokens)
    {
      token.Status = Statuses.Revoked;
      token.TTL = DateTime.UtcNow.AddMinutes(5);

      requests.AddRange(LookupsOf(token).Append(token).Select(DynamoDbItems.Put));
      revoked++;
    }

    await DynamoDbUtils.BatchWriteAsync(_client, _tableName, requests, cancellationToken);

    return revoked;
  }
}
