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

public class OpenIddictDynamoDbTokenStore<TToken> : IOpenIddictTokenStore<TToken>
    where TToken : OpenIddictDynamoDbToken, new()
{
  private IAmazonDynamoDB _client;
  private IDynamoDBContext _context;

  public OpenIddictDynamoDbTokenStore(
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
    var count = new CountModel(CountType.Token);
    count = await _context.LoadAsync<CountModel>(count.PartitionKey, count.SortKey, cancellationToken);

    return count?.Count ?? 0;
  }

  public ValueTask<long> CountAsync<TResult>(Func<IQueryable<TToken>, IQueryable<TResult>> query, CancellationToken cancellationToken)
  {
    throw new NotSupportedException();
  }

  public async ValueTask CreateAsync(TToken token, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    await _context.SaveAsync(token, cancellationToken);

    var count = await CountAsync(cancellationToken);
    var newCount = count + 1;
    await _context.SaveAsync(new CountModel(CountType.Token, newCount), cancellationToken);
  }

  public async ValueTask DeleteAsync(TToken token, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    await _context.DeleteAsync(token, cancellationToken);

    var count = await CountAsync(cancellationToken);
    await _context.SaveAsync(new CountModel(CountType.Token, count - 1), cancellationToken);
  }

  private IAsyncEnumerable<TToken> FindBySubjectAndSearchKey(string subject, string searchKey, CancellationToken cancellationToken)
  {
    return ExecuteAsync(cancellationToken);

    async IAsyncEnumerable<TToken> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
      var search = _context.FromQueryAsync<TToken>(new QueryOperationConfig
      {
        IndexName = "Subject-index",
        KeyExpression = new Expression
        {
          ExpressionStatement = "Subject = :subject and begins_with(SearchKey, :searchKey)",
          ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
          {
            { ":subject", subject },
            { ":searchKey", searchKey },
          }
        },
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
    ArgumentNullException.ThrowIfNull(subject);
    ArgumentNullException.ThrowIfNull(client);

    return FindBySubjectAndSearchKey(subject, $"APPLICATION#{client}", cancellationToken);
  }

  public IAsyncEnumerable<TToken> FindAsync(string subject, string client, string status, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(subject);
    ArgumentNullException.ThrowIfNull(client);
    ArgumentNullException.ThrowIfNull(status);

    return FindBySubjectAndSearchKey(subject, $"APPLICATION#{client}#STATUS#{status}", cancellationToken);
  }

  public IAsyncEnumerable<TToken> FindAsync(string subject, string client, string status, string type, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(subject);
    ArgumentNullException.ThrowIfNull(client);
    ArgumentNullException.ThrowIfNull(status);
    ArgumentNullException.ThrowIfNull(type);

    return FindBySubjectAndSearchKey(subject, $"APPLICATION#{client}#STATUS#{status}#TYPE#{type}", cancellationToken);
  }

  public IAsyncEnumerable<TToken> FindByApplicationIdAsync(string identifier, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(identifier);

    return ExecuteAsync(cancellationToken);

    async IAsyncEnumerable<TToken> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
      var search = _context.FromQueryAsync<TToken>(new QueryOperationConfig
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
      });

      var tokens = await search.GetRemainingAsync(cancellationToken);

      foreach (var token in tokens)
      {
        yield return token;
      }
    }
  }

  public IAsyncEnumerable<TToken> FindByAuthorizationIdAsync(string identifier, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(identifier);

    return ExecuteAsync(cancellationToken);

    async IAsyncEnumerable<TToken> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
      var search = _context.FromQueryAsync<TToken>(new QueryOperationConfig
      {
        IndexName = "AuthorizationId-index",
        KeyExpression = new Expression
        {
          ExpressionStatement = "AuthorizationId = :authorizationId",
          ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
          {
            { ":authorizationId", identifier },
          }
        },
      });

      var tokens = await search.GetRemainingAsync(cancellationToken);

      foreach (var token in tokens)
      {
        yield return token;
      }
    }
  }

  public async ValueTask<TToken?> FindByIdAsync(string identifier, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(identifier);

    return await GetByPartitionKey(new() { Id = identifier }, cancellationToken);
  }

  public async ValueTask<TToken?> FindByReferenceIdAsync(string identifier, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(identifier);

    var search = _context.FromQueryAsync<TToken>(new QueryOperationConfig
    {
      IndexName = "ReferenceId-index",
      KeyExpression = new Expression
      {
        ExpressionStatement = "ReferenceId = :referenceId",
        ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
        {
          { ":referenceId", identifier },
        }
      },
      Limit = 1,
    });
    var tokens = await search.GetRemainingAsync(cancellationToken);
    return tokens?.FirstOrDefault();
  }

  public IAsyncEnumerable<TToken> FindBySubjectAsync(string subject, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(subject);

    return ExecuteAsync(cancellationToken);

    async IAsyncEnumerable<TToken> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
      var search = _context.FromQueryAsync<TToken>(new QueryOperationConfig
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
      });

      var tokens = await search.GetRemainingAsync(cancellationToken);

      foreach (var token in tokens)
      {
        yield return token;
      }
    }
  }

  public ValueTask<string?> GetApplicationIdAsync(TToken token, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    return new(token.ApplicationId);
  }

  public ValueTask<TResult> GetAsync<TState, TResult>(Func<IQueryable<TToken>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
  {
    throw new NotSupportedException();
  }

  public ValueTask<string?> GetAuthorizationIdAsync(TToken token, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    return new(token.AuthorizationId);
  }

  public ValueTask<DateTimeOffset?> GetCreationDateAsync(TToken token, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    return new(token.CreationDate);
  }

  public ValueTask<DateTimeOffset?> GetExpirationDateAsync(TToken token, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    return new(token.ExpirationDate);
  }

  public ValueTask<string?> GetIdAsync(TToken token, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    return new(token.Id);
  }

  public ValueTask<string?> GetPayloadAsync(TToken token, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    return new(token.Payload);
  }

  public ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(TToken token, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

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
    ArgumentNullException.ThrowIfNull(token);

    return new(token.RedemptionDate);
  }

  public ValueTask<string?> GetReferenceIdAsync(TToken token, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    return new(token.ReferenceId);
  }

  public ValueTask<string?> GetStatusAsync(TToken token, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    return new(token.Status);
  }

  public ValueTask<string?> GetSubjectAsync(TToken token, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    return new(token.Subject);
  }

  public ValueTask<string?> GetTypeAsync(TToken token, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

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

  // TODO: Use DynamoDB expirations instead
  public async ValueTask PruneAsync(DateTimeOffset threshold, CancellationToken cancellationToken)
  {
    var deleteCount = 0;
    // Get all tokens which is older than threshold
    var filter = new ScanFilter();
    filter.AddCondition("CreationDate", ScanOperator.LessThan, new List<AttributeValue>
    {
      new AttributeValue(threshold.UtcDateTime.ToString("o")),
    });
    var search = _context.FromScanAsync<TToken>(new ScanOperationConfig
    {
      Filter = filter,
    });
    var tokens = await search.GetRemainingAsync(cancellationToken);
    var remainingTokens = new List<TToken>();

    var batchDelete = _context.CreateBatchWrite<TToken>();

    // Add tokens which is not Inactive/Valid or where ExpirationDate has passed to delete batch
    foreach (var token in tokens)
    {
      if (new[] { Statuses.Inactive, Statuses.Valid }.Contains(token.Status) == false
        || token.ExpirationDate < DateTime.UtcNow)
      {
        batchDelete.AddDeleteItem(token);
        deleteCount++;
      }
      else
      {
        remainingTokens.Add(token);
      }
    }

    // Get all authorizations connected to the remaining tokens
    var authorizations = _context.CreateBatchGet<OpenIddictDynamoDbAuthorization>();
    var authorizationIds = remainingTokens
      .Select(x => x.AuthorizationId)
      .Where(x => x != default)
      .Distinct();
    foreach (var authorizationId in authorizationIds)
    {
      var authorization = new OpenIddictDynamoDbAuthorization
      {
        Id = authorizationId!,
      };
      authorizations.AddKey(authorization.PartitionKey, authorization.SortKey);
    }
    await authorizations.ExecuteAsync(cancellationToken);

    // Add tokens which has invalid authorizations to delete batch
    foreach (var authorization in authorizations.Results.Where(x => x.Status != Statuses.Valid))
    {
      var tokensToDelete = remainingTokens
        .Where(x => x.AuthorizationId == authorization.Id);
      batchDelete.AddDeleteItems(tokensToDelete);
      deleteCount += tokensToDelete.Count();
    }

    await batchDelete.ExecuteAsync(cancellationToken);

    var count = await CountAsync(cancellationToken);
    await _context.SaveAsync(new CountModel(CountType.Token, count - deleteCount), cancellationToken);
  }

  public ValueTask SetApplicationIdAsync(TToken token, string? identifier, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    token.ApplicationId = identifier;

    return default;
  }

  public ValueTask SetAuthorizationIdAsync(TToken token, string? identifier, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    token.AuthorizationId = identifier;

    return default;
  }

  public ValueTask SetCreationDateAsync(TToken token, DateTimeOffset? date, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    token.CreationDate = date?.UtcDateTime;

    return default;
  }

  public ValueTask SetExpirationDateAsync(TToken token, DateTimeOffset? date, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    token.ExpirationDate = date?.UtcDateTime;

    return default;
  }

  public ValueTask SetPayloadAsync(TToken token, string? payload, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    token.Payload = payload;

    return default;
  }

  public ValueTask SetPropertiesAsync(TToken token, ImmutableDictionary<string, JsonElement> properties, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

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
    ArgumentNullException.ThrowIfNull(token);

    token.RedemptionDate = date?.UtcDateTime;

    return default;
  }

  public ValueTask SetReferenceIdAsync(TToken token, string? identifier, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    token.ReferenceId = identifier;

    return default;
  }

  public ValueTask SetStatusAsync(TToken token, string? status, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    token.Status = status;

    return default;
  }

  public ValueTask SetSubjectAsync(TToken token, string? subject, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    token.Subject = subject;

    return default;
  }

  public ValueTask SetTypeAsync(TToken token, string? type, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    token.Type = type;

    return default;
  }

  public async ValueTask UpdateAsync(TToken token, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(token);

    // Ensure no one else is updating
    var databaseApplication = await GetByPartitionKey(token, cancellationToken);
    if (databaseApplication == default || databaseApplication.ConcurrencyToken != token.ConcurrencyToken)
    {
      throw new ArgumentException("Given token is invalid", nameof(token));
    }

    token.ConcurrencyToken = Guid.NewGuid().ToString();

    await _context.SaveAsync(token, cancellationToken);
  }

  private async Task<TToken?> GetByPartitionKey(TToken token, CancellationToken cancellationToken)
  {
    var search = _context.FromQueryAsync<TToken>(new QueryOperationConfig
    {
      KeyExpression = new Expression
      {
        ExpressionStatement = "PartitionKey = :partitionKey",
        ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
        {
          { ":partitionKey", token.PartitionKey },
        }
      },
      Limit = 1,
    });
    var result = await search.GetNextSetAsync();

    return result.Any() ? result.First() : default;
  }
}
