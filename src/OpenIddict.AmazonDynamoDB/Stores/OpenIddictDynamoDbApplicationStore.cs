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

public class OpenIddictDynamoDbApplicationStore<TApplication> : IOpenIddictApplicationStore<TApplication>
    where TApplication : OpenIddictDynamoDbApplication
{
    private IAmazonDynamoDB _client;
    private IDynamoDBContext _context;
    private IOptionsMonitor<OpenIddictDynamoDbOptions> _optionsMonitor;
    private OpenIddictDynamoDbOptions _openIddictDynamoDbOptions => _optionsMonitor.CurrentValue;

    public OpenIddictDynamoDbApplicationStore(IOptionsMonitor<OpenIddictDynamoDbOptions> optionsMonitor)
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
            TableName = Constants.DefaultApplicationTableName,
        });

        return description.Table.ItemCount;
    }

    public ValueTask<long> CountAsync<TResult>(Func<IQueryable<TApplication>, IQueryable<TResult>> query, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public async ValueTask CreateAsync(TApplication application, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);

        await _context.SaveAsync(application, cancellationToken);
        await SaveRedirectUris(application, cancellationToken);
    }

    private async Task SaveRedirectUris(TApplication application, CancellationToken cancellationToken)
    {
        var batch = _context.CreateBatchWrite<OpenIddictDynamoDbApplicationRedirect>();

        if (application.RedirectUris?.Any() == true)
        {
            foreach (var redirectUri in application.RedirectUris)
            {
                var applicationRedirect = new OpenIddictDynamoDbApplicationRedirect
                {
                    RedirectUri = redirectUri,
                    RedirectType = RedirectType.RedirectUri,
                    ApplicationId = application.Id,
                };

                batch.AddPutItem(applicationRedirect);
            }
        }

        if (application.PostLogoutRedirectUris?.Any() == true)
        {
            foreach (var redirectUri in application.PostLogoutRedirectUris)
            {
                var applicationRedirect = new OpenIddictDynamoDbApplicationRedirect
                {
                    RedirectUri = redirectUri,
                    RedirectType = RedirectType.PostLogoutRedirectUri,
                    ApplicationId = application.Id,
                };

                batch.AddPutItem(applicationRedirect);
            }
        }

        await batch.ExecuteAsync(cancellationToken);
    }

    public async ValueTask DeleteAsync(TApplication application, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);

        await _context.DeleteAsync(application, cancellationToken);
    }

    public async ValueTask<TApplication?> FindByClientIdAsync(string identifier, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        var search = _context.FromQueryAsync<TApplication>(new QueryOperationConfig
        {
            IndexName = "ClientId-index",
            KeyExpression = new Expression
            {
                ExpressionStatement = "ClientId = :clientId",
                ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                {
                    { ":clientId", identifier },
                }
            },
            Limit = 1,
        });
        var applications = await search.GetRemainingAsync(cancellationToken);
        var application = applications?.FirstOrDefault();

        if (application != default)
        {
            await SetRedirectUris(application, cancellationToken);
        }

        return application;
    }

    public async ValueTask<TApplication?> FindByIdAsync(string identifier, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        var application = await _context.LoadAsync<TApplication>(identifier, cancellationToken);

        if (application != default)
        {
            await SetRedirectUris(application, cancellationToken);
        }

        return application;
    }

    private async Task SetRedirectUris(TApplication application, CancellationToken cancellationToken)
    {
        var applicationId = application.Id;
        var search = _context.FromQueryAsync<OpenIddictDynamoDbApplicationRedirect>(new QueryOperationConfig
        {
            IndexName = "ApplicationId-index",
            KeyExpression = new Expression
            {
                ExpressionStatement = "ApplicationId = :applicationId",
                ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                {
                    { ":applicationId", applicationId },
                }
            },
        });
        var applicationRedirects = await search.GetRemainingAsync(cancellationToken);
        application.RedirectUris = applicationRedirects
            .Where(x => x.RedirectType == RedirectType.RedirectUri)
            .Select(x => x.RedirectUri!)
            .ToList();
        application.PostLogoutRedirectUris = applicationRedirects
            .Where(x => x.RedirectType == RedirectType.PostLogoutRedirectUri)
            .Select(x => x.RedirectUri!)
            .ToList();
    }

    public IAsyncEnumerable<TApplication> FindByPostLogoutRedirectUriAsync(string address, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(address))
        {
            throw new ArgumentException(OpenIddictResources.GetResourceString(OpenIddictResources.ID0143), nameof(address));
        }

        return ExecuteAsync(cancellationToken);

        async IAsyncEnumerable<TApplication> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var applicationRedirect = await _context.LoadAsync<OpenIddictDynamoDbApplicationRedirect>(
                address, rangeKey: RedirectType.PostLogoutRedirectUri, cancellationToken);

            if (applicationRedirect != default)
            {
                var application = await FindByIdAsync(applicationRedirect.ApplicationId!, cancellationToken);

                if (application != default)
                {
                    await SetRedirectUris(application, cancellationToken);

                    yield return application;
                }
            }
        }
    }

    public IAsyncEnumerable<TApplication> FindByRedirectUriAsync(string address, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(address))
        {
            throw new ArgumentException(OpenIddictResources.GetResourceString(OpenIddictResources.ID0143), nameof(address));
        }

        return ExecuteAsync(cancellationToken);

        async IAsyncEnumerable<TApplication> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var applicationRedirect = await _context.LoadAsync<OpenIddictDynamoDbApplicationRedirect>(
                address, rangeKey: RedirectType.RedirectUri, cancellationToken);

            if (applicationRedirect != default)
            {
                var application = await FindByIdAsync(applicationRedirect.ApplicationId!, cancellationToken);

                if (application != default)
                {
                    await SetRedirectUris(application, cancellationToken);

                    yield return application;
                }
            }
        }
    }

    public ValueTask<TResult> GetAsync<TState, TResult>(Func<IQueryable<TApplication>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public ValueTask<string?> GetClientIdAsync(TApplication application, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);

        return new(application.ClientId);
    }

    public ValueTask<string?> GetClientSecretAsync(TApplication application, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);

        return new(application.ClientSecret);
    }

    public ValueTask<string?> GetClientTypeAsync(TApplication application, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);

        return new(application.Type);
    }

    public ValueTask<string?> GetConsentTypeAsync(TApplication application, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);

        return new(application.ConsentType);
    }

    public ValueTask<string?> GetDisplayNameAsync(TApplication application, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);

        return new(application.DisplayName);
    }

    public ValueTask<ImmutableDictionary<CultureInfo, string>> GetDisplayNamesAsync(TApplication application, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);

        if (application.DisplayNames is not { Count: > 0 })
        {
            return new(ImmutableDictionary.Create<CultureInfo, string>());
        }

        return new(application.DisplayNames.ToImmutableDictionary(
            pair => CultureInfo.GetCultureInfo(pair.Key),
            pair => pair.Value));
    }

    public ValueTask<string?> GetIdAsync(TApplication application, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);

        return new(application.Id.ToString());
    }

    public ValueTask<ImmutableArray<string>> GetPermissionsAsync(TApplication application, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);

        if (application.Permissions is not { Count: > 0 })
        {
            return new(ImmutableArray.Create<string>());
        }

        return new(application.Permissions.ToImmutableArray());
    }

    public ValueTask<ImmutableArray<string>> GetPostLogoutRedirectUrisAsync(TApplication application, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);

        if (application.PostLogoutRedirectUris is not { Count: > 0 })
        {
            return new(ImmutableArray.Create<string>());
        }

        return new(application.PostLogoutRedirectUris.ToImmutableArray());
    }

    public ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(TApplication application, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);

        if (string.IsNullOrEmpty(application.Properties))
        {
            return new(ImmutableDictionary.Create<string, JsonElement>());
        }

        using var document = JsonDocument.Parse(application.Properties);
        var properties = ImmutableDictionary.CreateBuilder<string, JsonElement>();

        foreach (var property in document.RootElement.EnumerateObject())
        {
            properties[property.Name] = property.Value.Clone();
        }

        return new(properties.ToImmutable());
    }

    public ValueTask<ImmutableArray<string>> GetRedirectUrisAsync(TApplication application, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);

        if (application.RedirectUris is not { Count: > 0 })
        {
            return new(ImmutableArray.Create<string>());
        }

        return new(application.RedirectUris.ToImmutableArray());
    }

    public ValueTask<ImmutableArray<string>> GetRequirementsAsync(TApplication application, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);

        if (application.Requirements is not { Count: > 0 })
        {
            return new(ImmutableArray.Create<string>());
        }

        return new(application.Requirements.ToImmutableArray());
    }

    public ValueTask<TApplication> InstantiateAsync(CancellationToken cancellationToken)
    {
        try
        {
            return new(Activator.CreateInstance<TApplication>());
        }
        catch (MemberAccessException exception)
        {
            return new(Task.FromException<TApplication>(
                new InvalidOperationException(OpenIddictResources.GetResourceString(OpenIddictResources.ID0240), exception)));
        }
    }

    public ConcurrentDictionary<int, string?> ListCursors { get; set; } = new ConcurrentDictionary<int, string?>();
    public IAsyncEnumerable<TApplication> ListAsync(int? count, int? offset, CancellationToken cancellationToken)
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

        async IAsyncEnumerable<TApplication> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var (token, items) = await DynamoDbUtils.Paginate<TApplication>(_client, count, initalToken, cancellationToken);

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

    public IAsyncEnumerable<TResult> ListAsync<TState, TResult>(Func<IQueryable<TApplication>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public ValueTask SetClientIdAsync(TApplication application, string? identifier, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);

        application.ClientId = identifier;

        return default;
    }

    public ValueTask SetClientSecretAsync(TApplication application, string? secret, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);

        application.ClientSecret = secret;

        return default;
    }

    public ValueTask SetClientTypeAsync(TApplication application, string? type, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);

        application.Type = type;

        return default;
    }

    public ValueTask SetConsentTypeAsync(TApplication application, string? type, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);

        application.ConsentType = type;

        return default;
    }

    public ValueTask SetDisplayNameAsync(TApplication application, string? name, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);

        application.DisplayName = name;

        return default;
    }

    public ValueTask SetDisplayNamesAsync(TApplication application, ImmutableDictionary<CultureInfo, string> names, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);

        if (names is not { Count: > 0 })
        {
            application.DisplayNames = null;

            return default;
        }

        application.DisplayNames = names.ToDictionary(x => x.Key.ToString(), x => x.Value);

        return default;
    }

    public ValueTask SetPermissionsAsync(TApplication application, ImmutableArray<string> permissions, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);

        if (permissions.IsDefaultOrEmpty)
        {
            application.Permissions = null;

            return default;
        }

        application.Permissions = permissions.ToList();

        return default;
    }

    public ValueTask SetPostLogoutRedirectUrisAsync(TApplication application, ImmutableArray<string> addresses, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);

        if (addresses.IsDefaultOrEmpty)
        {
            application.PostLogoutRedirectUris = null;

            return default;
        }

        application.PostLogoutRedirectUris = addresses.ToList();

        return default;
    }

    public ValueTask SetPropertiesAsync(TApplication application, ImmutableDictionary<string, JsonElement> properties, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);

        if (properties is not { Count: > 0 })
        {
            application.Properties = null;

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

        application.Properties = Encoding.UTF8.GetString(stream.ToArray());

        return default;
    }

    public ValueTask SetRedirectUrisAsync(TApplication application, ImmutableArray<string> addresses, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);

        if (addresses.IsDefaultOrEmpty)
        {
            application.RedirectUris = null;

            return default;
        }

        application.RedirectUris = addresses.ToList();

        return default;
    }

    public ValueTask SetRequirementsAsync(TApplication application, ImmutableArray<string> requirements, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);

        if (requirements.IsDefaultOrEmpty)
        {
            application.Requirements = null;

            return default;
        }

        application.Requirements = requirements.ToList();

        return default;
    }

    public async ValueTask UpdateAsync(TApplication application, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);

        // Ensure no one else is updating
        var databaseApplication = await _context.LoadAsync<TApplication>(application.Id, cancellationToken);
        if (databaseApplication == default || databaseApplication.ConcurrencyToken != application.ConcurrencyToken)
        {
            throw new ArgumentException("Given application is invalid", nameof(application));
        }

        application.ConcurrencyToken = Guid.NewGuid().ToString();

        await _context.SaveAsync(application, cancellationToken);

        // Update application redirects
        // Fetch all redirects
        var applicationId = application.Id;
        var search = _context.FromQueryAsync<OpenIddictDynamoDbApplicationRedirect>(new QueryOperationConfig
        {
            IndexName = "ApplicationId-index",
            KeyExpression = new Expression
            {
                ExpressionStatement = "ApplicationId = :applicationId",
                ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                {
                    { ":applicationId", applicationId },
                }
            },
        });
        var applicationRedirects = await search.GetRemainingAsync(cancellationToken);

        // Remove previously stored redirects
        if (applicationRedirects.Any())
        {
            var writeRequests = applicationRedirects
                .Select(x => new WriteRequest
                {
                    DeleteRequest = new DeleteRequest
                    {
                        Key = new Dictionary<string, AttributeValue>
                        {
                            { "RedirectUri", new AttributeValue { S = x.RedirectUri } },
                            { "RedirectType", new AttributeValue { N = ((int)x.RedirectType).ToString() } },
                        },
                    },
                })
                .ToList();

            var request = new BatchWriteItemRequest
            {
                RequestItems = new Dictionary<string, List<WriteRequest>>
                {
                    { Constants.DefaultApplicationRedirectsTableName, writeRequests },
                },
            };

            await _client.BatchWriteItemAsync(request, cancellationToken);
        }

        // Save current redirects
        await SaveRedirectUris(application, cancellationToken);
    }
}