using Benday.Identity.CosmosDb;
using Xunit;
using Xunit.Abstractions;

namespace Benday.Identity.CosmosDb.IntegrationTests;

[Collection("CosmosEmulator")]
public class UserStoreCrudFixture : IntegrationTestBase
{
    public UserStoreCrudFixture(
        CosmosEmulatorFixture emulator,
        ITestOutputHelper outputHelper) : base(emulator, outputHelper)
    {
    }

    [Fact]
    public async Task CreateAsync_SavesUser()
    {
        var store = CreateUserStore();
        var user = CreateTestUser();

        var result = await store.CreateAsync(user, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.False(string.IsNullOrEmpty(user.Id));
        WriteLine($"Created user with Id: {user.Id}");
    }

    [Fact]
    public async Task FindByIdAsync_ReturnsUser()
    {
        var store = CreateUserStore();
        var user = CreateTestUser();
        await store.CreateAsync(user, CancellationToken.None);

        var found = await store.FindByIdAsync(user.Id, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(user.Id, found.Id);
        Assert.Equal(user.Email, found.Email);
        WriteLine($"Found user: {found.Email}");
    }

    [Fact]
    public async Task FindByIdAsync_NotFound_ReturnsNull()
    {
        var store = CreateUserStore();

        var found = await store.FindByIdAsync("nonexistent-id", CancellationToken.None);

        Assert.Null(found);
        WriteLine("Correctly returned null for nonexistent user");
    }

    [Fact]
    public async Task FindByNameAsync_ReturnsUser()
    {
        var store = CreateUserStore();
        var user = CreateTestUser();
        await store.CreateAsync(user, CancellationToken.None);

        var found = await store.FindByNameAsync(user.NormalizedUserName, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(user.Id, found.Id);
        WriteLine($"Found user by name: {found.UserName}");
    }

    [Fact]
    public async Task FindByEmailAsync_ReturnsUser()
    {
        var store = CreateUserStore();
        var user = CreateTestUser();
        await store.CreateAsync(user, CancellationToken.None);

        var found = await store.FindByEmailAsync(user.NormalizedEmail, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(user.Id, found.Id);
        WriteLine($"Found user by email: {found.Email}");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesConcurrencyStamp()
    {
        var store = CreateUserStore();
        var user = CreateTestUser();
        await store.CreateAsync(user, CancellationToken.None);
        var originalStamp = user.ConcurrencyStamp;

        var result = await store.UpdateAsync(user, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotEqual(originalStamp, user.ConcurrencyStamp);
        WriteLine($"Stamp changed from {originalStamp} to {user.ConcurrencyStamp}");
    }

    [Fact]
    public async Task DeleteAsync_RemovesUser()
    {
        var store = CreateUserStore();
        var user = CreateTestUser();
        await store.CreateAsync(user, CancellationToken.None);
        var userId = user.Id;

        var result = await store.DeleteAsync(user, CancellationToken.None);

        Assert.True(result.Succeeded);
        var found = await store.FindByIdAsync(userId, CancellationToken.None);
        Assert.Null(found);
        WriteLine("User deleted successfully");
    }
}
