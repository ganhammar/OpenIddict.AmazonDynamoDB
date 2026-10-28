using Amazon.DynamoDBv2.Model;

namespace OpenIddict.AmazonDynamoDB;

/// <summary>
/// The keys of the lookup items, which point from a value to the entity it belongs to, and of the
/// membership items, which list the entities under a value. They replace the single-attribute
/// global secondary indexes: a lookup is one strongly consistent read, and a unique value is
/// enforced by writing its lookup item in the same transaction as the entity.
/// </summary>
internal static class Lookups
{
  /// <summary>The sort key of a lookup item, of which a value has at most one.</summary>
  public const string SortKey = "#LOOKUP";

  public static string ClientId(string clientId) => $"CLIENTID#{clientId}";

  public static string ReferenceId(string referenceId) => $"REFERENCEID#{referenceId}";

  /// <summary>The partition that lists the applications registered for a redirect URI.</summary>
  public static string Redirect(RedirectType type, string redirectUri) => $"REDIRECT#{type}#{redirectUri}";

  public static string ApplicationMember(string applicationId) => $"APPLICATION#{applicationId}";

  /// <summary>Tokens are listed in their authorization's partition, under this prefix.</summary>
  public const string TokenMemberPrefix = "TOKEN#";

  public static string TokenMember(string tokenId) => $"{TokenMemberPrefix}{tokenId}";

  /// <summary>The scope lookup that holds a scope's name, as the scope store has always written it.</summary>
  public static OpenIddictDynamoDbScopeLookup ScopeName(string name, string? scopeId = default)
    => new(name, LookupType.Name) { ScopeId = scopeId };

  public static OpenIddictDynamoDbScopeLookup ScopeResource(string resource, string scopeId)
    => new(resource, LookupType.Resource, scopeId) { ScopeId = scopeId };
}

/// <summary>A pointer from a unique value to the id of the entity it belongs to, which expires with the entity.</summary>
internal sealed class LookupItem : IDynamoDbItem
{
  public LookupItem() { }

  public LookupItem(string partitionKey, string id, DateTime? ttl = default)
  {
    PartitionKey = partitionKey;
    Id = id;
    TTL = ttl;
  }

  public string PartitionKey { get; set; } = "";

  public string? SortKey => Lookups.SortKey;

  public string Id { get; set; } = "";

  public DateTime? TTL { get; set; }

  void IDynamoDbItem.Write(Dictionary<string, AttributeValue> item)
  {
    item.Set("PartitionKey", PartitionKey);
    item.Set("SortKey", SortKey);
    item.Set("Id", Id);
    item.SetEpochSeconds("ttl", TTL);
  }

  void IDynamoDbItem.Read(Dictionary<string, AttributeValue> item)
  {
    PartitionKey = item.GetString("PartitionKey") ?? "";
    Id = item.GetString("Id") ?? "";
    TTL = item.GetEpochSeconds("ttl");
  }
}

/// <summary>One entity listed under a value: an application under a redirect URI, or a token under its authorization.</summary>
internal sealed class MemberItem : IDynamoDbItem
{
  public MemberItem() { }

  public MemberItem(string partitionKey, string sortKey, string id, DateTime? ttl = default)
  {
    PartitionKey = partitionKey;
    SortKey = sortKey;
    Id = id;
    TTL = ttl;
  }

  public string PartitionKey { get; set; } = "";

  public string? SortKey { get; set; }

  public string Id { get; set; } = "";

  public DateTime? TTL { get; set; }

  void IDynamoDbItem.Write(Dictionary<string, AttributeValue> item)
  {
    item.Set("PartitionKey", PartitionKey);
    item.Set("SortKey", SortKey);
    item.Set("Id", Id);
    item.SetEpochSeconds("ttl", TTL);
  }

  void IDynamoDbItem.Read(Dictionary<string, AttributeValue> item)
  {
    PartitionKey = item.GetString("PartitionKey") ?? "";
    SortKey = item.GetString("SortKey");
    Id = item.GetString("Id") ?? "";
    TTL = item.GetEpochSeconds("ttl");
  }
}
