using System.Security.Claims;
using Benday.Common.Testing;
using Benday.CosmosDb.Repositories;
using Benday.Identity.CosmosDb;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Benday.Identity.CosmosDb.UnitTests;

public class CosmosDbRoleStoreFixture : TestClassBase
{
    public CosmosDbRoleStoreFixture(ITestOutputHelper outputHelper) : base(outputHelper)
    {
    }

    private CosmosDbRoleStore CreateSystemUnderTest()
    {
        var options = Options.Create(new CosmosRepositoryOptions<CosmosIdentityRole>
        {
            ConnectionString = "https://localhost:8081",
            DatabaseName = "TestDb",
            ContainerName = "Roles",
            PartitionKey = "/pk,/discriminator",
            UseHierarchicalPartitionKey = true
        });

        var fakeKey = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(
                "0123456789012345678901234567890123456789012345678901234567890123"));
        var client = new CosmosClient("https://localhost:8081", fakeKey);
        var logger = new Mock<ILogger<CosmosDbRoleStore>>();

        return new CosmosDbRoleStore(options, client, logger.Object);
    }

    [Fact]
    public async Task AddClaimAsync_AddsClaim()
    {
        var store = CreateSystemUnderTest();
        var role = new CosmosIdentityRole { Name = "Admin" };
        var claim = new Claim("permission", "manage-users");

        await store.AddClaimAsync(role, claim, CancellationToken.None);

        Assert.Single(role.Claims);
        Assert.Equal("permission", role.Claims[0].Type);
        Assert.Equal("manage-users", role.Claims[0].Value);
    }

    [Fact]
    public async Task AddClaimAsync_DuplicateIsIgnored()
    {
        var store = CreateSystemUnderTest();
        var role = new CosmosIdentityRole { Name = "Admin" };
        var claim = new Claim("permission", "manage-users");

        await store.AddClaimAsync(role, claim, CancellationToken.None);
        await store.AddClaimAsync(role, claim, CancellationToken.None);

        Assert.Single(role.Claims);
    }

    [Fact]
    public async Task RemoveClaimAsync_RemovesClaim()
    {
        var store = CreateSystemUnderTest();
        var role = new CosmosIdentityRole { Name = "Admin" };
        role.Claims.Add(new CosmosIdentityClaim { Type = "permission", Value = "manage-users" });
        role.Claims.Add(new CosmosIdentityClaim { Type = "permission", Value = "read" });

        await store.RemoveClaimAsync(role, new Claim("permission", "manage-users"), CancellationToken.None);

        Assert.Single(role.Claims);
        Assert.Equal("read", role.Claims[0].Value);
    }

    [Fact]
    public async Task RemoveClaimAsync_NoMatch_DoesNothing()
    {
        var store = CreateSystemUnderTest();
        var role = new CosmosIdentityRole { Name = "Admin" };
        role.Claims.Add(new CosmosIdentityClaim { Type = "permission", Value = "read" });

        await store.RemoveClaimAsync(role, new Claim("nonexistent", "value"), CancellationToken.None);

        Assert.Single(role.Claims);
    }

    [Fact]
    public async Task GetClaimsAsync_ReturnsConvertedClaims()
    {
        var store = CreateSystemUnderTest();
        var role = new CosmosIdentityRole { Name = "Admin" };
        role.Claims.Add(new CosmosIdentityClaim { Type = "permission", Value = "read" });
        role.Claims.Add(new CosmosIdentityClaim { Type = "permission", Value = "write" });

        var result = await store.GetClaimsAsync(role, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.IsType<Claim>(result[0]);
        Assert.Equal("permission", result[0].Type);
        Assert.Equal("read", result[0].Value);
    }

    [Fact]
    public async Task SetRoleNameAsync_SetsValue()
    {
        var store = CreateSystemUnderTest();
        var role = new CosmosIdentityRole();

        await store.SetRoleNameAsync(role, "NewRole", CancellationToken.None);

        Assert.Equal("NewRole", role.Name);
    }

    [Fact]
    public async Task SetRoleNameAsync_EmptyThrows()
    {
        var store = CreateSystemUnderTest();
        var role = new CosmosIdentityRole();

        await Assert.ThrowsAsync<ArgumentException>(
            () => store.SetRoleNameAsync(role, "", CancellationToken.None));
    }
}
