using System.Security.Claims;
using Benday.Common.Testing;
using Benday.Identity.CosmosDb;
using Xunit;
using Xunit.Abstractions;

namespace Benday.Identity.CosmosDb.UnitTests;

public class CosmosIdentityRoleExtensionsFixture : TestClassBase
{
    public CosmosIdentityRoleExtensionsFixture(ITestOutputHelper outputHelper) : base(outputHelper)
    {
    }

    [Fact]
    public void AddClaim_AddsToClaimsList()
    {
        var role = new CosmosIdentityRole { Name = "Admin" };
        var claim = new Claim("permission", "read");

        role.AddClaim(claim);

        Assert.Single(role.Claims);
        Assert.Equal("permission", role.Claims[0].Type);
        Assert.Equal("read", role.Claims[0].Value);
    }

    [Fact]
    public void AddClaim_DuplicateIsIgnored()
    {
        var role = new CosmosIdentityRole { Name = "Admin" };
        var claim = new Claim("permission", "read");

        role.AddClaim(claim);
        role.AddClaim(claim);

        Assert.Single(role.Claims);
    }

    [Fact]
    public void AddClaim_DifferentClaimsAreAdded()
    {
        var role = new CosmosIdentityRole { Name = "Admin" };

        role.AddClaim(new Claim("permission", "read"));
        role.AddClaim(new Claim("permission", "write"));

        Assert.Equal(2, role.Claims.Count);
    }

    [Fact]
    public void ToClaimList_ConvertsCorrectly()
    {
        var cosmosClaims = new List<CosmosIdentityClaim>
        {
            new CosmosIdentityClaim { Type = "role", Value = "admin" },
            new CosmosIdentityClaim { Type = "permission", Value = "write" }
        };

        var result = cosmosClaims.ToClaimList();

        Assert.Equal(2, result.Count);
        Assert.Equal("role", result[0].Type);
        Assert.Equal("admin", result[0].Value);
        Assert.Equal("permission", result[1].Type);
        Assert.Equal("write", result[1].Value);
    }

    [Fact]
    public void ToClaimList_EmptyList_ReturnsEmptyList()
    {
        var cosmosClaims = new List<CosmosIdentityClaim>();

        var result = cosmosClaims.ToClaimList();

        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
