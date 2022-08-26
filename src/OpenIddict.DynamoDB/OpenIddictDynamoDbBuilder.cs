using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace OpenIddict.DynamoDB;
public class OpenIddictDynamoDbBuilder
{
    public OpenIddictDynamoDbBuilder(IServiceCollection services)
        => Services = services ?? throw new ArgumentNullException(nameof(services));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public IServiceCollection Services { get; }
}
