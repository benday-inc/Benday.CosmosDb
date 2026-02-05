using Benday.Common.Testing;
using Benday.Identity.CosmosDb.UI;
using Xunit;
using Xunit.Abstractions;

namespace Benday.Identity.CosmosDb.UnitTests;

public class CosmosIdentityOptionsFixture : TestClassBase
{
    public CosmosIdentityOptionsFixture(ITestOutputHelper outputHelper) : base(outputHelper)
    {
    }

    [Fact]
    public void Defaults_AreCorrect()
    {
        var options = new CosmosIdentityOptions();

        Assert.Equal("Users", options.UsersContainerName);
        Assert.Equal("Roles", options.RolesContainerName);
        Assert.Equal("Identity.Auth", options.CookieName);
        Assert.Equal("/Account/Login", options.LoginPath);
        Assert.Equal("/Account/Logout", options.LogoutPath);
        Assert.Equal("/Account/AccessDenied", options.AccessDeniedPath);
        Assert.Equal(TimeSpan.FromDays(14), options.CookieExpiration);
        Assert.True(options.SlidingExpiration);
    }
}
