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
    public async Task Should_ReturnOne_When_CountingApplicationsAfterCreatingOne()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();
            var application = new OpenIddictDynamoDbApplication
            {
                ClientId = Guid.NewGuid().ToString(),
            };
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var count = await applicationStore.CountAsync(CancellationToken.None);

            // Assert
            Assert.Equal(1, count);            
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToCreateApplicationThatIsNull()
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

    [Fact]
    public async Task Should_ThrowException_When_TryingToDeleteApplicationThatIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.DeleteAsync((OpenIddictDynamoDbApplication)null!, CancellationToken.None));
            Assert.Equal("application", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_DeleteApplication_When_ApplicationIsValid()
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
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            await applicationStore.DeleteAsync(application, CancellationToken.None);

            // Assert
            var databaseApplication = await context.LoadAsync<OpenIddictDynamoDbApplication>(application.Id);
            Assert.Null(databaseApplication);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindApplicationWithoutIdentifier()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.FindByIdAsync((string)null!, CancellationToken.None));
            Assert.Equal("identifier", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_NotThrowException_When_TryingToFindApplicationWithIdentifierThatDoesntExist()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();

            // Act
            var application = await applicationStore.FindByIdAsync("doesnt-exist", CancellationToken.None);

            // Assert
            Assert.Null(application);
        }
    }

    [Fact]
    public async Task Should_ReturnApplication_When_TryingToFindApplicationByExistingIdentifier()
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
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var result = await applicationStore.FindByIdAsync(application.Id!, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(application.ClientId, result!.ClientId);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToFindApplicationByClientIdWithoutIdentifier()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await applicationStore.FindByClientIdAsync((string)null!, CancellationToken.None));
            Assert.Equal("identifier", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_NotThrowException_When_TryingToFindApplicationByClientIdWithIdentifierThatDoesntExist()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var applicationStore = new OpenIddictDynamoDbApplicationStore<OpenIddictDynamoDbApplication>(
                database.Client);
            await applicationStore.EnsureInitializedAsync();

            // Act
            var application = await applicationStore.FindByClientIdAsync("doesnt-exist", CancellationToken.None);

            // Assert
            Assert.Null(application);
        }
    }

    [Fact]
    public async Task Should_ReturnApplication_When_TryingToFindApplicationByClientIdByExistingIdentifier()
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
            await applicationStore.CreateAsync(application, CancellationToken.None);

            // Act
            var result = await applicationStore.FindByClientIdAsync(application.ClientId!, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(application.ClientId, result!.ClientId);
        }
    }
}