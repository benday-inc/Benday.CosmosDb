using System.Net.Mail;

namespace Benday.Identity.CosmosDb;

/// <summary>
/// Email sender implementation that uses <see cref="SmtpClient"/> to send emails.
/// Requires a configured <see cref="SmtpClient"/> and <see cref="CosmosIdentityOptions.FromEmailAddress"/>
/// to be registered in DI.
/// </summary>
public class SmtpCosmosIdentityEmailSender : ICosmosIdentityEmailSender
{
    private readonly SmtpClient _client;
    private readonly CosmosIdentityOptions _options;

    public SmtpCosmosIdentityEmailSender(SmtpClient client, CosmosIdentityOptions options)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(options.FromEmailAddress))
        {
            throw new InvalidOperationException(
                "CosmosIdentityOptions.FromEmailAddress must be configured when using SmtpCosmosIdentityEmailSender.");
        }
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var message = new MailMessage(_options.FromEmailAddress, email, subject, htmlMessage)
        {
            IsBodyHtml = true
        };

        await _client.SendMailAsync(message);
    }
}
