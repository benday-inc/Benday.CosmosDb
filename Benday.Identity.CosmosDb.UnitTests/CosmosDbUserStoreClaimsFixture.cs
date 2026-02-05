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

public class CosmosDbUserStoreClaimsFixture : TestClassBase
{
    public CosmosDbUserStoreClaimsFixture(ITestOutputHelper outputHelper) : base(outputHelper)
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
    public async Task AddClaimsAsync_AddsNewClaims()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();
        var claims = new List<Claim>
        {
            new Claim("type1", "value1"),
            new Claim("type2", "value2")
        };

        await store.AddClaimsAsync(user, claims, CancellationToken.None);

        Assert.Equal(2, user.Claims.Count);
        Assert.Contains(user.Claims, c => c.ClaimType == "type1" && c.ClaimValue == "value1");
        Assert.Contains(user.Claims, c => c.ClaimType == "type2" && c.ClaimValue == "value2");
    }

    [Fact]
    public async Task AddClaimsAsync_DuplicateIsIgnored()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();
        var claims = new List<Claim> { new Claim("type1", "value1") };

        await store.AddClaimsAsync(user, claims, CancellationToken.None);
        await store.AddClaimsAsync(user, claims, CancellationToken.None);

        Assert.Single(user.Claims);
    }

    [Fact]
    public async Task ReplaceClaimAsync_UpdatesExistingClaim()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();
        user.Claims.Add(new CosmosIdentityUserClaim { ClaimType = "role", ClaimValue = "editor" });

        var oldClaim = new Claim("role", "editor");
        var newClaim = new Claim("role", "admin");

        await store.ReplaceClaimAsync(user, oldClaim, newClaim, CancellationToken.None);

        Assert.Single(user.Claims);
        Assert.Equal("role", user.Claims[0].ClaimType);
        Assert.Equal("admin", user.Claims[0].ClaimValue);
    }

    [Fact]
    public async Task ReplaceClaimAsync_NoMatch_DoesNothing()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();
        user.Claims.Add(new CosmosIdentityUserClaim { ClaimType = "role", ClaimValue = "editor" });

        var oldClaim = new Claim("nonexistent", "value");
        var newClaim = new Claim("role", "admin");

        await store.ReplaceClaimAsync(user, oldClaim, newClaim, CancellationToken.None);

        Assert.Single(user.Claims);
        Assert.Equal("editor", user.Claims[0].ClaimValue);
    }

    [Fact]
    public async Task RemoveClaimsAsync_RemovesMatchingClaims()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();
        user.Claims.Add(new CosmosIdentityUserClaim { ClaimType = "role", ClaimValue = "admin" });
        user.Claims.Add(new CosmosIdentityUserClaim { ClaimType = "permission", ClaimValue = "read" });

        var claimsToRemove = new List<Claim> { new Claim("role", "admin") };

        await store.RemoveClaimsAsync(user, claimsToRemove, CancellationToken.None);

        Assert.Single(user.Claims);
        Assert.Equal("permission", user.Claims[0].ClaimType);
    }

    [Fact]
    public async Task RemoveClaimsAsync_NoMatch_DoesNothing()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();
        user.Claims.Add(new CosmosIdentityUserClaim { ClaimType = "role", ClaimValue = "admin" });

        var claimsToRemove = new List<Claim> { new Claim("nonexistent", "value") };

        await store.RemoveClaimsAsync(user, claimsToRemove, CancellationToken.None);

        Assert.Single(user.Claims);
    }

    [Fact]
    public async Task GetClaimsAsync_ReturnsConvertedClaims()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();
        user.Claims.Add(new CosmosIdentityUserClaim { ClaimType = "role", ClaimValue = "admin" });
        user.Claims.Add(new CosmosIdentityUserClaim { ClaimType = "email", ClaimValue = "test@test.com" });

        var result = await store.GetClaimsAsync(user, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.IsType<Claim>(result[0]);
        Assert.Equal("role", result[0].Type);
        Assert.Equal("admin", result[0].Value);
    }
}
