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
    where TAuthorization : OpenIddictDynamoDbAuthorization
{
    private IAmazonDynamoDB _client;
    private IDynamoDBContext _context;

    public OpenIddictDynamoDbAuthorizationStore(
        IOptionsMonitor<OpenIddictDynamoDbOptions> optionsMonitor,
        IAmazonDynamoDB? database = default)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);

        var options = optionsMonitor.CurrentValue;

        if (options.Database == default && database == default)
        {
            throw new ArgumentNullException(nameof(options.Database));
        }

        _client = database ?? options.Database!;
        _context = new DynamoDBContext(_client);
    }

    public async ValueTask<long> CountAsync(CancellationToken cancellationToken)
    {
        var description = await _client.DescribeTableAsync(new DescribeTableRequest
        {
            TableName = Constants.DefaultAuthorizationTableName,
        });

        return description.Table.ItemCount;
    }

    public ValueTask<long> CountAsync<TResult>(Func<IQueryable<TAuthorization>, IQueryable<TResult>> query, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public async ValueTask CreateAsync(TAuthorization authorization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        await _context.SaveAsync(authorization, cancellationToken);
    }

    public async ValueTask DeleteAsync(TAuthorization authorization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        await _context.DeleteAsync(authorization, cancellationToken);
    }

    private IAsyncEnumerable<TAuthorization> FindBySubjectAndSearchKey(string subject, string searchKey, CancellationToken cancellationToken)
    {
        return ExecuteAsync(cancellationToken);

        async IAsyncEnumerable<TAuthorization> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var search = _context.FromQueryAsync<TAuthorization>(new QueryOperationConfig
            {
                IndexName = "Subject-SearchKey-index",
                KeyExpression = new Expression
                {
                    ExpressionStatement = "Subject = :subject and begins_with(SearchKey, :searchKey)",
                    ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                    {
                        { ":subject", subject },
                        { ":searchKey", searchKey },
                    }
                },
                Limit = 1,
            });

            var authorizations = await search.GetRemainingAsync(cancellationToken);

            foreach (var authorization in authorizations)
            {
                yield return authorization;
            }
        }
    }

    public IAsyncEnumerable<TAuthorization> FindAsync(
        string subject, string client, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(client);

        return FindBySubjectAndSearchKey(subject, client, cancellationToken);
    }

    public IAsyncEnumerable<TAuthorization> FindAsync(
        string subject, string client, string status, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(status);

        return FindBySubjectAndSearchKey(subject, $"{client}#{status}", cancellationToken);
    }

    public IAsyncEnumerable<TAuthorization> FindAsync(
        string subject, string client, string status, string type, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(status);
        ArgumentNullException.ThrowIfNull(type);

        return FindBySubjectAndSearchKey(subject, $"{client}#{status}#{type}", cancellationToken);
    }

    public IAsyncEnumerable<TAuthorization> FindAsync(
        string subject,
        string client,
        string status,
        string type,
        ImmutableArray<string> scopes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(status);
        ArgumentNullException.ThrowIfNull(type);

        if (scopes == null)
        {
            throw new ArgumentNullException(nameof(scopes));
        }

        return ExecuteAsync(cancellationToken);

        async IAsyncEnumerable<TAuthorization> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var authorizations = FindBySubjectAndSearchKey(subject, $"{client}#{status}#{type}", cancellationToken);
            
            await foreach (var authorization in authorizations)
            {
                if (Enumerable.All(scopes, scope => authorization.Scopes!.Contains(scope)))
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
            var search = _context.FromQueryAsync<TAuthorization>(new QueryOperationConfig
            {
                IndexName = "ApplicationId-index",
                KeyExpression = new Expression
                {
                    ExpressionStatement = "ApplicationId = :applicationId",
                    ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                    {
                        { ":applicationId", identifier },
                    }
                },
                Limit = 1,
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

        return await _context.LoadAsync<TAuthorization>(identifier, cancellationToken);
    }

    public IAsyncEnumerable<TAuthorization> FindBySubjectAsync(string subject, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subject);

        return ExecuteAsync(cancellationToken);

        async IAsyncEnumerable<TAuthorization> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var search = _context.FromQueryAsync<TAuthorization>(new QueryOperationConfig
            {
                IndexName = "Subject-index",
                KeyExpression = new Expression
                {
                    ExpressionStatement = "Subject = :subject",
                    ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                    {
                        { ":subject", subject },
                    }
                },
                Limit = 1,
            });

            var authorizations = await search.GetRemainingAsync(cancellationToken);

            foreach (var authorization in authorizations)
            {
                yield return authorization;
            }
        }
    }

    public ValueTask<string?> GetApplicationIdAsync(TAuthorization authorization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        return new(authorization.ApplicationId);
    }

    public ValueTask<TResult> GetAsync<TState, TResult>(Func<IQueryable<TAuthorization>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
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

    public ValueTask<ImmutableArray<string>> GetScopesAsync(TAuthorization authorization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        if (authorization.Scopes is not { Count: > 0 })
        {
            return new(ImmutableArray.Create<string>());
        }

        return new(authorization.Scopes.ToImmutableArray());
    }

    public ValueTask<string?> GetStatusAsync(TAuthorization authorization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        return new(authorization.Status);
    }

    public ValueTask<string?> GetSubjectAsync(TAuthorization authorization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        return new(authorization.Subject);
    }

    public ValueTask<string?> GetTypeAsync(TAuthorization authorization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        return new(authorization.Type);
    }

    public ValueTask<TAuthorization> InstantiateAsync(CancellationToken cancellationToken)
    {
        try
        {
            return new(Activator.CreateInstance<TAuthorization>());
        }
        catch (MemberAccessException exception)
        {
            return new(Task.FromException<TAuthorization>(
                new InvalidOperationException(OpenIddictResources.GetResourceString(OpenIddictResources.ID0240), exception)));
        }
    }

    public ConcurrentDictionary<int, string?> ListCursors { get; set; } = new ConcurrentDictionary<int, string?>();
    public IAsyncEnumerable<TAuthorization> ListAsync(int? count, int? offset, CancellationToken cancellationToken)
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
            var (token, items) = await DynamoDbUtils.Paginate<TAuthorization>(_client, count, initalToken, cancellationToken);

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

    public IAsyncEnumerable<TResult> ListAsync<TState, TResult>(Func<IQueryable<TAuthorization>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    // TODO: User DynamoDB expirations instead
    public async ValueTask PruneAsync(DateTimeOffset threshold, CancellationToken cancellationToken)
    {
        // Get all authorizations which is older than threshold
        var filter = new ScanFilter();
        filter.AddCondition("CreationDate", ScanOperator.LessThan, new List<AttributeValue>
        {
            new AttributeValue(threshold.UtcDateTime.ToString("o")),
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
            }
            else if (authorization.Type == AuthorizationTypes.AdHoc)
            {
                remainingAdHocAuthorizations.Add(authorization);
            }
        }

        // Add authorizations which is ad hoc and has no tokens
        foreach (var authorization in remainingAdHocAuthorizations)
        {
            var tokensQuery = _context.FromQueryAsync<OpenIddictDynamoDbToken>(new QueryOperationConfig
            {
                IndexName = "AuthorizationId-index",
                KeyExpression = new Expression
                {
                    ExpressionStatement = "AuthorizationId = :authorizationId",
                    ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                    {
                        { ":authorizationId", authorization.Id },
                    }
                },
                Limit = 1,
            });
            var tokens = await tokensQuery.GetRemainingAsync();

            if (tokens.Any() == false)
            {
                batchDelete.AddDeleteItem(authorization);
            }
        }

        await batchDelete.ExecuteAsync(cancellationToken);
    }

    public ValueTask SetApplicationIdAsync(TAuthorization authorization, string? identifier, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        authorization.ApplicationId = identifier;

        return default;
    }

    public ValueTask SetCreationDateAsync(TAuthorization authorization, DateTimeOffset? date, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        authorization.CreationDate = date?.UtcDateTime;

        return default;
    }

    public ValueTask SetPropertiesAsync(TAuthorization authorization, ImmutableDictionary<string, JsonElement> properties, CancellationToken cancellationToken)
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

    public ValueTask SetScopesAsync(TAuthorization authorization, ImmutableArray<string> scopes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        if (scopes.IsDefaultOrEmpty)
        {
            authorization.Scopes = null;

            return default;
        }

        authorization.Scopes = scopes.ToList();

        return default;
    }

    public ValueTask SetStatusAsync(TAuthorization authorization, string? status, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        authorization.Status = status;

        return default;
    }

    public ValueTask SetSubjectAsync(TAuthorization authorization, string? subject, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        authorization.Subject = subject;

        return default;
    }

    public ValueTask SetTypeAsync(TAuthorization authorization, string? type, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        authorization.Type = type;

        return default;
    }

    public async ValueTask UpdateAsync(TAuthorization authorization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        // Ensure no one else is updating
        var databaseApplication = await _context.LoadAsync<TAuthorization>(authorization.Id, cancellationToken);
        if (databaseApplication == default || databaseApplication.ConcurrencyToken != authorization.ConcurrencyToken)
        {
            throw new ArgumentException("Given authorization is invalid", nameof(authorization));
        }

        authorization.ConcurrencyToken = Guid.NewGuid().ToString();

        await _context.SaveAsync(authorization, cancellationToken);
    }
}