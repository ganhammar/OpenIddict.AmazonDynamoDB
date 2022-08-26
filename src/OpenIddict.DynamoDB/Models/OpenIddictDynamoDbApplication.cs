using System.Collections.Immutable;

namespace OpenIddict.DynamoDB;

public class OpenIddictDynamoDbApplication
{
    public virtual string? ClientId { get; set; }
    public virtual string? ClientSecret { get; set; }
    public virtual string? ConcurrencyToken { get; set; } = Guid.NewGuid().ToString();
    public virtual string? ConsentType { get; set; }
    public virtual string? DisplayName { get; set; }
    public virtual IReadOnlyDictionary<string, string>? DisplayNames { get; set; }
        = ImmutableDictionary.Create<string, string>();
    public virtual string? Id { get; set; }
    public virtual IReadOnlyList<string>? Permissions { get; set; } = ImmutableList.Create<string>();
    public virtual IReadOnlyList<string>? PostLogoutRedirectUris { get; set; } = ImmutableList.Create<string>();
    public virtual object? Properties { get; set; }
    public virtual IReadOnlyList<string>? RedirectUris { get; set; } = ImmutableList.Create<string>();
    public virtual IReadOnlyList<string>? Requirements { get; set; } = ImmutableList.Create<string>();
    public virtual string? Type { get; set; }
}
