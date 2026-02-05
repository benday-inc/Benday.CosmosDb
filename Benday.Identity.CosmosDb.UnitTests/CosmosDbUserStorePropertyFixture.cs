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

public class CosmosDbUserStorePropertyFixture : TestClassBase
{
    public CosmosDbUserStorePropertyFixture(ITestOutputHelper outputHelper) : base(outputHelper)
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
    public async Task SetUserNameAsync_SetsValue()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();

        await store.SetUserNameAsync(user, "testuser", CancellationToken.None);

        Assert.Equal("testuser", user.UserName);
    }

    [Fact]
    public async Task SetUserNameAsync_EmptyThrows()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();

        await Assert.ThrowsAsync<ArgumentException>(
            () => store.SetUserNameAsync(user, "", CancellationToken.None));
    }

    [Fact]
    public async Task SetUserNameAsync_NullThrows()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();

        await Assert.ThrowsAsync<ArgumentException>(
            () => store.SetUserNameAsync(user, null, CancellationToken.None));
    }

    [Fact]
    public async Task SetEmailAsync_SetsValue()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();

        await store.SetEmailAsync(user, "test@example.com", CancellationToken.None);

        Assert.Equal("test@example.com", user.Email);
    }

    [Fact]
    public async Task SetEmailAsync_EmptyThrows()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();

        await Assert.ThrowsAsync<ArgumentException>(
            () => store.SetEmailAsync(user, "", CancellationToken.None));
    }

    [Fact]
    public async Task SetPasswordHashAsync_SetsValue()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();

        await store.SetPasswordHashAsync(user, "hashed-password", CancellationToken.None);

        Assert.Equal("hashed-password", user.PasswordHash);
    }

    [Fact]
    public async Task SetPasswordHashAsync_EmptyThrows()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();

        await Assert.ThrowsAsync<ArgumentException>(
            () => store.SetPasswordHashAsync(user, "", CancellationToken.None));
    }

    [Fact]
    public async Task HasPasswordAsync_True_WhenHashSet()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser { PasswordHash = "some-hash" };

        var result = await store.HasPasswordAsync(user, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task HasPasswordAsync_False_WhenEmpty()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser { PasswordHash = string.Empty };

        var result = await store.HasPasswordAsync(user, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task SetSecurityStampAsync_SetsValue()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();

        await store.SetSecurityStampAsync(user, "new-stamp", CancellationToken.None);

        Assert.Equal("new-stamp", user.SecurityStamp);
    }

    [Fact]
    public async Task SetSecurityStampAsync_EmptyThrows()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();

        await Assert.ThrowsAsync<ArgumentException>(
            () => store.SetSecurityStampAsync(user, "", CancellationToken.None));
    }

    [Fact]
    public async Task IncrementAccessFailedCountAsync_Increments()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser { AccessFailedCount = 2 };

        var result = await store.IncrementAccessFailedCountAsync(user, CancellationToken.None);

        Assert.Equal(3, result);
        Assert.Equal(3, user.AccessFailedCount);
    }

    [Fact]
    public async Task ResetAccessFailedCountAsync_SetsToZero()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser { AccessFailedCount = 5 };

        await store.ResetAccessFailedCountAsync(user, CancellationToken.None);

        Assert.Equal(0, user.AccessFailedCount);
    }
}
