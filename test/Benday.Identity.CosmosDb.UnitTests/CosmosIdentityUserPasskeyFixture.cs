using Benday.Common.Testing;
using Benday.Identity.CosmosDb;
using Xunit;
using Xunit.Abstractions;

namespace Benday.Identity.CosmosDb.UnitTests;

public class CosmosIdentityUserPasskeyFixture : TestClassBase
{
    public CosmosIdentityUserPasskeyFixture(ITestOutputHelper outputHelper) : base(outputHelper)
    {
    }

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var passkey = new CosmosIdentityUserPasskey();

        Assert.Equal(string.Empty, passkey.CredentialId);
        Assert.Equal(string.Empty, passkey.PublicKey);
        Assert.Equal(0u, passkey.SignCount);
        Assert.NotNull(passkey.Transports);
        Assert.Empty(passkey.Transports);
        Assert.Equal(string.Empty, passkey.AttestationObject);
        Assert.Equal(string.Empty, passkey.ClientDataJson);
        Assert.False(passkey.IsUserVerified);
        Assert.False(passkey.IsBackupEligible);
        Assert.False(passkey.IsBackedUp);
        Assert.Equal(string.Empty, passkey.Name);
        Assert.Equal(default(DateTimeOffset), passkey.CreatedAt);
    }

    [Fact]
    public void CosmosIdentityUser_Passkeys_DefaultsToEmptyList()
    {
        var user = new CosmosIdentityUser();

        Assert.NotNull(user.Passkeys);
        Assert.Empty(user.Passkeys);
    }

    [Fact]
    public void Properties_CanBeSetAndRead()
    {
        var now = DateTimeOffset.UtcNow;
        var passkey = new CosmosIdentityUserPasskey
        {
            CredentialId = "abc123",
            PublicKey = "pubkey456",
            SignCount = 5,
            Transports = new[] { "usb", "internal" },
            AttestationObject = "attest789",
            ClientDataJson = "client012",
            IsUserVerified = true,
            IsBackupEligible = true,
            IsBackedUp = false,
            Name = "My laptop",
            CreatedAt = now
        };

        Assert.Equal("abc123", passkey.CredentialId);
        Assert.Equal("pubkey456", passkey.PublicKey);
        Assert.Equal(5u, passkey.SignCount);
        Assert.Equal(new[] { "usb", "internal" }, passkey.Transports);
        Assert.Equal("attest789", passkey.AttestationObject);
        Assert.Equal("client012", passkey.ClientDataJson);
        Assert.True(passkey.IsUserVerified);
        Assert.True(passkey.IsBackupEligible);
        Assert.False(passkey.IsBackedUp);
        Assert.Equal("My laptop", passkey.Name);
        Assert.Equal(now, passkey.CreatedAt);
    }
}
