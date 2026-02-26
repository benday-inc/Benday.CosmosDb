using Benday.Common.Testing;
using Benday.Identity.CosmosDb;
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
        Assert.True(options.AllowRegistration);
        Assert.Equal("UserAdmin", options.AdminRoleName);
        Assert.False(options.RequireConfirmedEmail);
        Assert.Equal(string.Empty, options.FromEmailAddress);
        Assert.True(options.ShowRememberMe);
        Assert.True(options.RememberMeDefaultValue);
    }
}
