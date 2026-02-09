using System.Threading.Tasks;

namespace Benday.Identity.CosmosDb;

/// <summary>
/// Abstraction for sending identity-related emails (confirmation, password reset, etc.).
/// The default implementation is a no-op. Register your own implementation
/// before calling AddCosmosIdentityWithUI() to enable email sending.
/// </summary>
public interface ICosmosIdentityEmailSender
{
    /// <summary>
    /// Sends an email message.
    /// </summary>
    /// <param name="email">The recipient email address.</param>
    /// <param name="subject">The email subject.</param>
    /// <param name="htmlMessage">The HTML body of the email.</param>
    Task SendEmailAsync(string email, string subject, string htmlMessage);
}
