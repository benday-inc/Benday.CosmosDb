using Benday.Common.Testing;
using Benday.Identity.CosmosDb;
using Xunit;
using Xunit.Abstractions;

namespace Benday.Identity.CosmosDb.UnitTests;

public class CosmosIdentityRoleFixture : TestClassBase
{
    public CosmosIdentityRoleFixture(ITestOutputHelper outputHelper) : base(outputHelper)
    {
    }

    [Fact]
    public void NewRole_HasExpectedDefaults()
    {
        var role = new CosmosIdentityRole();

        Assert.Equal(CosmosIdentityConstants.SystemOwnerId, role.OwnerId);
        Assert.NotNull(role.Claims);
        Assert.Empty(role.Claims);
        Assert.False(string.IsNullOrEmpty(role.ConcurrencyStamp));
        Assert.Equal(string.Empty, role.Name);
    }

    [Fact]
    public void NormalizedName_ReturnsUppercase()
    {
        var role = new CosmosIdentityRole { Name = "Admin" };

        Assert.Equal("ADMIN", role.NormalizedName);
    }

    [Fact]
    public void SetNormalizedName_IsNoOp()
    {
        var role = new CosmosIdentityRole { Name = "Editor" };

        role.NormalizedName = "SHOULD_BE_IGNORED";

        Assert.Equal("Editor", role.Name);
        Assert.Equal("EDITOR", role.NormalizedName);
    }
}
