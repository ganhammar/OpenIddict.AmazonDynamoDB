using System.Collections.Immutable;
using System.Text.Json;
using Amazon.DynamoDBv2.DataModel;
using Xunit;

namespace OpenIddict.DynamoDB.Tests;

[Collection("Sequential")]
public class OpenIddictDynamoDbTokenStoreTests
{
    [Fact]
    public async Task Should_ThrowNotSupported_When_TryingToCountBasedOnLinq()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NotSupportedException>(async () =>
                await tokenStore.CountAsync<int>(default!, CancellationToken.None));
        }
    }

    [Fact]
    public async Task Should_ThrowNotSupported_When_TryingToListBasedOnLinq()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            // Act & Assert
            Assert.Throws<NotSupportedException>(() =>
                tokenStore.ListAsync<int, int>(default!, default, CancellationToken.None));
        }
    }

    [Fact]
    public async Task Should_ThrowNotSupported_When_TryingToGetBasedOnLinq()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NotSupportedException>(async () =>
                await tokenStore.GetAsync<int, int>(default!, default!, CancellationToken.None));
        }
    }

    [Fact]
    public async Task Should_ReturnZero_When_CountingTokensInEmptyDatabase()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            // Act
            var count = await tokenStore.CountAsync(CancellationToken.None);

            // Assert
            Assert.Equal(0, count);            
        }
    }

    [Fact]
    public async Task Should_ReturnOne_When_CountingTokensAfterCreatingOne()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();
            var token = new OpenIddictDynamoDbToken();
            await tokenStore.CreateAsync(token, CancellationToken.None);

            // Act
            var count = await tokenStore.CountAsync(CancellationToken.None);

            // Assert
            Assert.Equal(1, count);            
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToCreateTokenThatIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await tokenStore.CreateAsync(default!, CancellationToken.None));
            Assert.Equal("token", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_CreateToken_When_TokenIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();
            var token = new OpenIddictDynamoDbToken
            {
                Subject = Guid.NewGuid().ToString(),
            };

            // Act
            await tokenStore.CreateAsync(token, CancellationToken.None);

            // Assert
            var databaseToken = await context.LoadAsync<OpenIddictDynamoDbToken>(token.Id);
            Assert.NotNull(databaseToken);
            Assert.Equal(token.Subject, databaseToken.Subject);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToDeleteTokenThatIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await tokenStore.DeleteAsync(default!, CancellationToken.None));
            Assert.Equal("token", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_DeleteToken_When_TokenIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();
            var token = new OpenIddictDynamoDbToken();
            await tokenStore.CreateAsync(token, CancellationToken.None);

            // Act
            await tokenStore.DeleteAsync(token, CancellationToken.None);

            // Assert
            var databaseToken = await context.LoadAsync<OpenIddictDynamoDbToken>(token.Id);
            Assert.Null(databaseToken);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetPropertiesAndTokenIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await tokenStore.GetPropertiesAsync(default!, CancellationToken.None));
            Assert.Equal("token", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnEmptyDictionary_When_PropertiesIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();
            var token = new OpenIddictDynamoDbToken();
            await tokenStore.CreateAsync(token, CancellationToken.None);

            // Act
            var properties = await tokenStore.GetPropertiesAsync(
                token,
                CancellationToken.None);

            // Assert
            Assert.Empty(properties);
        }
    }

    [Fact]
    public async Task Should_ReturnNonEmptyDictionary_When_PropertiesExists()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();
            var token = new OpenIddictDynamoDbToken
            {
                Properties = "{ \"Test\": { \"Something\": true }, \"Testing\": { \"Something\": true }, \"Testicles\": { \"Something\": true } }",
            };
            await tokenStore.CreateAsync(token, CancellationToken.None);

            // Act
            var properties = await tokenStore.GetPropertiesAsync(
                token,
                CancellationToken.None);

            // Assert
            Assert.NotNull(properties);
            Assert.Equal(3, properties.Count);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetIdAndTokenIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await tokenStore.GetIdAsync(default!, CancellationToken.None);
            });
            Assert.Equal("token", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnId_When_TokenIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();
            var token = new OpenIddictDynamoDbToken
            {
                Id = Guid.NewGuid().ToString(),
            };
            await tokenStore.CreateAsync(token, CancellationToken.None);

            // Act
            var id = await tokenStore.GetIdAsync(token, CancellationToken.None);

            // Assert
            Assert.NotNull(id);
            Assert.Equal(token.Id, id);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetAuthorizationIdAndTokenIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await tokenStore.GetAuthorizationIdAsync(default!, CancellationToken.None);
            });
            Assert.Equal("token", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnAuthorizationId_When_TokenIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();
            var token = new OpenIddictDynamoDbToken
            {
                AuthorizationId = Guid.NewGuid().ToString(),
            };
            await tokenStore.CreateAsync(token, CancellationToken.None);

            // Act
            var authorizationId = await tokenStore.GetAuthorizationIdAsync(token, CancellationToken.None);

            // Assert
            Assert.NotNull(authorizationId);
            Assert.Equal(token.AuthorizationId, authorizationId);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetCreationDateAndTokenIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await tokenStore.GetCreationDateAsync(default!, CancellationToken.None);
            });
            Assert.Equal("token", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnCreationDate_When_TokenIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();
            var token = new OpenIddictDynamoDbToken
            {
                CreationDate = DateTime.UtcNow,
            };
            await tokenStore.CreateAsync(token, CancellationToken.None);

            // Act
            var creationDate = await tokenStore.GetCreationDateAsync(token, CancellationToken.None);

            // Assert
            Assert.NotNull(creationDate);
            Assert.Equal(token.CreationDate, creationDate);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetExpirationDateAndTokenIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await tokenStore.GetExpirationDateAsync(default!, CancellationToken.None);
            });
            Assert.Equal("token", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnExpirationDate_When_TokenIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();
            var token = new OpenIddictDynamoDbToken
            {
                ExpirationDate = DateTime.UtcNow,
            };
            await tokenStore.CreateAsync(token, CancellationToken.None);

            // Act
            var expirationDate = await tokenStore.GetExpirationDateAsync(token, CancellationToken.None);

            // Assert
            Assert.NotNull(expirationDate);
            Assert.Equal(token.ExpirationDate, expirationDate);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetPayloadAndTokenIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await tokenStore.GetPayloadAsync(default!, CancellationToken.None);
            });
            Assert.Equal("token", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnPayload_When_TokenIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();
            var token = new OpenIddictDynamoDbToken
            {
                Payload = Guid.NewGuid().ToString(),
            };
            await tokenStore.CreateAsync(token, CancellationToken.None);

            // Act
            var expirationDate = await tokenStore.GetPayloadAsync(token, CancellationToken.None);

            // Assert
            Assert.NotNull(expirationDate);
            Assert.Equal(token.Payload, expirationDate);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetRedemptionDateAndTokenIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await tokenStore.GetRedemptionDateAsync(default!, CancellationToken.None);
            });
            Assert.Equal("token", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnRedemptionDate_When_TokenIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();
            var token = new OpenIddictDynamoDbToken
            {
                RedemptionDate = DateTime.UtcNow,
            };
            await tokenStore.CreateAsync(token, CancellationToken.None);

            // Act
            var redemptionDate = await tokenStore.GetRedemptionDateAsync(token, CancellationToken.None);

            // Assert
            Assert.NotNull(redemptionDate);
            Assert.Equal(token.RedemptionDate, redemptionDate);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetReferenceIdAndTokenIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await tokenStore.GetReferenceIdAsync(default!, CancellationToken.None);
            });
            Assert.Equal("token", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnReferenceId_When_TokenIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();
            var token = new OpenIddictDynamoDbToken
            {
                ReferenceId = Guid.NewGuid().ToString(),
            };
            await tokenStore.CreateAsync(token, CancellationToken.None);

            // Act
            var referenceId = await tokenStore.GetReferenceIdAsync(token, CancellationToken.None);

            // Assert
            Assert.NotNull(referenceId);
            Assert.Equal(token.ReferenceId, referenceId);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetStatusAndTokenIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await tokenStore.GetStatusAsync(default!, CancellationToken.None);
            });
            Assert.Equal("token", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnStatus_When_TokenIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();
            var token = new OpenIddictDynamoDbToken
            {
                Status = Guid.NewGuid().ToString(),
            };
            await tokenStore.CreateAsync(token, CancellationToken.None);

            // Act
            var status = await tokenStore.GetStatusAsync(token, CancellationToken.None);

            // Assert
            Assert.NotNull(status);
            Assert.Equal(token.Status, status);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetSubjectAndTokenIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await tokenStore.GetSubjectAsync(default!, CancellationToken.None);
            });
            Assert.Equal("token", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnSubject_When_TokenIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();
            var token = new OpenIddictDynamoDbToken
            {
                Subject = Guid.NewGuid().ToString(),
            };
            await tokenStore.CreateAsync(token, CancellationToken.None);

            // Act
            var subject = await tokenStore.GetSubjectAsync(token, CancellationToken.None);

            // Assert
            Assert.NotNull(subject);
            Assert.Equal(token.Subject, subject);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToGetTypeAndTokenIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await tokenStore.GetTypeAsync(default!, CancellationToken.None);
            });
            Assert.Equal("token", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ReturnType_When_TokenIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();
            var token = new OpenIddictDynamoDbToken
            {
                Type = Guid.NewGuid().ToString(),
            };
            await tokenStore.CreateAsync(token, CancellationToken.None);

            // Act
            var type = await tokenStore.GetTypeAsync(token, CancellationToken.None);

            // Assert
            Assert.NotNull(type);
            Assert.Equal(token.Type, type);
        }
    }

    [Fact]
    public async Task Should_ReturnNewToken_When_CallingInstantiate()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            // Act
            var token = await tokenStore.InstantiateAsync(CancellationToken.None);

            // Assert
            Assert.IsType<OpenIddictDynamoDbToken>(token);
        }
    }

    [Fact]
    public async Task Should_ReturnList_When_ListingTokens()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            var tokenCount = 10;
            foreach (var index in Enumerable.Range(0, tokenCount))
            {
                await tokenStore.CreateAsync(new OpenIddictDynamoDbToken
                {
                    Subject = index.ToString(),
                }, CancellationToken.None);
            }

            // Act
            var tokens = tokenStore.ListAsync(default, default, CancellationToken.None);

            // Assert
            var matchedTokens = new List<OpenIddictDynamoDbToken>();
            await foreach (var token in tokens)
            {
                matchedTokens.Add(token);
            }
            Assert.Equal(tokenCount, matchedTokens.Count);
        }
    }

    [Fact]
    public async Task Should_ReturnFirstFive_When_ListingTokensWithCount()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            foreach (var index in Enumerable.Range(0, 10))
            {
                await tokenStore.CreateAsync(new OpenIddictDynamoDbToken
                {
                    Subject = index.ToString(),
                }, CancellationToken.None);
            }

            // Act
            var expectedCount = 5;
            var tokens = tokenStore.ListAsync(expectedCount, default, CancellationToken.None);

            // Assert
            var matchedTokens = new List<OpenIddictDynamoDbToken>();
            await foreach (var token in tokens)
            {
                matchedTokens.Add(token);
            }
            Assert.Equal(expectedCount, matchedTokens.Count);
        }
    }

    [Fact]
    public async Task Should_ReturnLastFive_When_ListingTokensWithCountAndOffset()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            foreach (var index in Enumerable.Range(0, 10))
            {
                await tokenStore.CreateAsync(new OpenIddictDynamoDbToken
                {
                    Subject = index.ToString(),
                }, CancellationToken.None);
            }

            // Act
            var expectedCount = 5;

            // Need to fetch first page to be able to fetch second
            var first = tokenStore.ListAsync(expectedCount, default, CancellationToken.None);
            var firstTokens = new List<OpenIddictDynamoDbToken>();
            await foreach (var token in first)
            {
                firstTokens.Add(token);
            }

            var tokens = tokenStore.ListAsync(expectedCount, expectedCount, CancellationToken.None);

            // Assert
            var matchedTokens = new List<OpenIddictDynamoDbToken>();
            await foreach (var token in tokens)
            {
                matchedTokens.Add(token);
            }
            Assert.Equal(expectedCount, matchedTokens.Count);
            Assert.Empty(firstTokens.Select(x => x.Id).Intersect(matchedTokens.Select(x => x.Id)));
        }
    }

    [Fact]
    public async Task Should_ThrowNotSupported_When_TryingToFetchWithOffsetWithoutFirstFetchingPreviousPages()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            // Act & Assert
            Assert.Throws<NotSupportedException>(() =>
                tokenStore.ListAsync(5, 5, CancellationToken.None));
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetPropertiesAndTokenIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await tokenStore.SetPropertiesAsync(default!, default!, CancellationToken.None));
            Assert.Equal("token", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetNull_When_SetEmptyListAsProperties()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();
            var token = new OpenIddictDynamoDbToken();
            await tokenStore.CreateAsync(token, CancellationToken.None);

            // Act
            await tokenStore.SetPropertiesAsync(
                token,
                default!,
                CancellationToken.None);

            // Assert
            Assert.Null(token.Properties);
        }
    }

    [Fact]
    public async Task Should_SetProperties_When_SettingProperties()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();
            var token = new OpenIddictDynamoDbToken();
            await tokenStore.CreateAsync(token, CancellationToken.None);

            // Act
            var properties = new Dictionary<string, JsonElement>
            {
                { "Test", JsonDocument.Parse("{ \"Test\": true }").RootElement },
                { "Testing", JsonDocument.Parse("{ \"Test\": true }").RootElement },
                { "Testicles", JsonDocument.Parse("{ \"Test\": true }").RootElement },
            };
            await tokenStore.SetPropertiesAsync(
                token,
                properties.ToImmutableDictionary(x => x.Key, x => x.Value),
                CancellationToken.None);

            // Assert
            Assert.NotNull(token.Properties);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_ToUpdateTokenThatIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await tokenStore.UpdateAsync(default!, CancellationToken.None));
            Assert.Equal("token", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToUpdateTokenThatDoesntExist()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
                await tokenStore.UpdateAsync(new OpenIddictDynamoDbToken(), CancellationToken.None));
            Assert.Equal("token", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_ConcurrencyTokenHasChanged()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();
            var token = new OpenIddictDynamoDbToken();
            await tokenStore.CreateAsync(token, CancellationToken.None);

            // Act & Assert
            token.ConcurrencyToken = Guid.NewGuid().ToString();
            var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
                await tokenStore.UpdateAsync(token, CancellationToken.None));
            Assert.Equal("token", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_UpdateToken_When_TokenIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();
            var token = new OpenIddictDynamoDbToken();
            await tokenStore.CreateAsync(token, CancellationToken.None);

            // Act
            token.Subject = "testing-to-update";
            await tokenStore.UpdateAsync(token, CancellationToken.None);

            // Assert
            var databaseToken = await context.LoadAsync<OpenIddictDynamoDbToken>(token.Id);
            Assert.NotNull(databaseToken);
            Assert.Equal(databaseToken.Subject, token.Subject);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetApplicationIdAndTokenIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await tokenStore.SetApplicationIdAsync(default!, default, CancellationToken.None));
            Assert.Equal("token", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetApplicationId_When_TokenIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();
            var token = new OpenIddictDynamoDbToken();

            // Act
            var value = Guid.NewGuid().ToString();
            await tokenStore.SetApplicationIdAsync(token, value, CancellationToken.None);

            // Assert
            Assert.Equal(value, token.ApplicationId);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetAuthorizationIdAndTokenIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await tokenStore.SetAuthorizationIdAsync(default!, default, CancellationToken.None));
            Assert.Equal("token", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetAuthorizationId_When_TokenIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();
            var token = new OpenIddictDynamoDbToken();

            // Act
            var value = Guid.NewGuid().ToString();
            await tokenStore.SetAuthorizationIdAsync(token, value, CancellationToken.None);

            // Assert
            Assert.Equal(value, token.AuthorizationId);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetCreationDateAndTokenIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await tokenStore.SetCreationDateAsync(default!, default, CancellationToken.None));
            Assert.Equal("token", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetCreationDate_When_TokenIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();
            var token = new OpenIddictDynamoDbToken();

            // Act
            var value = DateTimeOffset.UtcNow;
            await tokenStore.SetCreationDateAsync(token, value, CancellationToken.None);

            // Assert
            Assert.Equal(value.UtcDateTime, token.CreationDate);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetExpirationDateAndTokenIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await tokenStore.SetExpirationDateAsync(default!, default, CancellationToken.None));
            Assert.Equal("token", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetExpirationDate_When_TokenIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();
            var token = new OpenIddictDynamoDbToken();

            // Act
            var value = DateTimeOffset.UtcNow;
            await tokenStore.SetExpirationDateAsync(token, value, CancellationToken.None);

            // Assert
            Assert.Equal(value.UtcDateTime, token.ExpirationDate);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetRedemptionDateAndTokenIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await tokenStore.SetRedemptionDateAsync(default!, default, CancellationToken.None));
            Assert.Equal("token", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetRedemptionDate_When_TokenIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();
            var token = new OpenIddictDynamoDbToken();

            // Act
            var value = DateTimeOffset.UtcNow;
            await tokenStore.SetRedemptionDateAsync(token, value, CancellationToken.None);

            // Assert
            Assert.Equal(value.UtcDateTime, token.RedemptionDate);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetPayloadAndTokenIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await tokenStore.SetPayloadAsync(default!, default, CancellationToken.None));
            Assert.Equal("token", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetPayload_When_TokenIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();
            var token = new OpenIddictDynamoDbToken();

            // Act
            var value = Guid.NewGuid().ToString();
            await tokenStore.SetPayloadAsync(token, value, CancellationToken.None);

            // Assert
            Assert.Equal(value, token.Payload);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetStatusAndTokenIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await tokenStore.SetStatusAsync(default!, default, CancellationToken.None));
            Assert.Equal("token", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetStatus_When_TokenIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();
            var token = new OpenIddictDynamoDbToken();

            // Act
            var value = Guid.NewGuid().ToString();
            await tokenStore.SetStatusAsync(token, value, CancellationToken.None);

            // Assert
            Assert.Equal(value, token.Status);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetSubjectAndTokenIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await tokenStore.SetSubjectAsync(default!, default, CancellationToken.None));
            Assert.Equal("token", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetSubject_When_TokenIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();
            var token = new OpenIddictDynamoDbToken();

            // Act
            var value = Guid.NewGuid().ToString();
            await tokenStore.SetSubjectAsync(token, value, CancellationToken.None);

            // Assert
            Assert.Equal(value, token.Subject);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetTypeAndTokenIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await tokenStore.SetTypeAsync(default!, default, CancellationToken.None));
            Assert.Equal("token", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetType_When_TokenIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();
            var token = new OpenIddictDynamoDbToken();

            // Act
            var value = Guid.NewGuid().ToString();
            await tokenStore.SetTypeAsync(token, value, CancellationToken.None);

            // Assert
            Assert.Equal(value, token.Type);
        }
    }

    [Fact]
    public async Task Should_ThrowException_When_TryingToSetReferenceIdAndTokenIsNull()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await tokenStore.SetReferenceIdAsync(default!, default, CancellationToken.None));
            Assert.Equal("token", exception.ParamName);
        }
    }

    [Fact]
    public async Task Should_SetReferenceId_When_TokenIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var context = new DynamoDBContext(database.Client);
            var tokenStore = new OpenIddictDynamoDbTokenStore<OpenIddictDynamoDbToken>(
                database.Client);
            await tokenStore.EnsureInitializedAsync();
            var token = new OpenIddictDynamoDbToken();

            // Act
            var value = Guid.NewGuid().ToString();
            await tokenStore.SetReferenceIdAsync(token, value, CancellationToken.None);

            // Assert
            Assert.Equal(value, token.ReferenceId);
        }
    }
}