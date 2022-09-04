using System.Collections.Immutable;
using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Util;
using OpenIddict.Abstractions;

namespace OpenIddict.DynamoDB;

public class OpenIddictDynamoDbScopeStore<TScope> : IOpenIddictScopeStore<TScope>
    where TScope : OpenIddictDynamoDbScope
{
    private IAmazonDynamoDB _client;
    private IDynamoDBContext _context;

    public OpenIddictDynamoDbScopeStore(IAmazonDynamoDB client)
    {
        _client = client;
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

    public async ValueTask CreateAsync(TScope scope, CancellationToken cancellationToken)
    {
        if (scope == null)
        {
            throw new ArgumentNullException(nameof(scope));
        }

        cancellationToken.ThrowIfCancellationRequested();

        await _context.SaveAsync(scope, cancellationToken);
    }

    public async ValueTask DeleteAsync(TScope scope, CancellationToken cancellationToken)
    {
        if (scope == null)
        {
            throw new ArgumentNullException(nameof(scope));
        }

        cancellationToken.ThrowIfCancellationRequested();

        await _context.DeleteAsync(scope, cancellationToken);
    }

    public async ValueTask<TScope?> FindByIdAsync(string identifier, CancellationToken cancellationToken)
    {
        if (identifier == null)
        {
            throw new ArgumentNullException(nameof(identifier));
        }

        return await _context.LoadAsync<TScope>(identifier, cancellationToken);
    }

    public async ValueTask<TScope?> FindByNameAsync(string name, CancellationToken cancellationToken)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

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
        return scopes?.FirstOrDefault();
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
                yield return scope;
            }
        }
    }

    public IAsyncEnumerable<TScope> FindByResourceAsync(string resource, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<TResult> GetAsync<TState, TResult>(Func<IQueryable<TScope>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public ValueTask<string?> GetDescriptionAsync(TScope scope, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<ImmutableDictionary<CultureInfo, string>> GetDescriptionsAsync(TScope scope, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<string?> GetDisplayNameAsync(TScope scope, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<ImmutableDictionary<CultureInfo, string>> GetDisplayNamesAsync(TScope scope, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<string?> GetIdAsync(TScope scope, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<string?> GetNameAsync(TScope scope, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(TScope scope, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<ImmutableArray<string>> GetResourcesAsync(TScope scope, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<TScope> InstantiateAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<TScope> ListAsync(int? count, int? offset, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<TResult> ListAsync<TState, TResult>(Func<IQueryable<TScope>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public ValueTask SetDescriptionAsync(TScope scope, string? description, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask SetDescriptionsAsync(TScope scope, ImmutableDictionary<CultureInfo, string> descriptions, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask SetDisplayNameAsync(TScope scope, string? name, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask SetDisplayNamesAsync(TScope scope, ImmutableDictionary<CultureInfo, string> names, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask SetNameAsync(TScope scope, string? name, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask SetPropertiesAsync(TScope scope, ImmutableDictionary<string, JsonElement> properties, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask SetResourcesAsync(TScope scope, ImmutableArray<string> resources, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask UpdateAsync(TScope scope, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task EnsureInitializedAsync(
        string scopeTableName = Constants.DefaultScopeTableName)
    {
        if (_client == null)
        {
            throw new ArgumentNullException(nameof(_client));
        }

        if (_context == null)
        {
            throw new ArgumentNullException(nameof(_context));
        }

        if (scopeTableName != Constants.DefaultScopeTableName)
        {
            AWSConfigsDynamoDB.Context.AddAlias(new TableAlias(scopeTableName, Constants.DefaultScopeTableName));
        }

        return EnsureInitializedAsync(_client, scopeTableName);
    }

    private async Task EnsureInitializedAsync(IAmazonDynamoDB client, string scopeTableName)
    {
        var defaultProvisionThroughput = new ProvisionedThroughput
        {
            ReadCapacityUnits = 5,
            WriteCapacityUnits = 5
        };
        var scopeGlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
        {
            new GlobalSecondaryIndex
            {
                IndexName = "Name-index",
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement("ScopeName", KeyType.HASH),
                },
                ProvisionedThroughput = defaultProvisionThroughput,
                Projection = new Projection
                {
                    ProjectionType = ProjectionType.ALL,
                },
            },
        };

        var tableNames = await client.ListTablesAsync();

        if (!tableNames.TableNames.Contains(scopeTableName))
        {
            await CreateAuthorizationTableAsync(
                client, scopeTableName, defaultProvisionThroughput, scopeGlobalSecondaryIndexes);
        }
        else
        {
            await DynamoDbUtils.UpdateSecondaryIndexes(client, scopeTableName, scopeGlobalSecondaryIndexes);
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
                    AttributeName = "ScopeName",
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