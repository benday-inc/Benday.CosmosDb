using System.Security.Claims;
using Benday.Identity.CosmosDb;
using Xunit;
using Xunit.Abstractions;

namespace Benday.Identity.CosmosDb.IntegrationTests;

[Collection("CosmosEmulator")]
public class UserRoleIntegrationFixture : IntegrationTestBase
{
    public UserRoleIntegrationFixture(
        CosmosEmulatorFixture emulator,
        ITestOutputHelper outputHelper) : base(emulator, outputHelper)
    {
    }

    [Fact]
    public async Task AddToRoleAsync_SavesAndPersists()
    {
        var store = CreateUserStore();
        var user = CreateTestUser();
        await store.CreateAsync(user, CancellationToken.None);

        await store.AddToRoleAsync(user, "Admin", CancellationToken.None);

        // Reload from DB to verify persistence
        var reloaded = await store.FindByIdAsync(user.Id, CancellationToken.None);
        Assert.NotNull(reloaded);
        var roles = await store.GetRolesAsync(reloaded, CancellationToken.None);
        Assert.Contains("Admin", roles);
        WriteLine($"User {reloaded.Email} has role: Admin");
    }

    [Fact]
    public async Task RemoveFromRoleAsync_SavesAndPersists()
    {
        var store = CreateUserStore();
        var user = CreateTestUser();
        user.Claims.Add(new CosmosIdentityUserClaim
        {
            ClaimType = ClaimTypes.Role,
            ClaimValue = "Admin"
        });
        await store.CreateAsync(user, CancellationToken.None);

        await store.RemoveFromRoleAsync(user, "Admin", CancellationToken.None);

        // Reload from DB
        var reloaded = await store.FindByIdAsync(user.Id, CancellationToken.None);
        Assert.NotNull(reloaded);
        var roles = await store.GetRolesAsync(reloaded, CancellationToken.None);
        Assert.DoesNotContain("Admin", roles);
        WriteLine("Role removed and persisted");
    }

    [Fact]
    public async Task GetUsersInRoleAsync_ReturnsMatchingUsers()
    {
        var store = CreateUserStore();
        var roleName = $"TestRole-{Guid.NewGuid():N}";

        // Create two users in the same role
        var user1 = CreateTestUser();
        user1.Claims.Add(new CosmosIdentityUserClaim
        {
            ClaimType = ClaimTypes.Role,
            ClaimValue = roleName
        });
        await store.CreateAsync(user1, CancellationToken.None);

        var user2 = CreateTestUser();
        user2.Claims.Add(new CosmosIdentityUserClaim
        {
            ClaimType = ClaimTypes.Role,
            ClaimValue = roleName
        });
        await store.CreateAsync(user2, CancellationToken.None);

        // Create a user NOT in the role
        var user3 = CreateTestUser();
        await store.CreateAsync(user3, CancellationToken.None);

        var usersInRole = await store.GetUsersInRoleAsync(roleName, CancellationToken.None);

        Assert.True(usersInRole.Count >= 2);
        Assert.Contains(usersInRole, u => u.Id == user1.Id);
        Assert.Contains(usersInRole, u => u.Id == user2.Id);
        Assert.DoesNotContain(usersInRole, u => u.Id == user3.Id);
        WriteLine($"Found {usersInRole.Count} users in role {roleName}");
    }
}
