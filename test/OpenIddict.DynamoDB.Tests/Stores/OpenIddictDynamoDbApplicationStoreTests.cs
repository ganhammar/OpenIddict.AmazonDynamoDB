using Amazon.DynamoDBv2.DataModel;
using Xunit;

namespace OpenIddict.DynamoDB.Tests;

public class OpenIddictDynamoDbApplicationStoreTests
{
    [Fact]
    public async Task Should_ReturnZero_When_CountingApplicationsInEmptyDatabase()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();

            // Act
            var count = await applicationStore.CountAsync(CancellationToken.None);

            // Assert
            Assert.Equal(0, count);            
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_ApplicationIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.CreateAsync((OpenIddictDynamoDbApplication)null!, CancellationToken.None));
            Assert.Equal("application", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_CreateApplication_When_ApplicationIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();
            var application = new OpenIddictDynamoDbApplication
            {
                ClientId = Guid.NewGuid().ToString(),
            };

            // Act
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Assert
            var databaseApplication = await context.LoadAsync<OpenIddictDynamoDbApplication>(application.Id);
            Assert.NotNull(databaseApplication);
            Assert.Equal(application.ClientId, databaseApplication.ClientId);
        }
    }
}