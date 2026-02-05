using Benday.Identity.CosmosDb;
using Xunit;
using Xunit.Abstractions;

namespace Benday.Identity.CosmosDb.IntegrationTests;

[Collection("CosmosEmulator")]
public class RoleStoreCrudFixture : IntegrationTestBase
{
    public RoleStoreCrudFixture(
        CosmosEmulatorFixture emulator,
        ITestOutputHelper outputHelper) : base(emulator, outputHelper)
    {
    }

    [Fact]
    public async Task CreateAsync_SavesRole()
    {
        var store = CreateRoleStore();
        var role = CreateTestRole();

        var result = await store.CreateAsync(role, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.False(string.IsNullOrEmpty(role.Id));
        WriteLine($"Created role with Id: {role.Id}, Name: {role.Name}");
    }

    [Fact]
    public async Task FindByIdAsync_ReturnsRole()
    {
        var store = CreateRoleStore();
        var role = CreateTestRole();
        await store.CreateAsync(role, CancellationToken.None);

        var found = await store.FindByIdAsync(role.Id, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(role.Id, found.Id);
        Assert.Equal(role.Name, found.Name);
        WriteLine($"Found role: {found.Name}");
    }

    [Fact]
    public async Task FindByNameAsync_ReturnsRole()
    {
        var store = CreateRoleStore();
        var role = CreateTestRole();
        await store.CreateAsync(role, CancellationToken.None);

        var found = await store.FindByNameAsync(role.NormalizedName, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(role.Id, found.Id);
        WriteLine($"Found role by name: {found.Name}");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesConcurrencyStamp()
    {
        var store = CreateRoleStore();
        var role = CreateTestRole();
        await store.CreateAsync(role, CancellationToken.None);
        var originalStamp = role.ConcurrencyStamp;

        var result = await store.UpdateAsync(role, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotEqual(originalStamp, role.ConcurrencyStamp);
        WriteLine($"Stamp changed from {originalStamp} to {role.ConcurrencyStamp}");
    }

    [Fact]
    public async Task DeleteAsync_RemovesRole()
    {
        var store = CreateRoleStore();
        var role = CreateTestRole();
        await store.CreateAsync(role, CancellationToken.None);
        var roleId = role.Id;

        var result = await store.DeleteAsync(role, CancellationToken.None);

        Assert.True(result.Succeeded);
        var found = await store.FindByIdAsync(roleId, CancellationToken.None);
        Assert.Null(found);
        WriteLine("Role deleted successfully");
    }
}
