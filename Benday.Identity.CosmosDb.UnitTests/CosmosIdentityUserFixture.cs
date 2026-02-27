using Benday.Common.Testing;
using Benday.Identity.CosmosDb;
using Xunit;
using Xunit.Abstractions;

namespace Benday.Identity.CosmosDb.UnitTests;

public class CosmosIdentityUserFixture : TestClassBase
{
    public CosmosIdentityUserFixture(ITestOutputHelper outputHelper) : base(outputHelper)
    {
    }

    [Fact]
    public void NewUser_HasExpectedDefaults()
    {
        var user = new CosmosIdentityUser();

        Assert.Equal(CosmosIdentityConstants.SystemOwnerId, user.OwnerId);
        Assert.NotNull(user.Claims);
        Assert.Empty(user.Claims);
        Assert.NotNull(user.Logins);
        Assert.Empty(user.Logins);
        Assert.NotNull(user.Passkeys);
        Assert.Empty(user.Passkeys);
        Assert.NotNull(user.RecoveryCodes);
        Assert.Empty(user.RecoveryCodes);
        Assert.True(user.LockoutEnabled);
        Assert.False(string.IsNullOrEmpty(user.SecurityStamp));
        Assert.False(string.IsNullOrEmpty(user.ConcurrencyStamp));
        Assert.Equal(string.Empty, user.UserName);
        Assert.Equal(string.Empty, user.Email);
        Assert.Equal(string.Empty, user.PasswordHash);
        Assert.False(user.EmailConfirmed);
        Assert.False(user.TwoFactorEnabled);
        Assert.False(user.PhoneNumberConfirmed);
        Assert.Equal(0, user.AccessFailedCount);
        Assert.Null(user.LockoutEnd);
        Assert.Null(user.PhoneNumber);
        Assert.Null(user.AuthenticatorKey);
    }

    [Fact]
    public void NormalizedUserName_ReturnsUppercase()
    {
        var user = new CosmosIdentityUser { UserName = "test@example.com" };

        Assert.Equal("TEST@EXAMPLE.COM", user.NormalizedUserName);
    }

    [Fact]
    public void NormalizedEmail_ReturnsUppercase()
    {
        var user = new CosmosIdentityUser { Email = "Test@Example.COM" };

        Assert.Equal("TEST@EXAMPLE.COM", user.NormalizedEmail);
    }

    [Fact]
    public void SetNormalizedUserName_IsNoOp()
    {
        var user = new CosmosIdentityUser { UserName = "original" };

        user.NormalizedUserName = "SHOULD_BE_IGNORED";

        Assert.Equal("original", user.UserName);
        Assert.Equal("ORIGINAL", user.NormalizedUserName);
    }

    [Fact]
    public void SetNormalizedEmail_IsNoOp()
    {
        var user = new CosmosIdentityUser { Email = "original@test.com" };

        user.NormalizedEmail = "SHOULD_BE_IGNORED";

        Assert.Equal("original@test.com", user.Email);
        Assert.Equal("ORIGINAL@TEST.COM", user.NormalizedEmail);
    }
}
