using Benday.Common.Testing;
using Benday.Identity.CosmosDb;
using Benday.Identity.CosmosDb.UI;
using Xunit;
using Xunit.Abstractions;

namespace Benday.Identity.CosmosDb.UnitTests;

public class NoOpCosmosIdentityEmailSenderFixture : TestClassBase
{
    public NoOpCosmosIdentityEmailSenderFixture(ITestOutputHelper outputHelper) : base(outputHelper)
    {
    }

    [Fact]
    public void Implements_ICosmosIdentityEmailSender()
    {
        var sender = new NoOpCosmosIdentityEmailSender();

        Assert.IsAssignableFrom<ICosmosIdentityEmailSender>(sender);

        WriteLine("NoOpCosmosIdentityEmailSender implements ICosmosIdentityEmailSender");
    }

    [Fact]
    public async Task SendEmailAsync_DoesNotThrow()
    {
        var sender = new NoOpCosmosIdentityEmailSender();

        await sender.SendEmailAsync("test@example.com", "Subject", "<p>Body</p>");

        WriteLine("SendEmailAsync completed without throwing");
    }
}
