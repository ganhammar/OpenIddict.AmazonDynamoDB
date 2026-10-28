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

namespace OpenIddict.AmazonDynamoDB;

public class OpenIddictDynamoDbScopeStore<TScope> : IOpenIddictScopeStore<TScope>
    where TScope : OpenIddictDynamoDbScope, new()
{
  private readonly IAmazonDynamoDB _client;
  private readonly string _tableName;

  public OpenIddictDynamoDbScopeStore(
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
    => await DynamoDbUtils.GetCountAsync(_client, _tableName, CountType.Scope, cancellationToken);

  public ValueTask<long> CountAsync<TResult>(Func<IQueryable<TScope>, IQueryable<TResult>> query, CancellationToken cancellationToken)
  {
    throw new NotSupportedException();
  }

  // The scope and the lookup of its name are written together, so a name that another scope
  // holds fails the whole write; the lookups of its resources follow
  public async ValueTask CreateAsync(TScope scope, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(scope);

    var items = new List<TransactWriteItem>
    {
      DynamoDbItems.TransactPut(_tableName, scope, "attribute_not_exists(PartitionKey)"),
    };

    if (scope.Name is { } name)
    {
      items.Add(DynamoDbItems.TransactPut(_tableName, Lookups.ScopeName(name, scope.Id), "attribute_not_exists(PartitionKey)"));
    }

    try
    {
      await DynamoDbItems.TransactWriteAsync(_client, items, cancellationToken);
    }
    catch (TransactionCanceledException exception)
    {
      throw DynamoDbItems.FailedConditions(exception).Contains(1)
        ? DuplicateName(exception)
        : new InvalidOperationException($"A scope with the id {scope.Id} already exists", exception);
    }

    scope.StoredName = scope.Name;
    await SaveResourceLookups(scope, [], cancellationToken);

    await DynamoDbUtils.UpdateCountAsync(_client, _tableName, CountType.Scope, 1, cancellationToken);
  }

  private static OpenIddictExceptions.ValidationException DuplicateName(Exception exception)
  {
    var message = OpenIddictResources.GetResourceString(OpenIddictResources.ID2060);
    return new OpenIddictExceptions.ValidationException(message, [new System.ComponentModel.DataAnnotations.ValidationResult(message)], exception);
  }

  // Removes the scope with the lookups of its stored name and resources
  public async ValueTask DeleteAsync(TScope scope, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(scope);

    var stored = await FindByIdAsync(scope.Id, cancellationToken) ?? scope;
    var items = new List<TransactWriteItem>
    {
      DynamoDbItems.TransactDelete(_tableName, scope.PartitionKey, scope.SortKey),
    };

    if (stored.Name is { } name)
    {
      items.Add(DeleteNameLookup(name, scope.Id));
    }

    await DynamoDbItems.TransactWriteAsync(_client, items, cancellationToken);
    await DynamoDbUtils.BatchWriteAsync(
      _client, _tableName, (stored.Resources ?? []).Select(x => DynamoDbItems.Delete(Lookups.ScopeResource(x, scope.Id))), cancellationToken);

    await DynamoDbUtils.UpdateCountAsync(_client, _tableName, CountType.Scope, -1, cancellationToken);
  }

  public async ValueTask<TScope?> FindByIdAsync(string identifier, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(identifier);

    var scope = new TScope
    {
      Id = identifier,
    };
    // A stale scope would fail to update
    return await DynamoDbItems.GetAsync<TScope>(
      _client, _tableName, scope.PartitionKey, scope.SortKey, cancellationToken, consistentRead: true);
  }

  public async ValueTask<TScope?> FindByNameAsync(string name, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(name);

    var lookup = Lookups.ScopeName(name);
    var stored = await DynamoDbItems.GetAsync<OpenIddictDynamoDbScopeLookup>(
      _client, _tableName, lookup.PartitionKey, lookup.SortKey, cancellationToken, consistentRead: true);

    return stored?.ScopeId is null ? default : await FindByIdAsync(stored.ScopeId, cancellationToken);
  }

  public IAsyncEnumerable<TScope> FindByNamesAsync(ImmutableArray<string> names, CancellationToken cancellationToken)
  {
    if (names == null)
    {
      throw new ArgumentNullException(nameof(names));
    }
    else if (names is { Length: 0 })
    {
      return AsyncEnumerable.Empty<TScope>();
    }

    return ExecuteAsync(cancellationToken);

    async IAsyncEnumerable<TScope> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
      var keys = names
        .Select(x => new OpenIddictDynamoDbScopeLookup(x, LookupType.Name))
        .Select(DynamoDbItems.Key);
      var lookups = await DynamoDbItems.BatchGetAsync<OpenIddictDynamoDbScopeLookup>(_client, _tableName, keys, cancellationToken);
      var scopeIds = lookups.Select(x => x.ScopeId!).Distinct();
      var scopes = await GetById(scopeIds, cancellationToken);

      foreach (var scope in scopes)
      {
        yield return scope;
      }
    }
  }

  public IAsyncEnumerable<TScope> FindByResourceAsync(string resource, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(resource);

    return ExecuteAsync(cancellationToken);

    async IAsyncEnumerable<TScope> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
      var lookup = new OpenIddictDynamoDbScopeLookup(resource, LookupType.Resource);
      var lookups = await DynamoDbItems.QueryAsync<OpenIddictDynamoDbScopeLookup>(_client, new QueryRequest
      {
        TableName = _tableName,
        KeyConditionExpression = "PartitionKey = :partitionKey and begins_with(SortKey, :sortKey)",
        ExpressionAttributeValues = new()
        {
          { ":partitionKey", new AttributeValue { S = lookup.PartitionKey } },
          { ":sortKey", new AttributeValue { S = lookup.SortKey } },
        },
      }, cancellationToken);

      var scopeIds = lookups.Select(x => x.ScopeId!).Distinct();
      var scopes = await GetById(scopeIds, cancellationToken);

      foreach (var scope in scopes)
      {
        yield return scope;
      }
    }
  }

  private Task<List<TScope>> GetById(IEnumerable<string> scopeIds, CancellationToken cancellationToken)
  {
    var keys = scopeIds
      .Select(x => new TScope { Id = x })
      .Select(DynamoDbItems.Key);

    return DynamoDbItems.BatchGetAsync<TScope>(_client, _tableName, keys, cancellationToken);
  }

  public ValueTask<TResult?> GetAsync<TState, TResult>(Func<IQueryable<TScope>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
  {
    throw new NotSupportedException();
  }

  public ValueTask<string?> GetDescriptionAsync(TScope scope, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(scope);

    return new(scope.Description);
  }

  public ValueTask<ImmutableDictionary<CultureInfo, string>> GetDescriptionsAsync(TScope scope, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(scope);

    if (scope.Descriptions is not { Count: > 0 })
    {
      return new(ImmutableDictionary.Create<CultureInfo, string>());
    }

    return new(scope.Descriptions.ToImmutableDictionary(
      pair => CultureInfo.GetCultureInfo(pair.Key),
      pair => pair.Value));
  }

  public ValueTask<string?> GetDisplayNameAsync(TScope scope, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(scope);

    return new(scope.DisplayName);
  }

  public ValueTask<ImmutableDictionary<CultureInfo, string>> GetDisplayNamesAsync(TScope scope, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(scope);

    if (scope.DisplayNames is not { Count: > 0 })
    {
      return new(ImmutableDictionary.Create<CultureInfo, string>());
    }

    return new(scope.DisplayNames.ToImmutableDictionary(
      pair => CultureInfo.GetCultureInfo(pair.Key),
      pair => pair.Value));
  }

  public ValueTask<string?> GetIdAsync(TScope scope, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(scope);

    return new(scope.Id);
  }

  public ValueTask<string?> GetNameAsync(TScope scope, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(scope);

    return new(scope.Name);
  }

  public ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(TScope scope, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(scope);

    if (string.IsNullOrEmpty(scope.Properties))
    {
      return new(ImmutableDictionary.Create<string, JsonElement>());
    }

    using var document = JsonDocument.Parse(scope.Properties);
    var properties = ImmutableDictionary.CreateBuilder<string, JsonElement>();

    foreach (var property in document.RootElement.EnumerateObject())
    {
      properties[property.Name] = property.Value.Clone();
    }

    return new(properties.ToImmutable());
  }

  public ValueTask<ImmutableArray<string>> GetResourcesAsync(TScope scope, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(scope);

    if (scope.Resources is not { Count: > 0 })
    {
      return new(ImmutableArray.Create<string>());
    }

    return new(scope.Resources.ToImmutableArray());
  }

  public ValueTask<TScope> InstantiateAsync(CancellationToken cancellationToken)
    => new(new TScope());

  public ConcurrentDictionary<int, string?> ListCursors { get; set; } = new ConcurrentDictionary<int, string?>();
  public IAsyncEnumerable<TScope> ListAsync(int? count, int? offset, CancellationToken cancellationToken)
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

    async IAsyncEnumerable<TScope> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
      var (token, items) = await DynamoDbUtils.Paginate<TScope>(
        _client, _tableName, "SCOPE#", "#SCOPE#", count, initalToken, cancellationToken);

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
    Func<IQueryable<TScope>, TState, IQueryable<TResult>> query,
    TState state,
    CancellationToken cancellationToken)
  {
    throw new NotSupportedException();
  }

  public ValueTask SetDescriptionAsync(TScope scope, string? description, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(scope);

    scope.Description = description;

    return default;
  }

  public ValueTask SetDescriptionsAsync(TScope scope, ImmutableDictionary<CultureInfo, string> descriptions, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(scope);

    if (descriptions is not { Count: > 0 })
    {
      scope.Descriptions = null;

      return default;
    }

    scope.Descriptions = descriptions.ToDictionary(x => x.Key.ToString(), x => x.Value);

    return default;
  }

  public ValueTask SetDisplayNameAsync(TScope scope, string? name, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(scope);

    scope.DisplayName = name;

    return default;
  }

  public ValueTask SetDisplayNamesAsync(TScope scope, ImmutableDictionary<CultureInfo, string> names, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(scope);

    if (names is not { Count: > 0 })
    {
      scope.DisplayNames = null;

      return default;
    }

    scope.DisplayNames = names.ToDictionary(x => x.Key.ToString(), x => x.Value);

    return default;
  }

  public ValueTask SetNameAsync(TScope scope, string? name, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(scope);

    scope.Name = name;

    return default;
  }

  public ValueTask SetPropertiesAsync(TScope scope, ImmutableDictionary<string, JsonElement> properties, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(scope);

    if (properties is not { Count: > 0 })
    {
      scope.Properties = null;

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

    scope.Properties = Encoding.UTF8.GetString(stream.ToArray());

    return default;
  }

  public ValueTask SetResourcesAsync(TScope scope, ImmutableArray<string> resources, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(scope);

    if (resources is not { Length: > 0 })
    {
      scope.Resources = null;

      return default;
    }

    scope.Resources = resources.ToList();

    return default;
  }

  public async ValueTask UpdateAsync(TScope scope, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(scope);

    var concurrencyToken = scope.ConcurrencyToken;
    scope.ConcurrencyToken = Guid.NewGuid().ToString();

    // The scope is written only when it still has the concurrency token it was loaded with, and
    // the lookup of its name moves with a changed one in the same transaction
    var items = new List<TransactWriteItem>
    {
      DynamoDbItems.TransactPut(
        _tableName,
        scope,
        concurrencyToken is null
          ? "attribute_exists(PartitionKey) and attribute_not_exists(ConcurrencyToken)"
          : "attribute_exists(PartitionKey) and ConcurrencyToken = :concurrencyToken",
        concurrencyToken is null ? default : new() { { ":concurrencyToken", new AttributeValue { S = concurrencyToken } } }),
    };
    var lookupIndex = -1;

    if (scope.StoredName != scope.Name)
    {
      if (scope.StoredName is { } previousName)
      {
        items.Add(DeleteNameLookup(previousName, scope.Id));
      }

      if (scope.Name is { } name)
      {
        lookupIndex = items.Count;
        items.Add(DynamoDbItems.TransactPut(
          _tableName, Lookups.ScopeName(name, scope.Id),
          "attribute_not_exists(PartitionKey) or ScopeId = :id", new() { { ":id", new AttributeValue { S = scope.Id } } }));
      }
    }

    try
    {
      await DynamoDbItems.TransactWriteAsync(_client, items, cancellationToken);
    }
    catch (TransactionCanceledException exception)
    {
      scope.ConcurrencyToken = concurrencyToken;
      var failed = DynamoDbItems.FailedConditions(exception);

      throw failed.Contains(lookupIndex) && !failed.Contains(0)
        ? DuplicateName(exception)
        : new OpenIddictExceptions.ConcurrencyException(OpenIddictResources.GetResourceString(OpenIddictResources.ID0245), exception);
    }

    var previousResources = scope.StoredResources ?? [];
    scope.StoredName = scope.Name;
    await SaveResourceLookups(scope, previousResources, cancellationToken);
  }

  // Removes a name lookup that points at the scope, or is already gone
  private TransactWriteItem DeleteNameLookup(string name, string scopeId)
  {
    var lookup = Lookups.ScopeName(name);
    return DynamoDbItems.TransactDelete(
      _tableName, lookup.PartitionKey, lookup.SortKey,
      "attribute_not_exists(PartitionKey) or ScopeId = :id", new() { { ":id", new AttributeValue { S = scopeId } } });
  }

  // Each resource of a scope has a lookup of its own, keyed by the scope, so they need no condition
  private async Task SaveResourceLookups(TScope scope, IEnumerable<string> previousResources, CancellationToken cancellationToken)
  {
    var current = (scope.Resources ?? []).ToHashSet();
    var removed = previousResources.Where(x => current.Contains(x) == false);

    await DynamoDbUtils.BatchWriteAsync(
      _client,
      _tableName,
      removed.Select(x => DynamoDbItems.Delete(Lookups.ScopeResource(x, scope.Id)))
        .Concat(current.Select(x => DynamoDbItems.Put(Lookups.ScopeResource(x, scope.Id)))),
      cancellationToken);

    scope.StoredResources = current.ToList();
  }
}
