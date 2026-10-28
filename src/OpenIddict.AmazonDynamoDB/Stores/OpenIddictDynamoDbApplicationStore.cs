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
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;

namespace OpenIddict.AmazonDynamoDB;

public class OpenIddictDynamoDbApplicationStore<TApplication> : IOpenIddictApplicationStore<TApplication>
  where TApplication : OpenIddictDynamoDbApplication, new()
{
  private readonly IAmazonDynamoDB _client;
  private readonly string _tableName;
  private const string RedirectSortKeyPrefix = "REDIRECT#";

  public OpenIddictDynamoDbApplicationStore(
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
    => await DynamoDbUtils.GetCountAsync(_client, _tableName, CountType.Application, cancellationToken);

  public ValueTask<long> CountAsync<TResult>(Func<IQueryable<TApplication>, IQueryable<TResult>> query, CancellationToken cancellationToken)
  {
    throw new NotSupportedException();
  }

  // The application and the lookup of its client id are written together, so a client id that
  // another application holds fails the whole write
  public async ValueTask CreateAsync(TApplication application, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(application);

    var items = new List<TransactWriteItem>
    {
      DynamoDbItems.TransactPut(_tableName, application, "attribute_not_exists(PartitionKey)"),
    };

    if (application.ClientId is { } clientId)
    {
      items.Add(DynamoDbItems.TransactPut(
        _tableName, new LookupItem(Lookups.ClientId(clientId), application.Id), "attribute_not_exists(PartitionKey)"));
    }

    try
    {
      await DynamoDbItems.TransactWriteAsync(_client, items, cancellationToken);
    }
    catch (TransactionCanceledException exception)
    {
      throw DynamoDbItems.FailedConditions(exception).Contains(1)
        ? DuplicateClientId(exception)
        : new InvalidOperationException($"An application with the id {application.Id} already exists", exception);
    }

    application.StoredClientId = application.ClientId;
    await SaveRedirectUris(application, cancellationToken);

    await DynamoDbUtils.UpdateCountAsync(_client, _tableName, CountType.Application, 1, cancellationToken);
  }

  private static OpenIddictExceptions.ValidationException DuplicateClientId(Exception exception)
  {
    var message = OpenIddictResources.GetResourceString(OpenIddictResources.ID2111);
    return new OpenIddictExceptions.ValidationException(message, [new System.ComponentModel.DataAnnotations.ValidationResult(message)], exception);
  }

  // An application is listed under each of its redirect URIs, which is how it is found by one
  private static MemberItem Member(OpenIddictDynamoDbApplicationRedirect redirect) => new(
    Lookups.Redirect(redirect.RedirectType, redirect.RedirectUri!), Lookups.ApplicationMember(redirect.ApplicationId!), redirect.ApplicationId!);

  private static IEnumerable<OpenIddictDynamoDbApplicationRedirect> Redirects(TApplication application)
  {
    foreach (var redirectUri in application.RedirectUris ?? [])
    {
      yield return new OpenIddictDynamoDbApplicationRedirect
      {
        RedirectUri = redirectUri,
        RedirectType = RedirectType.RedirectUri,
        ApplicationId = application.Id,
      };
    }

    foreach (var redirectUri in application.PostLogoutRedirectUris ?? [])
    {
      yield return new OpenIddictDynamoDbApplicationRedirect
      {
        RedirectUri = redirectUri,
        RedirectType = RedirectType.PostLogoutRedirectUri,
        ApplicationId = application.Id,
      };
    }
  }

  private Task SaveRedirectUris(TApplication application, CancellationToken cancellationToken)
    => DynamoDbUtils.BatchWriteAsync(
      _client,
      _tableName,
      Redirects(application).SelectMany(x => new IDynamoDbItem[] { x, Member(x) }).Select(DynamoDbItems.Put),
      cancellationToken);

  // Removes the application together with its redirects, which share its partition, and the
  // lookups that point into it
  public async ValueTask DeleteAsync(TApplication application, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(application);

    var requests = new List<WriteRequest>();

    foreach (var item in await DynamoDbItems.QueryPartitionAsync<RawItem>(_client, _tableName, application.PartitionKey, default, cancellationToken, consistentRead: true))
    {
      var sortKey = item.SortKey ?? "";

      if (sortKey.StartsWith(RedirectSortKeyPrefix, StringComparison.Ordinal))
      {
        requests.Add(DynamoDbItems.Delete(Member(DynamoDbItems.FromItem<OpenIddictDynamoDbApplicationRedirect>(item.Attributes))));
      }
      else if (item.Attributes.GetString("ClientId") is { } clientId)
      {
        requests.Add(new WriteRequest(new DeleteRequest(DynamoDbItems.Key(Lookups.ClientId(clientId), Lookups.SortKey))));
      }

      requests.Add(new WriteRequest(new DeleteRequest(DynamoDbItems.Key(item.PartitionKey, item.SortKey))));
    }

    await DynamoDbUtils.BatchWriteAsync(_client, _tableName, requests, cancellationToken);

    await DynamoDbUtils.UpdateCountAsync(_client, _tableName, CountType.Application, -1, cancellationToken);
  }

  public async ValueTask<TApplication?> FindByClientIdAsync(string identifier, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(identifier);

    var lookup = await DynamoDbItems.GetAsync<LookupItem>(
      _client, _tableName, Lookups.ClientId(identifier), Lookups.SortKey, cancellationToken, consistentRead: true);

    return lookup is null ? default : await FindByIdAsync(lookup.Id, cancellationToken);
  }

  public async ValueTask<TApplication?> FindByIdAsync(
    string identifier, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(identifier);

    var application = new TApplication
    {
      Id = identifier,
    };
    // A stale application would fail to update
    return await DynamoDbItems.GetAsync<TApplication>(
      _client, _tableName, application.PartitionKey, application.SortKey, cancellationToken, consistentRead: true);
  }

  public IAsyncEnumerable<TApplication> FindByPostLogoutRedirectUriAsync(string address, CancellationToken cancellationToken)
  {
    if (string.IsNullOrEmpty(address))
    {
      throw new ArgumentException(OpenIddictResources.GetResourceString(OpenIddictResources.ID0143), nameof(address));
    }

    return FindByRedirectType(address, RedirectType.PostLogoutRedirectUri, cancellationToken);
  }

  public IAsyncEnumerable<TApplication> FindByRedirectUriAsync(string address, CancellationToken cancellationToken)
  {
    if (string.IsNullOrEmpty(address))
    {
      throw new ArgumentException(OpenIddictResources.GetResourceString(OpenIddictResources.ID0143), nameof(address));
    }

    return FindByRedirectType(address, RedirectType.RedirectUri, cancellationToken);
  }

  private IAsyncEnumerable<TApplication> FindByRedirectType(
    string address, RedirectType redirectType, CancellationToken cancellationToken)
  {
    return ExecuteAsync(cancellationToken);

    async IAsyncEnumerable<TApplication> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
      var members = await DynamoDbItems.QueryPartitionAsync<MemberItem>(
        _client, _tableName, Lookups.Redirect(redirectType, address), default, cancellationToken, consistentRead: true);

      var keys = members
        .Select(x => x.Id)
        .Distinct()
        .Select(x => new TApplication { Id = x })
        .Select(DynamoDbItems.Key);

      foreach (var application in await DynamoDbItems.BatchGetAsync<TApplication>(_client, _tableName, keys, cancellationToken))
      {
        yield return application;
      }
    }
  }

  public ValueTask<TResult?> GetAsync<TState, TResult>(Func<IQueryable<TApplication>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
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

  public ValueTask<string?> GetApplicationTypeAsync(TApplication application, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(application);

    return new(application.ApplicationType);
  }

  public ValueTask<JsonWebKeySet?> GetJsonWebKeySetAsync(TApplication application, CancellationToken cancellationToken)
  {
    if (application is null)
    {
      throw new ArgumentNullException(nameof(application));
    }

    if (application.JsonWebKeySet is null)
    {
      return new(result: null);
    }

    return new(JsonWebKeySet.Create(application.JsonWebKeySet));
  }

  public ValueTask<ImmutableDictionary<string, string>> GetSettingsAsync(TApplication application, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(application);

    if (application.Settings is not { Count: > 0 })
    {
      return new(ImmutableDictionary.Create<string, string>());
    }

    return new(application.Settings.ToImmutableDictionary(
      pair => pair.Key,
      pair => pair.Value));
  }

  public ValueTask<TApplication> InstantiateAsync(CancellationToken cancellationToken)
    => new(new TApplication());

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
      var (token, items) = await DynamoDbUtils.Paginate<TApplication>(
        _client, _tableName, "APPLICATION#", "#USER#", count, initalToken, cancellationToken);

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
    using var writer = new Utf8JsonWriter(stream, new()
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

  public ValueTask SetApplicationTypeAsync(TApplication application, string? type, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(application);

    application.ApplicationType = type;

    return default;
  }

  public ValueTask SetJsonWebKeySetAsync(TApplication application, JsonWebKeySet? set, CancellationToken cancellationToken)
  {
    if (application is null)
    {
      throw new ArgumentNullException(nameof(application));
    }

    // OpenIddict's source-generated context, which needs no reflection
    application.JsonWebKeySet = set is not null ? JsonSerializer.Serialize(set, OpenIddictSerializer.Default.JsonWebKeySet) : null;

    return default;
  }

  public ValueTask SetSettingsAsync(TApplication application, ImmutableDictionary<string, string> settings, CancellationToken cancellationToken)
  {
    if (application is null)
    {
      throw new ArgumentNullException(nameof(application));
    }

    if (settings is not { Count: > 0 })
    {
      application.Settings = null;

      return default;
    }

    application.Settings = settings.ToDictionary(x => x.Key, x => x.Value);

    return default;
  }

  public async ValueTask UpdateAsync(TApplication application, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(application);

    var concurrencyToken = application.ConcurrencyToken;
    application.ConcurrencyToken = Guid.NewGuid().ToString();

    // The application is written only when it still has the concurrency token it was loaded with,
    // and the lookup of its client id moves with a changed one in the same transaction
    var items = new List<TransactWriteItem>
    {
      DynamoDbItems.TransactPut(
        _tableName,
        application,
        concurrencyToken is null
          ? "attribute_exists(PartitionKey) and attribute_not_exists(ConcurrencyToken)"
          : "attribute_exists(PartitionKey) and ConcurrencyToken = :concurrencyToken",
        concurrencyToken is null ? default : new() { { ":concurrencyToken", new AttributeValue { S = concurrencyToken } } }),
    };
    var lookupIndex = -1;

    if (application.StoredClientId != application.ClientId)
    {
      if (application.StoredClientId is { } previousClientId)
      {
        items.Add(DynamoDbItems.TransactDelete(
          _tableName, Lookups.ClientId(previousClientId), Lookups.SortKey,
          "attribute_not_exists(PartitionKey) or Id = :id", new() { { ":id", new AttributeValue { S = application.Id } } }));
      }

      if (application.ClientId is { } clientId)
      {
        lookupIndex = items.Count;
        items.Add(DynamoDbItems.TransactPut(
          _tableName, new LookupItem(Lookups.ClientId(clientId), application.Id),
          "attribute_not_exists(PartitionKey) or Id = :id", new() { { ":id", new AttributeValue { S = application.Id } } }));
      }
    }

    try
    {
      await DynamoDbItems.TransactWriteAsync(_client, items, cancellationToken);
    }
    catch (TransactionCanceledException exception)
    {
      application.ConcurrencyToken = concurrencyToken!;
      var failed = DynamoDbItems.FailedConditions(exception);

      throw failed.Contains(lookupIndex) && !failed.Contains(0)
        ? DuplicateClientId(exception)
        : new OpenIddictExceptions.ConcurrencyException(OpenIddictResources.GetResourceString(OpenIddictResources.ID0239), exception);
    }

    application.StoredClientId = application.ClientId;

    // Replace the stored redirects, and the listings under their URIs, with the application's current ones
    var applicationRedirects = await DynamoDbItems.QueryAsync<OpenIddictDynamoDbApplicationRedirect>(_client, new QueryRequest
    {
      TableName = _tableName,
      KeyConditionExpression = "PartitionKey = :partitionKey and begins_with(SortKey, :sortKey)",
      ExpressionAttributeValues = new()
      {
        { ":partitionKey", new AttributeValue { S = application.PartitionKey } },
        { ":sortKey", new AttributeValue { S = RedirectSortKeyPrefix } },
      },
    }, cancellationToken);

    await DynamoDbUtils.BatchWriteAsync(
      _client,
      _tableName,
      applicationRedirects.SelectMany(x => new IDynamoDbItem[] { x, Member(x) }).Select(DynamoDbItems.Delete),
      cancellationToken);

    await SaveRedirectUris(application, cancellationToken);
  }
}
