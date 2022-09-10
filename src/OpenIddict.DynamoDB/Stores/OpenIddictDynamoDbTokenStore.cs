using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Util;
using OpenIddict.Abstractions;

namespace OpenIddict.DynamoDB;

public class OpenIddictDynamoDbTokenStore<TToken> : IOpenIddictTokenStore<TToken>
    where TToken : OpenIddictDynamoDbToken
{
    private IAmazonDynamoDB _client;
    private IDynamoDBContext _context;

    public OpenIddictDynamoDbTokenStore(IAmazonDynamoDB client)
    {
        _client = client;
        _context = new DynamoDBContext(_client);
    }

    public async ValueTask<long> CountAsync(CancellationToken cancellationToken)
    {
        var description = await _client.DescribeTableAsync(new DescribeTableRequest
        {
            TableName = Constants.DefaultTokenTableName,
        });

        return description.Table.ItemCount;
    }

    public ValueTask<long> CountAsync<TResult>(Func<IQueryable<TToken>, IQueryable<TResult>> query, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public async ValueTask CreateAsync(TToken token, CancellationToken cancellationToken)
    {
        if (token == null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        cancellationToken.ThrowIfCancellationRequested();

        await _context.SaveAsync(token, cancellationToken);
    }

    public async ValueTask DeleteAsync(TToken token, CancellationToken cancellationToken)
    {
        if (token == null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        cancellationToken.ThrowIfCancellationRequested();

        await _context.DeleteAsync(token, cancellationToken);
    }

    private IAsyncEnumerable<TToken> FindBySubjectAndSearchKey(string subject, string searchKey, CancellationToken cancellationToken)
    {
        return ExecuteAsync(cancellationToken);

        async IAsyncEnumerable<TToken> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var search = _context.FromQueryAsync<TToken>(new QueryOperationConfig
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

            var tokens = await search.GetRemainingAsync(cancellationToken);

            foreach (var token in tokens)
            {
                yield return token;
            }
        }
    }

    public IAsyncEnumerable<TToken> FindAsync(string subject, string client, CancellationToken cancellationToken)
    {
        if (subject == null)
        {
            throw new ArgumentNullException(nameof(subject));
        }

        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        return FindBySubjectAndSearchKey(subject, client, cancellationToken);
    }

    public IAsyncEnumerable<TToken> FindAsync(string subject, string client, string status, CancellationToken cancellationToken)
    {
        if (subject == null)
        {
            throw new ArgumentNullException(nameof(subject));
        }

        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        if (status == null)
        {
            throw new ArgumentNullException(nameof(status));
        }

        return FindBySubjectAndSearchKey(subject, $"{client}#{status}", cancellationToken);
    }

    public IAsyncEnumerable<TToken> FindAsync(string subject, string client, string status, string type, CancellationToken cancellationToken)
    {
        if (subject == null)
        {
            throw new ArgumentNullException(nameof(subject));
        }

        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        if (status == null)
        {
            throw new ArgumentNullException(nameof(status));
        }

        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return FindBySubjectAndSearchKey(subject, $"{client}#{status}#{type}", cancellationToken);
    }

    public IAsyncEnumerable<TToken> FindByApplicationIdAsync(string identifier, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<TToken> FindByAuthorizationIdAsync(string identifier, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async ValueTask<TToken?> FindByIdAsync(string identifier, CancellationToken cancellationToken)
    {
        if (identifier == null)
        {
            throw new ArgumentNullException(nameof(identifier));
        }

        return await _context.LoadAsync<TToken>(identifier, cancellationToken);
    }

    public ValueTask<TToken?> FindByReferenceIdAsync(string identifier, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<TToken> FindBySubjectAsync(string subject, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<string?> GetApplicationIdAsync(TToken token, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        return new(token.ApplicationId);
    }

    public ValueTask<TResult> GetAsync<TState, TResult>(Func<IQueryable<TToken>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public ValueTask<string?> GetAuthorizationIdAsync(TToken token, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        return new(token.AuthorizationId);
    }

    public ValueTask<DateTimeOffset?> GetCreationDateAsync(TToken token, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        return new(token.CreationDate);
    }

    public ValueTask<DateTimeOffset?> GetExpirationDateAsync(TToken token, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        return new(token.ExpirationDate);
    }

    public ValueTask<string?> GetIdAsync(TToken token, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        return new(token.Id);
    }

    public ValueTask<string?> GetPayloadAsync(TToken token, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        return new(token.Payload);
    }

    public ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(TToken token, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

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
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        return new(token.RedemptionDate);
    }

    public ValueTask<string?> GetReferenceIdAsync(TToken token, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        return new(token.ReferenceId);
    }

    public ValueTask<string?> GetStatusAsync(TToken token, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        return new(token.Status);
    }

    public ValueTask<string?> GetSubjectAsync(TToken token, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        return new(token.Subject);
    }

    public ValueTask<string?> GetTypeAsync(TToken token, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        return new(token.Type);
    }

    public ValueTask<TToken> InstantiateAsync(CancellationToken cancellationToken)
    {
        try
        {
            return new(Activator.CreateInstance<TToken>());
        }
        catch (MemberAccessException exception)
        {
            return new(Task.FromException<TToken>(
                new InvalidOperationException(OpenIddictResources.GetResourceString(OpenIddictResources.ID0248), exception)));
        }
    }

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
            var (token, items) = await DynamoDbUtils.Paginate<TToken>(_client, count, initalToken, cancellationToken);

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

    public ValueTask PruneAsync(DateTimeOffset threshold, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask SetApplicationIdAsync(TToken token, string? identifier, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        token.ApplicationId = identifier;

        return default;
    }

    public ValueTask SetAuthorizationIdAsync(TToken token, string? identifier, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        token.AuthorizationId = identifier;

        return default;
    }

    public ValueTask SetCreationDateAsync(TToken token, DateTimeOffset? date, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        token.CreationDate = date?.UtcDateTime;

        return default;
    }

    public ValueTask SetExpirationDateAsync(TToken token, DateTimeOffset? date, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        token.ExpirationDate = date?.UtcDateTime;

        return default;
    }

    public ValueTask SetPayloadAsync(TToken token, string? payload, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        token.Payload = payload;

        return default;
    }

    public ValueTask SetPropertiesAsync(TToken token, ImmutableDictionary<string, JsonElement> properties, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

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
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        token.RedemptionDate = date?.UtcDateTime;

        return default;
    }

    public ValueTask SetReferenceIdAsync(TToken token, string? identifier, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        token.ReferenceId = identifier;

        return default;
    }

    public ValueTask SetStatusAsync(TToken token, string? status, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        token.Status = status;

        return default;
    }

    public ValueTask SetSubjectAsync(TToken token, string? subject, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        token.Subject = subject;

        return default;
    }

    public ValueTask SetTypeAsync(TToken token, string? type, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        token.Type = type;

        return default;
    }

    public async ValueTask UpdateAsync(TToken token, CancellationToken cancellationToken)
    {
        if (token == null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        // Ensure no one else is updating
        var databaseApplication = await _context.LoadAsync<TToken>(token.Id, cancellationToken);
        if (databaseApplication == default || databaseApplication.ConcurrencyToken != token.ConcurrencyToken)
        {
            throw new ArgumentException("Given token is invalid", nameof(token));
        }

        token.ConcurrencyToken = Guid.NewGuid().ToString();

        await _context.SaveAsync(token, cancellationToken);
    }

    public Task EnsureInitializedAsync(
        string tokenTableName = Constants.DefaultTokenTableName)
    {
        if (_client == null)
        {
            throw new ArgumentNullException(nameof(_client));
        }

        if (_context == null)
        {
            throw new ArgumentNullException(nameof(_context));
        }

        if (tokenTableName != Constants.DefaultTokenTableName)
        {
            AWSConfigsDynamoDB.Context.AddAlias(new TableAlias(tokenTableName, Constants.DefaultTokenTableName));
        }

        return EnsureInitializedAsync(_client, tokenTableName);
    }

    private async Task EnsureInitializedAsync(IAmazonDynamoDB client, string tokenTableName)
    {
        var defaultProvisionThroughput = new ProvisionedThroughput
        {
            ReadCapacityUnits = 5,
            WriteCapacityUnits = 5
        };
        var tokenGlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
        {
            new GlobalSecondaryIndex
            {
                IndexName = "Subject-SearchKey-index",
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement("Subject", KeyType.HASH),
                    new KeySchemaElement("SearchKey", KeyType.RANGE),
                },
                ProvisionedThroughput = defaultProvisionThroughput,
                Projection = new Projection
                {
                    ProjectionType = ProjectionType.ALL,
                },
            },
        };

        var tableNames = await client.ListTablesAsync();

        if (!tableNames.TableNames.Contains(tokenTableName))
        {
            await CreateTokenTableAsync(
                client, tokenTableName, defaultProvisionThroughput, tokenGlobalSecondaryIndexes);
        }
        else
        {
            await DynamoDbUtils.UpdateSecondaryIndexes(client, tokenTableName, tokenGlobalSecondaryIndexes);
        }
    }

    private async Task CreateTokenTableAsync(IAmazonDynamoDB client, string tableName,
        ProvisionedThroughput provisionedThroughput, List<GlobalSecondaryIndex>? globalSecondaryIndexes = default)
    {
        var response = await client.CreateTableAsync(new CreateTableRequest
        {
            TableName = tableName,
            ProvisionedThroughput = provisionedThroughput,
            KeySchema = new List<KeySchemaElement>
            {
                new KeySchemaElement
                {
                    AttributeName = "Id",
                    KeyType = KeyType.HASH,
                },
            },
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new AttributeDefinition
                {
                    AttributeName = "Id",
                    AttributeType = ScalarAttributeType.S,
                },
                // new AttributeDefinition
                // {
                //     AttributeName = "ApplicationId",
                //     AttributeType = ScalarAttributeType.S,
                // },
                new AttributeDefinition
                {
                    AttributeName = "Subject",
                    AttributeType = ScalarAttributeType.S,
                },
                // new AttributeDefinition
                // {
                //     AttributeName = "AuthorizationId",
                //     AttributeType = ScalarAttributeType.S,
                // },
                // new AttributeDefinition
                // {
                //     AttributeName = "ReferenceId",
                //     AttributeType = ScalarAttributeType.S,
                // },
                new AttributeDefinition
                {
                    AttributeName = "SearchKey",
                    AttributeType = ScalarAttributeType.S,
                },
            },
            GlobalSecondaryIndexes = globalSecondaryIndexes,
        });

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            throw new Exception($"Couldn't create table {tableName}");
        }

        await DynamoDbUtils.WaitForActiveTableAsync(client, tableName);
    }
}