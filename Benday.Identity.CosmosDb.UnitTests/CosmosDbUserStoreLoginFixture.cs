using Benday.Common.Testing;
using Benday.CosmosDb.Repositories;
using Benday.Identity.CosmosDb;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Benday.Identity.CosmosDb.UnitTests;

public class CosmosDbUserStoreLoginFixture : TestClassBase
{
    public CosmosDbUserStoreLoginFixture(ITestOutputHelper outputHelper) : base(outputHelper)
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
    public async Task AddLoginAsync_AddsLogin()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();
        var login = new UserLoginInfo("Google", "google-key-123", "Google");

        await store.AddLoginAsync(user, login, CancellationToken.None);

        Assert.Single(user.Logins);
        Assert.Equal("Google", user.Logins[0].LoginProvider);
        Assert.Equal("google-key-123", user.Logins[0].ProviderKey);
        Assert.Equal("Google", user.Logins[0].ProviderDisplayName);
    }

    [Fact]
    public async Task AddLoginAsync_DuplicateIsIgnored()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();
        var login = new UserLoginInfo("Google", "google-key-123", "Google");

        await store.AddLoginAsync(user, login, CancellationToken.None);
        await store.AddLoginAsync(user, login, CancellationToken.None);

        Assert.Single(user.Logins);
    }

    [Fact]
    public async Task RemoveLoginAsync_RemovesLogin()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();
        user.Logins.Add(new CosmosIdentityUserLogin
        {
            LoginProvider = "Google",
            ProviderKey = "google-key-123",
            ProviderDisplayName = "Google"
        });

        await store.RemoveLoginAsync(user, "Google", "google-key-123", CancellationToken.None);

        Assert.Empty(user.Logins);
    }

    [Fact]
    public async Task RemoveLoginAsync_NoMatch_DoesNothing()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();
        user.Logins.Add(new CosmosIdentityUserLogin
        {
            LoginProvider = "Google",
            ProviderKey = "google-key-123",
            ProviderDisplayName = "Google"
        });

        await store.RemoveLoginAsync(user, "Facebook", "fb-key-456", CancellationToken.None);

        Assert.Single(user.Logins);
    }

    [Fact]
    public async Task GetLoginsAsync_ReturnsConvertedLogins()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();
        user.Logins.Add(new CosmosIdentityUserLogin
        {
            LoginProvider = "Google",
            ProviderKey = "google-key-123",
            ProviderDisplayName = "Google"
        });
        user.Logins.Add(new CosmosIdentityUserLogin
        {
            LoginProvider = "Facebook",
            ProviderKey = "fb-key-456",
            ProviderDisplayName = "Facebook"
        });

        var result = await store.GetLoginsAsync(user, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.IsType<UserLoginInfo>(result[0]);
        Assert.Equal("Google", result[0].LoginProvider);
        Assert.Equal("google-key-123", result[0].ProviderKey);
    }
}
