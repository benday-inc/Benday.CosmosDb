using Benday.Identity.CosmosDb;

namespace Benday.Identity.CosmosDb.UI;

/// <summary>
/// Default no-op email sender that discards all email requests.
/// Replace by registering your own <see cref="ICosmosIdentityEmailSender"/>
/// before calling AddCosmosIdentityWithUI().
/// </summary>
public class NoOpCosmosIdentityEmailSender : ICosmosIdentityEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        return Task.CompletedTask;
    }
}
