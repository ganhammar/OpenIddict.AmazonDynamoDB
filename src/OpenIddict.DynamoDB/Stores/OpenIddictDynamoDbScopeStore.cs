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

namespace OpenIddict.DynamoDB;

public class OpenIddictDynamoDbScopeStore<TScope> : IOpenIddictScopeStore<TScope>
    where TScope : OpenIddictDynamoDbScope
{
    private IAmazonDynamoDB _client;
    private IDynamoDBContext _context;
    private IOptionsMonitor<OpenIddictDynamoDbOptions> _optionsMonitor;
    private OpenIddictDynamoDbOptions _openIddictDynamoDbOptions => _optionsMonitor.CurrentValue;

    public OpenIddictDynamoDbScopeStore(IOptionsMonitor<OpenIddictDynamoDbOptions> optionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);

        _optionsMonitor = optionsMonitor;

        ArgumentNullException.ThrowIfNull(_openIddictDynamoDbOptions.Database);

        _client = _openIddictDynamoDbOptions.Database;
        _context = new DynamoDBContext(_client);
    }

    public async ValueTask<long> CountAsync(CancellationToken cancellationToken)
    {
        var description = await _client.DescribeTableAsync(new DescribeTableRequest
        {
            TableName = Constants.DefaultScopeTableName,
        });

        return description.Table.ItemCount;
    }

    public ValueTask<long> CountAsync<TResult>(Func<IQueryable<TScope>, IQueryable<TResult>> query, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    private async Task SaveResources(TScope scope, CancellationToken cancellationToken)
    {
        if (scope.Resources?.Any() != true)
        {
            return;
        }

        var batch = _context.CreateBatchWrite<OpenIddictDynamoDbScopeResource>();

        foreach (var resouce in scope.Resources)
        {
            var scopeResource = new OpenIddictDynamoDbScopeResource
            {
                ScopeId = scope.Id,
                ScopeResource = resouce,
            };

            batch.AddPutItem(scopeResource);
        }

        await batch.ExecuteAsync(cancellationToken);
    }

    private async Task SetResources(TScope scope, CancellationToken cancellationToken)
    {
        var scopeId = scope.Id;
        var search = _context.FromQueryAsync<OpenIddictDynamoDbScopeResource>(new QueryOperationConfig
        {
            KeyExpression = new Expression
            {
                ExpressionStatement = "ScopeId = :scopeId",
                ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                {
                    { ":scopeId", scopeId },
                }
            },
        });

        var resources = await search.GetRemainingAsync(cancellationToken);

        scope.Resources = resources
            .Where(x => x.ScopeResource != default)
            .Select(x => x.ScopeResource!)
            .ToList();
    }

    public async ValueTask CreateAsync(TScope scope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);

        cancellationToken.ThrowIfCancellationRequested();

        await _context.SaveAsync(scope, cancellationToken);
        await SaveResources(scope, cancellationToken);
    }

    public async ValueTask DeleteAsync(TScope scope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);

        cancellationToken.ThrowIfCancellationRequested();

        await _context.DeleteAsync(scope, cancellationToken);
    }

    public async ValueTask<TScope?> FindByIdAsync(string identifier, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        var scope = await _context.LoadAsync<TScope>(identifier, cancellationToken);
        await SetResources(scope, cancellationToken);

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
                }
            },
            Limit = 1
        });
        var scopes = await search.GetRemainingAsync(cancellationToken);
        var scope = scopes?.FirstOrDefault();

        if (scope == default)
        {
            return default;
        }

        await SetResources(scope, cancellationToken);

        return scope;
    }

    public IAsyncEnumerable<TScope> FindByNamesAsync(ImmutableArray<string> names, CancellationToken cancellationToken)
    {
        if (names == null)
        {
            throw new ArgumentNullException(nameof(names));
        }

        if (names is { Length: > 100 })
        {
            throw new NotSupportedException("Cannot fetch more than 100 scopes at a time");
        }

        return ExecuteAsync(cancellationToken);

        async IAsyncEnumerable<TScope> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var batch = _context.ScanAsync<TScope>(
                new[]
                {
                    new ScanCondition("Name", ScanOperator.In, names.ToArray()),
                });

            var scopes = await batch.GetRemainingAsync(cancellationToken);

            foreach (var scope in scopes)
            {
                await SetResources(scope, cancellationToken);
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
            var search = _context.FromQueryAsync<OpenIddictDynamoDbScopeResource>(new QueryOperationConfig
            {
                IndexName = "Resource-index",
                KeyExpression = new Expression
                {
                    ExpressionStatement = "ScopeResource = :resource",
                    ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                    {
                        { ":resource", resource },
                    }
                },
            });

            var scopeResources = await search.GetRemainingAsync();
            var scopeIds = scopeResources.Select(x => x.ScopeId).Distinct();

            var batch = _context.CreateBatchGet<TScope>();
            foreach (var scopeId in scopeIds)
            {
                batch.AddKey(scopeId);
            }
            await batch.ExecuteAsync(cancellationToken);

            foreach (var scope in batch.Results)
            {
                await SetResources(scope, cancellationToken);
                yield return scope;
            }
        }
    }

    public ValueTask<TResult> GetAsync<TState, TResult>(Func<IQueryable<TScope>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
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

    public IAsyncEnumerable<TResult> ListAsync<TState, TResult>(Func<IQueryable<TScope>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
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
        var databaseApplication = await _context.LoadAsync<TScope>(scope.Id, cancellationToken);
        if (databaseApplication == default || databaseApplication.ConcurrencyToken != scope.ConcurrencyToken)
        {
            throw new ArgumentException("Given scope is invalid", nameof(scope));
        }

        scope.ConcurrencyToken = Guid.NewGuid().ToString();

        await _context.SaveAsync(scope, cancellationToken);

        // Update scope resouces
        // Fetch all resources
        var search = _context.FromQueryAsync<OpenIddictDynamoDbScopeResource>(new QueryOperationConfig
        {
            KeyExpression = new Expression
            {
                ExpressionStatement = "ScopeId = :scopeId",
                ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                {
                    { ":scopeId", scope.Id },
                }
            },
        });

        var resources = await search.GetRemainingAsync(cancellationToken);

        // Remove previously stored redirects
        if (resources?.Any() == true)
        {
            var writeRequests = resources
                .Select(x => new WriteRequest
                {
                    DeleteRequest = new DeleteRequest
                    {
                        Key = new Dictionary<string, AttributeValue>
                        {
                            { "ScopeId", new AttributeValue { S = x.ScopeId } },
                            { "ScopeResource", new AttributeValue { S = x.ScopeResource } },
                        },
                    },
                })
                .ToList();

            var request = new BatchWriteItemRequest
            {
                RequestItems = new Dictionary<string, List<WriteRequest>>
                {
                    { Constants.DefaultScopeResourceTableName, writeRequests },
                },
            };

            await _client.BatchWriteItemAsync(request, cancellationToken);
        }

        // Save current redirects
        await SaveResources(scope, cancellationToken);
    }
}