using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace OpenIddict.AmazonDynamoDB;

public static class OpenIddictDynamoDbSetup
{
    public static void EnsureInitialized(IServiceProvider services)
    {
        EnsureInitialized(services.GetRequiredService<IOptionsMonitor<OpenIddictDynamoDbOptions>>());
    }

    public static async Task EnsureInitializedAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(
            services.GetRequiredService<IOptionsMonitor<OpenIddictDynamoDbOptions>>(),
            cancellationToken);
    }

    public static async Task EnsureInitializedAsync(
        IOptionsMonitor<OpenIddictDynamoDbOptions> openIddictDynamoDbOptions,
        CancellationToken cancellationToken = default)
    {
        var promises = new[]
        {
            OpenIddictDynamoDbApplicationSetup.EnsureInitializedAsync(
                openIddictDynamoDbOptions.CurrentValue),
            OpenIddictDynamoDbAuthorizationSetup.EnsureInitializedAsync(
                openIddictDynamoDbOptions.CurrentValue),
            OpenIddictDynamoDbTokenSetup.EnsureInitializedAsync(
                openIddictDynamoDbOptions.CurrentValue),
            OpenIddictDynamoDbScopeSetup.EnsureInitializedAsync(
                openIddictDynamoDbOptions.CurrentValue)
        };

        await Task.WhenAll(promises);
    }

    public static void EnsureInitialized(IOptionsMonitor<OpenIddictDynamoDbOptions> openIddictDynamoDbOptions)
    {
        EnsureInitializedAsync(openIddictDynamoDbOptions).GetAwaiter().GetResult();
    }
}