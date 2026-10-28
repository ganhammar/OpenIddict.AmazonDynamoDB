using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;

namespace OpenIddict.AmazonDynamoDB;

[DynamoDBTable(Constants.DefaultTableName)]
public class OpenIddictDynamoDbApplication : IDynamoDbItem
{
  [DynamoDBHashKey]
  public string PartitionKey
  {
    get => $"APPLICATION#{Id}";
    set { }
  }
  [DynamoDBRangeKey]
  public string? SortKey
  {
    get => $"#USER#{Id}";
    set { }
  }
  public virtual string Id { get; set; } = Guid.NewGuid().ToString();
  public virtual string? ClientId { get; set; }
  public virtual string? ClientSecret { get; set; }
  public virtual string ConcurrencyToken { get; set; } = Guid.NewGuid().ToString();
  public virtual string? ConsentType { get; set; }
  public virtual string? DisplayName { get; set; }
  public virtual Dictionary<string, string>? DisplayNames { get; set; } = [];
  public virtual List<string>? Permissions { get; set; } = [];
  public virtual List<string>? PostLogoutRedirectUris { get; set; } = [];
  public virtual string? Properties { get; set; }
  public virtual List<string>? RedirectUris { get; set; } = [];
  public virtual List<string>? Requirements { get; set; } = [];
  public virtual string? Type { get; set; }
  public virtual string? ApplicationType { get; set; }
  public virtual string? JsonWebKeySet { get; set; }
  public virtual Dictionary<string, string>? Settings { get; set; } = [];

  /// <summary>The client id as the store last read it, whose lookup is removed when it changes.</summary>
  internal string? StoredClientId { get; set; }

  /// <summary>
  /// Writes the application's attributes to the item the store saves. A derived type with
  /// properties of its own overrides this, calls the base implementation and adds them, using the
  /// <see cref="AttributeValues"/> extension methods.
  /// </summary>
  protected virtual void WriteAttributes(Dictionary<string, AttributeValue> item)
  {
    ArgumentNullException.ThrowIfNull(item);

    item.Set("PartitionKey", PartitionKey);
    item.Set("SortKey", SortKey);
    item.Set("Id", Id);
    item.Set("ClientId", ClientId);
    item.Set("ClientSecret", ClientSecret);
    item.Set("ConcurrencyToken", ConcurrencyToken);
    item.Set("ConsentType", ConsentType);
    item.Set("DisplayName", DisplayName);
    item.SetStringMap("DisplayNames", DisplayNames);
    item.SetStringSet("Permissions", Permissions);
    item.SetStringSet("PostLogoutRedirectUris", PostLogoutRedirectUris);
    item.Set("Properties", Properties);
    item.SetStringSet("RedirectUris", RedirectUris);
    item.SetStringSet("Requirements", Requirements);
    item.Set("Type", Type);
    item.Set("ApplicationType", ApplicationType);
    item.Set("JsonWebKeySet", JsonWebKeySet);
    item.SetStringMap("Settings", Settings);
  }

  /// <summary>
  /// Reads the application's attributes from the item the store loaded. A derived type with
  /// properties of its own overrides this, calls the base implementation and reads them, using the
  /// <see cref="AttributeValues"/> extension methods.
  /// </summary>
  protected virtual void ReadAttributes(Dictionary<string, AttributeValue> item)
  {
    ArgumentNullException.ThrowIfNull(item);

    Id = item.GetString("Id") ?? Id;
    ClientId = item.GetString("ClientId");
    ClientSecret = item.GetString("ClientSecret");
    ConcurrencyToken = item.GetString("ConcurrencyToken") ?? ConcurrencyToken;
    ConsentType = item.GetString("ConsentType");
    DisplayName = item.GetString("DisplayName");
    DisplayNames = item.GetStringMap("DisplayNames") ?? DisplayNames;
    Permissions = item.GetStringList("Permissions") ?? Permissions;
    PostLogoutRedirectUris = item.GetStringList("PostLogoutRedirectUris") ?? PostLogoutRedirectUris;
    Properties = item.GetString("Properties");
    RedirectUris = item.GetStringList("RedirectUris") ?? RedirectUris;
    Requirements = item.GetStringList("Requirements") ?? Requirements;
    Type = item.GetString("Type");
    ApplicationType = item.GetString("ApplicationType");
    JsonWebKeySet = item.GetString("JsonWebKeySet");
    Settings = item.GetStringMap("Settings") ?? Settings;
    StoredClientId = ClientId;
  }

  void IDynamoDbItem.Write(Dictionary<string, AttributeValue> item) => WriteAttributes(item);

  void IDynamoDbItem.Read(Dictionary<string, AttributeValue> item) => ReadAttributes(item);
}
