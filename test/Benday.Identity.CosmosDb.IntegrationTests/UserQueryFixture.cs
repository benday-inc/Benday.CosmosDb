using System.Security.Claims;
using Benday.Identity.CosmosDb;
using Microsoft.AspNetCore.Identity;
using Xunit;
using Xunit.Abstractions;

namespace Benday.Identity.CosmosDb.IntegrationTests;

[Collection("CosmosEmulator")]
public class UserQueryFixture : IntegrationTestBase
{
    public UserQueryFixture(
        CosmosEmulatorFixture emulator,
        ITestOutputHelper outputHelper) : base(emulator, outputHelper)
    {
    }

    [Fact]
    public async Task GetUsersForClaimAsync_ReturnsMatchingUsers()
    {
        var store = CreateUserStore();
        var claimType = $"custom-{Guid.NewGuid():N}";
        var claimValue = "test-value";

        var user1 = CreateTestUser();
        user1.Claims.Add(new CosmosIdentityUserClaim
        {
            ClaimType = claimType,
            ClaimValue = claimValue
        });
        await store.CreateAsync(user1, CancellationToken.None);

        var user2 = CreateTestUser();
        await store.CreateAsync(user2, CancellationToken.None);

        var claim = new Claim(claimType, claimValue);
        var usersWithClaim = await store.GetUsersForClaimAsync(claim, CancellationToken.None);

        Assert.Contains(usersWithClaim, u => u.Id == user1.Id);
        Assert.DoesNotContain(usersWithClaim, u => u.Id == user2.Id);
        WriteLine($"Found {usersWithClaim.Count} users with claim {claimType}");
    }

    [Fact]
    public async Task FindByLoginAsync_ReturnsMatchingUser()
    {
        var store = CreateUserStore();
        var provider = $"Provider-{Guid.NewGuid():N}";
        var providerKey = $"key-{Guid.NewGuid():N}";

        var user = CreateTestUser();
        user.Logins.Add(new CosmosIdentityUserLogin
        {
            LoginProvider = provider,
            ProviderKey = providerKey,
            ProviderDisplayName = "Test Provider"
        });
        await store.CreateAsync(user, CancellationToken.None);

        var found = await store.FindByLoginAsync(provider, providerKey, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(user.Id, found.Id);
        WriteLine($"Found user by login: {found.Email}");
    }

    [Fact]
    public async Task FindByLoginAsync_NoMatch_ReturnsNull()
    {
        var store = CreateUserStore();

        var found = await store.FindByLoginAsync("nonexistent-provider", "nonexistent-key", CancellationToken.None);

        Assert.Null(found);
        WriteLine("Correctly returned null for nonexistent login");
    }
}
