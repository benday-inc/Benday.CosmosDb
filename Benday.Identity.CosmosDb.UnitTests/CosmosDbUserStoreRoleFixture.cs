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

public class CosmosDbUserStoreRoleFixture : TestClassBase
{
    public CosmosDbUserStoreRoleFixture(ITestOutputHelper outputHelper) : base(outputHelper)
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

    [Fact]
    public async Task GetRolesAsync_ReturnsRoleNames()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();
        user.Claims.Add(new CosmosIdentityUserClaim
        {
            ClaimType = ClaimTypes.Role,
            ClaimValue = "Admin"
        });
        user.Claims.Add(new CosmosIdentityUserClaim
        {
            ClaimType = ClaimTypes.Role,
            ClaimValue = "Editor"
        });

        var roles = await store.GetRolesAsync(user, CancellationToken.None);

        Assert.Equal(2, roles.Count);
        Assert.Contains("Admin", roles);
        Assert.Contains("Editor", roles);
    }

    [Fact]
    public async Task GetRolesAsync_NoRoles_ReturnsEmpty()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();

        var roles = await store.GetRolesAsync(user, CancellationToken.None);

        Assert.Empty(roles);
    }

    [Fact]
    public async Task GetRolesAsync_IgnoresNonRoleClaims()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();
        user.Claims.Add(new CosmosIdentityUserClaim
        {
            ClaimType = ClaimTypes.Role,
            ClaimValue = "Admin"
        });
        user.Claims.Add(new CosmosIdentityUserClaim
        {
            ClaimType = "permission",
            ClaimValue = "read"
        });

        var roles = await store.GetRolesAsync(user, CancellationToken.None);

        Assert.Single(roles);
        Assert.Equal("Admin", roles[0]);
    }

    [Fact]
    public async Task IsInRoleAsync_True_WhenRoleExists()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();
        user.Claims.Add(new CosmosIdentityUserClaim
        {
            ClaimType = ClaimTypes.Role,
            ClaimValue = "Admin"
        });

        var result = await store.IsInRoleAsync(user, "Admin", CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task IsInRoleAsync_False_WhenRoleDoesNotExist()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();

        var result = await store.IsInRoleAsync(user, "Admin", CancellationToken.None);

        Assert.False(result);
    }
}
