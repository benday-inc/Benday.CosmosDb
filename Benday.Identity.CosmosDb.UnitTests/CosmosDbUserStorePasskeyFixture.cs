using Benday.Common.Testing;
using Benday.CosmosDb.Repositories;
using Benday.Identity.CosmosDb;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Benday.Identity.CosmosDb.UnitTests;

public class CosmosDbUserStorePasskeyFixture : TestClassBase
{
    public CosmosDbUserStorePasskeyFixture(ITestOutputHelper outputHelper) : base(outputHelper)
    {
    }

    private CosmosDbUserStore CreateSystemUnderTest()
    {
        var options = Options.Create(new CosmosRepositoryOptions<CosmosIdentityUser>
        {
            ConnectionString = "https://localhost:8081",
            DatabaseName = "TestDb",
            ContainerName = "Users",
            PartitionKey = "/pk,/discriminator",
            UseHierarchicalPartitionKey = true
        });

        var fakeKey = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(
                "0123456789012345678901234567890123456789012345678901234567890123"));
        var client = new CosmosClient("https://localhost:8081", fakeKey);
        var logger = new Mock<ILogger<CosmosDbUserStore>>();

        return new CosmosDbUserStore(options, client, logger.Object);
    }

    private static UserPasskeyInfo CreateTestPasskey(
        byte[]? credentialId = null,
        byte[]? publicKey = null,
        string name = "Test passkey")
    {
        credentialId ??= new byte[] { 1, 2, 3, 4, 5 };
        publicKey ??= new byte[] { 10, 20, 30, 40, 50 };

        return new UserPasskeyInfo(
            credentialId,
            publicKey,
            DateTimeOffset.UtcNow,
            signCount: 0,
            transports: new[] { "internal" },
            isUserVerified: true,
            isBackupEligible: true,
            isBackedUp: false,
            attestationObject: new byte[] { 100, 101, 102 },
            clientDataJson: new byte[] { 200, 201, 202 })
        {
            Name = name
        };
    }

    [Fact]
    public async Task AddOrUpdatePasskeyAsync_AddsNewPasskey()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();
        var passkey = CreateTestPasskey();

        await store.AddOrUpdatePasskeyAsync(user, passkey, CancellationToken.None);

        Assert.Single(user.Passkeys);
        Assert.Equal("Test passkey", user.Passkeys[0].Name);
        Assert.True(user.Passkeys[0].IsUserVerified);
        Assert.True(user.Passkeys[0].IsBackupEligible);
        Assert.False(user.Passkeys[0].IsBackedUp);
        Assert.Equal(0u, user.Passkeys[0].SignCount);
        Assert.Equal(new[] { "internal" }, user.Passkeys[0].Transports);
    }

    [Fact]
    public async Task AddOrUpdatePasskeyAsync_UpdatesExistingPasskey()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();
        var credentialId = new byte[] { 1, 2, 3, 4, 5 };

        var passkey1 = CreateTestPasskey(credentialId: credentialId, name: "Original");
        await store.AddOrUpdatePasskeyAsync(user, passkey1, CancellationToken.None);

        // Create updated passkey with same credential ID but different sign count
        var passkey2 = new UserPasskeyInfo(
            credentialId,
            new byte[] { 10, 20, 30, 40, 50 },
            DateTimeOffset.UtcNow,
            signCount: 5,
            transports: new[] { "internal" },
            isUserVerified: true,
            isBackupEligible: true,
            isBackedUp: true,
            attestationObject: new byte[] { 100, 101, 102 },
            clientDataJson: new byte[] { 200, 201, 202 })
        {
            Name = "Updated"
        };

        await store.AddOrUpdatePasskeyAsync(user, passkey2, CancellationToken.None);

        Assert.Single(user.Passkeys);
        Assert.Equal("Updated", user.Passkeys[0].Name);
        Assert.Equal(5u, user.Passkeys[0].SignCount);
        Assert.True(user.Passkeys[0].IsBackedUp);
    }

    [Fact]
    public async Task GetPasskeysAsync_ReturnsEmptyForNewUser()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();

        var result = await store.GetPasskeysAsync(user, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPasskeysAsync_ReturnsConvertedPasskeys()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();
        var credentialId = new byte[] { 1, 2, 3, 4, 5 };

        await store.AddOrUpdatePasskeyAsync(user, CreateTestPasskey(credentialId: credentialId), CancellationToken.None);

        var result = await store.GetPasskeysAsync(user, CancellationToken.None);

        Assert.Single(result);
        var passkey = result[0];
        Assert.IsType<UserPasskeyInfo>(passkey);
        Assert.Equal(credentialId, passkey.CredentialId);
        Assert.Equal("Test passkey", passkey.Name);
    }

    [Fact]
    public async Task FindPasskeyAsync_ReturnsMatchingPasskey()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();
        var credentialId = new byte[] { 1, 2, 3, 4, 5 };

        await store.AddOrUpdatePasskeyAsync(user, CreateTestPasskey(credentialId: credentialId, name: "My key"), CancellationToken.None);

        var result = await store.FindPasskeyAsync(user, credentialId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("My key", result.Name);
        Assert.Equal(credentialId, result.CredentialId);
    }

    [Fact]
    public async Task FindPasskeyAsync_ReturnsNullWhenNotFound()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();

        var result = await store.FindPasskeyAsync(user, new byte[] { 99, 98, 97 }, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task RemovePasskeyAsync_RemovesMatchingPasskey()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();
        var credentialId = new byte[] { 1, 2, 3, 4, 5 };

        await store.AddOrUpdatePasskeyAsync(user, CreateTestPasskey(credentialId: credentialId), CancellationToken.None);
        Assert.Single(user.Passkeys);

        await store.RemovePasskeyAsync(user, credentialId, CancellationToken.None);

        Assert.Empty(user.Passkeys);
    }

    [Fact]
    public async Task RemovePasskeyAsync_NoMatch_DoesNothing()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();

        await store.AddOrUpdatePasskeyAsync(user, CreateTestPasskey(), CancellationToken.None);
        Assert.Single(user.Passkeys);

        await store.RemovePasskeyAsync(user, new byte[] { 99, 98, 97 }, CancellationToken.None);

        Assert.Single(user.Passkeys);
    }

    [Fact]
    public async Task RoundTrip_Base64UrlEncoding_PreservesData()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();
        var credentialId = new byte[] { 0, 255, 128, 64, 32, 16, 8, 4, 2, 1 };
        var publicKey = new byte[] { 200, 150, 100, 50, 25, 12, 6, 3 };

        await store.AddOrUpdatePasskeyAsync(user, CreateTestPasskey(credentialId: credentialId, publicKey: publicKey), CancellationToken.None);

        var result = await store.FindPasskeyAsync(user, credentialId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(credentialId, result.CredentialId);
        Assert.Equal(publicKey, result.PublicKey);
    }

    [Fact]
    public async Task AddMultiplePasskeys_AllStored()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();

        await store.AddOrUpdatePasskeyAsync(user, CreateTestPasskey(credentialId: new byte[] { 1 }, name: "Key 1"), CancellationToken.None);
        await store.AddOrUpdatePasskeyAsync(user, CreateTestPasskey(credentialId: new byte[] { 2 }, name: "Key 2"), CancellationToken.None);
        await store.AddOrUpdatePasskeyAsync(user, CreateTestPasskey(credentialId: new byte[] { 3 }, name: "Key 3"), CancellationToken.None);

        var result = await store.GetPasskeysAsync(user, CancellationToken.None);

        Assert.Equal(3, result.Count);
    }
}
