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

public class OpenIddictDynamoDbAuthorizationStore<TAuthorization> : IOpenIddictAuthorizationStore<TAuthorization>
    where TAuthorization : OpenIddictDynamoDbAuthorization
{
    private IAmazonDynamoDB _client;
    private IDynamoDBContext _context;

    public OpenIddictDynamoDbAuthorizationStore(IAmazonDynamoDB client)
    {
        _client = client;
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
        if (authorization == null)
        {
            throw new ArgumentNullException(nameof(authorization));
        }

        cancellationToken.ThrowIfCancellationRequested();

        await _context.SaveAsync(authorization, cancellationToken);
    }

    public async ValueTask DeleteAsync(TAuthorization authorization, CancellationToken cancellationToken)
    {
        if (authorization == null)
        {
            throw new ArgumentNullException(nameof(authorization));
        }

        cancellationToken.ThrowIfCancellationRequested();

        await _context.DeleteAsync(authorization, cancellationToken);
    }

    public IAsyncEnumerable<TAuthorization> FindAsync(string subject, string client, CancellationToken cancellationToken)
    {
        if (subject == null)
        {
            throw new ArgumentNullException(nameof(subject));
        }

        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        return ExecuteAsync(cancellationToken);

        async IAsyncEnumerable<TAuthorization> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var search = _context.FromQueryAsync<TAuthorization>(new QueryOperationConfig
            {
                IndexName = "ApplicationId-Subject-index",
                KeyExpression = new Expression
                {
                    ExpressionStatement = "Subject = :subject, ApplicationId = :applicationId",
                    ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                    {
                        { ":subject", subject },
                        { ":applicationId", client },
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

    public IAsyncEnumerable<TAuthorization> FindAsync(string subject, string client, string status, CancellationToken cancellationToken)
    {
        if (status == null)
        {
            throw new ArgumentNullException(nameof(status));
        }

        return ExecuteAsync(cancellationToken);

        async IAsyncEnumerable<TAuthorization> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var authorizations = FindAsync(subject, client, cancellationToken);
            
            await foreach (var authorization in authorizations)
            {
                if (authorization.Status == status)
                {
                    yield return authorization;
                }
            }
        }
    }

    public IAsyncEnumerable<TAuthorization> FindAsync(string subject, string client, string status, string type, CancellationToken cancellationToken)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return ExecuteAsync(cancellationToken);

        async IAsyncEnumerable<TAuthorization> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var authorizations = FindAsync(subject, client, cancellationToken);
            
            await foreach (var authorization in authorizations)
            {
                if (authorization.Status == status && authorization.Type == type)
                {
                    yield return authorization;
                }
            }
        }
    }

    public IAsyncEnumerable<TAuthorization> FindAsync(string subject, string client, string status, string type, ImmutableArray<string> scopes, CancellationToken cancellationToken)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return ExecuteAsync(cancellationToken);

        async IAsyncEnumerable<TAuthorization> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var authorizations = FindAsync(subject, client, cancellationToken);
            
            await foreach (var authorization in authorizations)
            {
                if (authorization.Status == status && authorization.Type == type
                    && Enumerable.All(scopes, scope => authorization.Scopes!.Contains(scope)))
                {
                    yield return authorization;
                }
            }
        }
    }

    public IAsyncEnumerable<TAuthorization> FindByApplicationIdAsync(string identifier, CancellationToken cancellationToken)
    {
        if (identifier == null)
        {
            throw new ArgumentNullException(nameof(identifier));
        }

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
        if (identifier == null)
        {
            throw new ArgumentNullException(nameof(identifier));
        }

        return await _context.LoadAsync<TAuthorization>(identifier, cancellationToken);
    }

    public IAsyncEnumerable<TAuthorization> FindBySubjectAsync(string subject, CancellationToken cancellationToken)
    {
        if (subject == null)
        {
            throw new ArgumentNullException(nameof(subject));
        }

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
        if (authorization is null)
        {
            throw new ArgumentNullException(nameof(authorization));
        }

        return new(authorization.ApplicationId);
    }

    public ValueTask<TResult> GetAsync<TState, TResult>(Func<IQueryable<TAuthorization>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public ValueTask<DateTimeOffset?> GetCreationDateAsync(TAuthorization authorization, CancellationToken cancellationToken)
    {
        if (authorization is null)
        {
            throw new ArgumentNullException(nameof(authorization));
        }

        return new(authorization.CreationDate);
    }

    public ValueTask<string?> GetIdAsync(TAuthorization authorization, CancellationToken cancellationToken)
    {
        if (authorization is null)
        {
            throw new ArgumentNullException(nameof(authorization));
        }

        return new(authorization.Id);
    }

    public ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(TAuthorization authorization, CancellationToken cancellationToken)
    {
        if (authorization is null)
        {
            throw new ArgumentNullException(nameof(authorization));
        }

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
        if (authorization is null)
        {
            throw new ArgumentNullException(nameof(authorization));
        }

        if (authorization.Scopes is not { Count: > 0 })
        {
            return new(ImmutableArray.Create<string>());
        }

        return new(authorization.Scopes.ToImmutableArray());
    }

    public ValueTask<string?> GetStatusAsync(TAuthorization authorization, CancellationToken cancellationToken)
    {
        if (authorization is null)
        {
            throw new ArgumentNullException(nameof(authorization));
        }

        return new(authorization.Status);
    }

    public ValueTask<string?> GetSubjectAsync(TAuthorization authorization, CancellationToken cancellationToken)
    {
        if (authorization is null)
        {
            throw new ArgumentNullException(nameof(authorization));
        }

        return new(authorization.Subject);
    }

    public ValueTask<string?> GetTypeAsync(TAuthorization authorization, CancellationToken cancellationToken)
    {
        if (authorization is null)
        {
            throw new ArgumentNullException(nameof(authorization));
        }

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

    public IAsyncEnumerable<TAuthorization> ListAsync(int? count, int? offset, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<TResult> ListAsync<TState, TResult>(Func<IQueryable<TAuthorization>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public ValueTask PruneAsync(DateTimeOffset threshold, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask SetApplicationIdAsync(TAuthorization authorization, string? identifier, CancellationToken cancellationToken)
    {
        if (authorization == null)
        {
            throw new ArgumentNullException(nameof(authorization));
        }

        authorization.ApplicationId = identifier;

        return default;
    }

    public ValueTask SetCreationDateAsync(TAuthorization authorization, DateTimeOffset? date, CancellationToken cancellationToken)
    {
        if (authorization == null)
        {
            throw new ArgumentNullException(nameof(authorization));
        }

        authorization.CreationDate = date?.UtcDateTime;

        return default;
    }

    public ValueTask SetPropertiesAsync(TAuthorization authorization, ImmutableDictionary<string, JsonElement> properties, CancellationToken cancellationToken)
    {
        if (authorization is null)
        {
            throw new ArgumentNullException(nameof(authorization));
        }

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
        if (authorization is null)
        {
            throw new ArgumentNullException(nameof(authorization));
        }

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
        if (authorization is null)
        {
            throw new ArgumentNullException(nameof(authorization));
        }

        authorization.Status = status;

        return default;
    }

    public ValueTask SetSubjectAsync(TAuthorization authorization, string? subject, CancellationToken cancellationToken)
    {
        if (authorization is null)
        {
            throw new ArgumentNullException(nameof(authorization));
        }

        authorization.Subject = subject;

        return default;
    }

    public ValueTask SetTypeAsync(TAuthorization authorization, string? type, CancellationToken cancellationToken)
    {
        if (authorization is null)
        {
            throw new ArgumentNullException(nameof(authorization));
        }

        authorization.Type = type;

        return default;
    }

    public async ValueTask UpdateAsync(TAuthorization authorization, CancellationToken cancellationToken)
    {
        if (authorization == null)
        {
            throw new ArgumentNullException(nameof(authorization));
        }

        // Ensure no one else is updating
        var databaseApplication = await _context.LoadAsync<TAuthorization>(authorization.Id, cancellationToken);
        if (databaseApplication == default || databaseApplication.ConcurrencyToken != authorization.ConcurrencyToken)
        {
            throw new ArgumentException("Given authorization is invalid", nameof(authorization));
        }

        authorization.ConcurrencyToken = Guid.NewGuid().ToString();

        await _context.SaveAsync(authorization, cancellationToken);
    }

    public Task EnsureInitializedAsync(
        string authorizationTableName = Constants.DefaultAuthorizationTableName)
    {
        if (_client == null)
        {
            throw new ArgumentNullException(nameof(_client));
        }

        if (_context == null)
        {
            throw new ArgumentNullException(nameof(_context));
        }

        if (authorizationTableName != Constants.DefaultAuthorizationTableName)
        {
            AWSConfigsDynamoDB.Context.AddAlias(new TableAlias(authorizationTableName, Constants.DefaultAuthorizationTableName));
        }

        return EnsureInitializedAsync(_client, authorizationTableName);
    }

    private async Task EnsureInitializedAsync(IAmazonDynamoDB client, string authorizationTableName)
    {
        var defaultProvisionThroughput = new ProvisionedThroughput
        {
            ReadCapacityUnits = 5,
            WriteCapacityUnits = 5
        };
        var authorizationGlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
        {
            new GlobalSecondaryIndex
            {
                IndexName = "ApplicationId-index",
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement("ApplicationId", KeyType.HASH),
                },
                ProvisionedThroughput = defaultProvisionThroughput,
                Projection = new Projection
                {
                    ProjectionType = ProjectionType.ALL,
                },
            },
            new GlobalSecondaryIndex
            {
                IndexName = "Subject-index",
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement("Subject", KeyType.HASH),
                },
                ProvisionedThroughput = defaultProvisionThroughput,
                Projection = new Projection
                {
                    ProjectionType = ProjectionType.ALL,
                },
            },
            new GlobalSecondaryIndex
            {
                IndexName = "ApplicationId-Subject-index",
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement("ApplicationId", KeyType.HASH),
                    new KeySchemaElement("Subject", KeyType.RANGE),
                },
                ProvisionedThroughput = defaultProvisionThroughput,
                Projection = new Projection
                {
                    ProjectionType = ProjectionType.ALL,
                },
            },
        };

        var tableNames = await client.ListTablesAsync();

        if (!tableNames.TableNames.Contains(authorizationTableName))
        {
            await CreateAuthorizationTableAsync(
                client, authorizationTableName, defaultProvisionThroughput, authorizationGlobalSecondaryIndexes);
        }
        else
        {
            await DynamoDbUtils.UpdateSecondaryIndexes(client, authorizationTableName, authorizationGlobalSecondaryIndexes);
        }
    }

    private async Task CreateAuthorizationTableAsync(IAmazonDynamoDB client, string tableName,
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
                new AttributeDefinition
                {
                    AttributeName = "ApplicationId",
                    AttributeType = ScalarAttributeType.S,
                },
                new AttributeDefinition
                {
                    AttributeName = "Subject",
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