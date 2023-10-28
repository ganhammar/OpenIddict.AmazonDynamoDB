using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Globalization;
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

namespace OpenIddict.AmazonDynamoDB;

public class OpenIddictDynamoDbScopeStore<TScope> : IOpenIddictScopeStore<TScope>
    where TScope : OpenIddictDynamoDbScope, new()
{
  private readonly IAmazonDynamoDB _client;
  private readonly IDynamoDBContext _context;
  private readonly string _tableName;

  public OpenIddictDynamoDbScopeStore(
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
    _context = new DynamoDBContext(_client);
    _tableName = options.DefaultTableName ?? Constants.DefaultTableName;
  }

  public async ValueTask<long> CountAsync(CancellationToken cancellationToken)
  {
    var count = new CountModel(CountType.Scope);
    count = await _context.LoadAsync<CountModel>(count.PartitionKey, count.SortKey, cancellationToken);

    return count?.Count ?? 0;
  }

  public ValueTask<long> CountAsync<TResult>(Func<IQueryable<TScope>, IQueryable<TResult>> query, CancellationToken cancellationToken)
  {
    throw new NotSupportedException();
  }

  public async ValueTask CreateAsync(TScope scope, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(scope);

    await _context.SaveAsync(scope, cancellationToken);
    await SaveLookups(scope, cancellationToken);

    var count = await CountAsync(cancellationToken);
    await _context.SaveAsync(new CountModel(CountType.Scope, count + 1), cancellationToken);
  }

  public async ValueTask DeleteAsync(TScope scope, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(scope);

    await _context.DeleteAsync(scope, cancellationToken);

    var count = await CountAsync(cancellationToken);
    await _context.SaveAsync(new CountModel(CountType.Scope, count - 1), cancellationToken);
  }

  public async ValueTask<TScope?> FindByIdAsync(string identifier, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(identifier);

    var scope = new TScope
    {
      Id = identifier,
    };
    scope = await _context.LoadAsync<TScope>(scope.PartitionKey, scope.SortKey, cancellationToken);

    return scope;
  }

  public async ValueTask<TScope?> FindByNameAsync(string name, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(name);

    var search = _context.FromQueryAsync<TScope>(new QueryOperationConfig
    {
      IndexName = "Name-index",
      KeyExpression = new Expression
      {
        ExpressionStatement = "ScopeName = :name",
        ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
        {
          { ":name", name },
        },
      },
      Limit = 1,
    });
    var scopes = await search.GetRemainingAsync(cancellationToken);
    var scope = scopes?.FirstOrDefault();

    if (scope == default)
    {
      return default;
    }

    return scope;
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
      var batch = _context.CreateBatchGet<OpenIddictDynamoDbScopeLookup>();
      foreach (var name in names)
      {
        var lookup = new OpenIddictDynamoDbScopeLookup(name, LookupType.Name);
        batch.AddKey(lookup.PartitionKey, lookup.SortKey);
      }
      await batch.ExecuteAsync(cancellationToken);
      var scopeIds = batch.Results.Select(x => x.ScopeId!).Distinct();
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
      var search = _context.FromQueryAsync<OpenIddictDynamoDbScopeLookup>(new QueryOperationConfig
      {
        KeyExpression = new Expression
        {
          ExpressionStatement = "PartitionKey = :partitionKey and begins_with(SortKey, :sortKey)",
          ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
          {
            { ":partitionKey", lookup.PartitionKey },
            { ":sortKey", lookup.SortKey },
          },
        },
      });

      var lookups = await search.GetRemainingAsync(cancellationToken);
      var scopeIds = lookups.Select(x => x.ScopeId!).Distinct();
      var scopes = await GetById(scopeIds, cancellationToken);

      foreach (var scope in scopes)
      {
        yield return scope;
      }
    }
  }

  private async Task<List<TScope>> GetById(IEnumerable<string> scopeIds, CancellationToken cancellationToken)
  {
    var batch = _context.CreateBatchGet<TScope>();
    foreach (var scopeId in scopeIds)
    {
      var scope = new TScope
      {
        Id = scopeId!,
      };
      batch.AddKey(scope.PartitionKey, scope.SortKey);
    }
    await batch.ExecuteAsync(cancellationToken);

    return batch.Results;
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
  {
    try
    {
      return new(Activator.CreateInstance<TScope>());
    }
    catch (MemberAccessException exception)
    {
      return new(Task.FromException<TScope>(
        new InvalidOperationException(OpenIddictResources.GetResourceString(OpenIddictResources.ID0246), exception)));
    }
  }

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
      var (token, items) = await DynamoDbUtils.Paginate<TScope>(_client, count, initalToken, cancellationToken);

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

    // Ensure no one else is updating
    var databaseApplication = await _context.LoadAsync<TScope>(scope.PartitionKey, scope.SortKey, cancellationToken);
    if (databaseApplication == default || databaseApplication.ConcurrencyToken != scope.ConcurrencyToken)
    {
      throw new ArgumentException("Given scope is invalid", nameof(scope));
    }

    scope.ConcurrencyToken = Guid.NewGuid().ToString();

    await _context.SaveAsync(scope, cancellationToken);
    await UpdateLookups(scope, cancellationToken);
  }

  private async Task UpdateLookups(TScope scope, CancellationToken cancellationToken)
  {
    // Update scope lookups
    // Fetch all lookups
    var search = _context.FromQueryAsync<OpenIddictDynamoDbScopeLookup>(new()
    {
      IndexName = "ScopeId-index",
      KeyExpression = new Expression
      {
        ExpressionStatement = "ScopeId = :scopeId and begins_with(PartitionKey, :partitionKey)",
        ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
        {
          { ":scopeId", scope.Id },
          { ":partitionKey", "SCOPELOOKUP#" },
        }
      },
    });

    var lookups = await search.GetRemainingAsync(cancellationToken);

    // Remove previously stored scope lookups
    if (lookups?.Any() == true)
    {
      var writeRequests = lookups
        .Select(x => new WriteRequest
        {
          DeleteRequest = new DeleteRequest
          {
            Key = new Dictionary<string, AttributeValue>
            {
              { "PartitionKey", new AttributeValue { S = x.PartitionKey } },
              { "SortKey", new AttributeValue { S = x.SortKey } },
            },
          },
        })
        .ToList();

      var request = new BatchWriteItemRequest
      {
        RequestItems = new Dictionary<string, List<WriteRequest>>
        {
          { _tableName, writeRequests },
        },
      };

      await _client.BatchWriteItemAsync(request, cancellationToken);
    }

    // Save current redirects
    await SaveLookups(scope, cancellationToken);
  }

  private async Task SaveLookups(TScope scope, CancellationToken cancellationToken)
  {
    var batch = _context.CreateBatchWrite<OpenIddictDynamoDbScopeLookup>();

    if (scope.Resources?.Any() == true)
    {
      foreach (var resouce in scope.Resources)
      {
        batch.AddPutItem(new(resouce, LookupType.Resource, scope.Id)
        {
          ScopeId = scope.Id,
        });
      }
    }

    batch.AddPutItem(new(scope.Name!, LookupType.Name)
    {
      ScopeId = scope.Id,
    });

    await batch.ExecuteAsync(cancellationToken);
  }
}
