using System.Collections.Immutable;
using System.Globalization;
using System.Net;
using System.Text.Json;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Util;
using OpenIddict.Abstractions;

namespace OpenIddict.DynamoDB;

public class OpenIddictDynamoDbApplicationStore<TApplication> : IOpenIddictApplicationStore<TApplication>
    where TApplication : OpenIddictDynamoDbApplication
{
    private IAmazonDynamoDB _client;
    private IDynamoDBContext _context;

    public OpenIddictDynamoDbApplicationStore(IAmazonDynamoDB client)
    {
        _client = client;
        _context = new DynamoDBContext(_client);
    }

    public async ValueTask<long> CountAsync(CancellationToken cancellationToken)
    {
        var client = new AmazonDynamoDBClient();
        var request = new DescribeTableRequest
        {
            TableName = Constants.DefaultApplicationTableName
        };
        var description = await _client.DescribeTableAsync(request);

        return description.Table.ItemCount;
    }

    public ValueTask<long> CountAsync<TResult>(Func<IQueryable<TApplication>, IQueryable<TResult>> query, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async ValueTask CreateAsync(TApplication application, CancellationToken cancellationToken)
    {
        if (application == null)
        {
            throw new ArgumentNullException(nameof(application));
        }

        cancellationToken.ThrowIfCancellationRequested();

        await _context.SaveAsync(application, cancellationToken);
    }

    public async ValueTask DeleteAsync(TApplication application, CancellationToken cancellationToken)
    {
        if (application == null)
        {
            throw new ArgumentNullException(nameof(application));
        }

        cancellationToken.ThrowIfCancellationRequested();

        await _context.DeleteAsync(application, cancellationToken);
    }

    public ValueTask<TApplication?> FindByClientIdAsync(string identifier, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<TApplication?> FindByIdAsync(string identifier, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<TApplication> FindByPostLogoutRedirectUriAsync(string address, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<TApplication> FindByRedirectUriAsync(string address, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<TResult> GetAsync<TState, TResult>(Func<IQueryable<TApplication>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<string?> GetClientIdAsync(TApplication application, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<string?> GetClientSecretAsync(TApplication application, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<string?> GetClientTypeAsync(TApplication application, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<string?> GetConsentTypeAsync(TApplication application, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<string?> GetDisplayNameAsync(TApplication application, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<ImmutableDictionary<CultureInfo, string>> GetDisplayNamesAsync(TApplication application, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<string?> GetIdAsync(TApplication application, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<ImmutableArray<string>> GetPermissionsAsync(TApplication application, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<ImmutableArray<string>> GetPostLogoutRedirectUrisAsync(TApplication application, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(TApplication application, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<ImmutableArray<string>> GetRedirectUrisAsync(TApplication application, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<ImmutableArray<string>> GetRequirementsAsync(TApplication application, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<TApplication> InstantiateAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<TApplication> ListAsync(int? count, int? offset, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<TResult> ListAsync<TState, TResult>(Func<IQueryable<TApplication>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask SetClientIdAsync(TApplication application, string? identifier, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask SetClientSecretAsync(TApplication application, string? secret, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask SetClientTypeAsync(TApplication application, string? type, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask SetConsentTypeAsync(TApplication application, string? type, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask SetDisplayNameAsync(TApplication application, string? name, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask SetDisplayNamesAsync(TApplication application, ImmutableDictionary<CultureInfo, string> names, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask SetPermissionsAsync(TApplication application, ImmutableArray<string> permissions, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask SetPostLogoutRedirectUrisAsync(TApplication application, ImmutableArray<string> addresses, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask SetPropertiesAsync(TApplication application, ImmutableDictionary<string, JsonElement> properties, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask SetRedirectUrisAsync(TApplication application, ImmutableArray<string> addresses, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask SetRequirementsAsync(TApplication application, ImmutableArray<string> requirements, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask UpdateAsync(TApplication application, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task EnsureInitializedAsync(string rolesTableName = Constants.DefaultApplicationTableName)
    {
        if (_client == null)
        {
            throw new ArgumentNullException(nameof(_client));
        }

        if (_context == null)
        {
            throw new ArgumentNullException(nameof(_context));
        }

        if (rolesTableName != Constants.DefaultApplicationTableName)
        {
            AWSConfigsDynamoDB.Context.AddAlias(new TableAlias(rolesTableName, Constants.DefaultApplicationTableName));
        }

        return EnsureInitializedAsync(_client, rolesTableName);
    }

    private async Task EnsureInitializedAsync(IAmazonDynamoDB client, string rolesTableName)
    {
        var defaultProvisionThroughput = new ProvisionedThroughput
        {
            ReadCapacityUnits = 5,
            WriteCapacityUnits = 5
        };

        var tableNames = await client.ListTablesAsync();

        if (!tableNames.TableNames.Contains(rolesTableName))
        {
            await CreateTableAsync(client, rolesTableName, defaultProvisionThroughput);
            return;
        }
    }

    private async Task CreateTableAsync(IAmazonDynamoDB client, string rolesTableName,
        ProvisionedThroughput provisionedThroughput, List<GlobalSecondaryIndex>? globalSecondaryIndexes = default)
    {
        var response = await client.CreateTableAsync(new CreateTableRequest
        {
            TableName = rolesTableName,
            ProvisionedThroughput = provisionedThroughput,
            KeySchema = new List<KeySchemaElement>
            {
                new KeySchemaElement
                {
                    AttributeName = "Id",
                    KeyType = KeyType.HASH
                }
            },
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new AttributeDefinition
                {
                    AttributeName = "Id",
                    AttributeType = ScalarAttributeType.S
                }
            },
            GlobalSecondaryIndexes = globalSecondaryIndexes
        });

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            throw new Exception($"Couldn't create table {rolesTableName}");
        }

        await DynamoUtils.WaitForActiveTableAsync(client, rolesTableName);
    }
}