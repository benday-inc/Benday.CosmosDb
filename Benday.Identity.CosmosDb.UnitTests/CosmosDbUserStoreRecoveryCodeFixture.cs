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

public class CosmosDbUserStoreRecoveryCodeFixture : TestClassBase
{
    public CosmosDbUserStoreRecoveryCodeFixture(ITestOutputHelper outputHelper) : base(outputHelper)
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
    public async Task ReplaceCodesAsync_SetsRecoveryCodes()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();
        var codes = new[] { "CODE1", "CODE2", "CODE3" };

        await store.ReplaceCodesAsync(user, codes, CancellationToken.None);

        Assert.Equal(3, user.RecoveryCodes.Count);
        Assert.Contains("CODE1", user.RecoveryCodes);
        Assert.Contains("CODE2", user.RecoveryCodes);
        Assert.Contains("CODE3", user.RecoveryCodes);
    }

    [Fact]
    public async Task RedeemCodeAsync_ValidCode_ReturnsTrueAndRemoves()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();
        user.RecoveryCodes = new List<string> { "CODE1", "CODE2", "CODE3" };

        var result = await store.RedeemCodeAsync(user, "CODE2", CancellationToken.None);

        Assert.True(result);
        Assert.Equal(2, user.RecoveryCodes.Count);
        Assert.DoesNotContain("CODE2", user.RecoveryCodes);
    }

    [Fact]
    public async Task RedeemCodeAsync_InvalidCode_ReturnsFalse()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();
        user.RecoveryCodes = new List<string> { "CODE1", "CODE2" };

        var result = await store.RedeemCodeAsync(user, "INVALID", CancellationToken.None);

        Assert.False(result);
        Assert.Equal(2, user.RecoveryCodes.Count);
    }

    [Fact]
    public async Task CountCodesAsync_ReturnsCorrectCount()
    {
        var store = CreateSystemUnderTest();
        var user = new CosmosIdentityUser();
        user.RecoveryCodes = new List<string> { "CODE1", "CODE2", "CODE3" };

        var count = await store.CountCodesAsync(user, CancellationToken.None);

        Assert.Equal(3, count);
    }
}
