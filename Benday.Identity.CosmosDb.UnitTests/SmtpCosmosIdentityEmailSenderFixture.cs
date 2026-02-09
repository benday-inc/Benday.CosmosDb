using System.Net.Mail;
using Benday.Common.Testing;
using Benday.Identity.CosmosDb;
using Xunit;
using Xunit.Abstractions;

namespace Benday.Identity.CosmosDb.UnitTests;

public class SmtpCosmosIdentityEmailSenderFixture : TestClassBase
{
    public SmtpCosmosIdentityEmailSenderFixture(ITestOutputHelper outputHelper) : base(outputHelper)
    {
    }

    [Fact]
    public void Implements_ICosmosIdentityEmailSender()
    {
        var options = new CosmosIdentityOptions { FromEmailAddress = "test@example.com" };
        var client = new SmtpClient("localhost");
        var sender = new SmtpCosmosIdentityEmailSender(client, options);

        Assert.IsAssignableFrom<ICosmosIdentityEmailSender>(sender);

        WriteLine("SmtpCosmosIdentityEmailSender implements ICosmosIdentityEmailSender");
    }

    [Fact]
    public void Constructor_ThrowsWhenFromEmailAddressIsEmpty()
    {
        var options = new CosmosIdentityOptions(); // FromEmailAddress defaults to ""
        var client = new SmtpClient("localhost");

        var ex = Assert.Throws<InvalidOperationException>(
            () => new SmtpCosmosIdentityEmailSender(client, options));

        Assert.Contains("FromEmailAddress", ex.Message);

        WriteLine($"Threw expected exception: {ex.Message}");
    }

    [Fact]
    public void Constructor_ThrowsWhenClientIsNull()
    {
        var options = new CosmosIdentityOptions { FromEmailAddress = "test@example.com" };

        Assert.Throws<ArgumentNullException>(
            () => new SmtpCosmosIdentityEmailSender(null!, options));

        WriteLine("Threw ArgumentNullException for null SmtpClient");
    }

    [Fact]
    public void Constructor_ThrowsWhenOptionsIsNull()
    {
        var client = new SmtpClient("localhost");

        Assert.Throws<ArgumentNullException>(
            () => new SmtpCosmosIdentityEmailSender(client, null!));

        WriteLine("Threw ArgumentNullException for null options");
    }
}
