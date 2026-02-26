namespace Benday.Identity.CosmosDb;

/// <summary>
/// Configuration options for the Cosmos Identity registration.
/// </summary>
public class CosmosIdentityOptions
{
    /// <summary>
    /// The Cosmos DB container name for storing users.
    /// Defaults to the ContainerName from the CosmosConfig passed to AddCosmosIdentity.
    /// Can be overridden via the configureOptions callback.
    /// </summary>
    public string UsersContainerName { get; set; } = "Users";

    /// <summary>
    /// The Cosmos DB container name for storing roles.
    /// Defaults to the ContainerName from the CosmosConfig passed to AddCosmosIdentity.
    /// Can be overridden via the configureOptions callback.
    /// </summary>
    public string RolesContainerName { get; set; } = "Roles";

    /// <summary>
    /// The name of the authentication cookie. Default: "Identity.Auth".
    /// </summary>
    public string CookieName { get; set; } = "Identity.Auth";

    /// <summary>
    /// The path to the login page. Default: "/Account/Login".
    /// </summary>
    public string LoginPath { get; set; } = "/Account/Login";

    /// <summary>
    /// The path to the logout page. Default: "/Account/Logout".
    /// </summary>
    public string LogoutPath { get; set; } = "/Account/Logout";

    /// <summary>
    /// The path to the access denied page. Default: "/Account/AccessDenied".
    /// </summary>
    public string AccessDeniedPath { get; set; } = "/Account/AccessDenied";

    /// <summary>
    /// The cookie expiration time. Default: 14 days.
    /// </summary>
    public TimeSpan CookieExpiration { get; set; } = TimeSpan.FromDays(14);

    /// <summary>
    /// Whether to use sliding expiration for the cookie. Default: true.
    /// </summary>
    public bool SlidingExpiration { get; set; } = true;

    /// <summary>
    /// Whether self-registration is allowed. Default: true.
    /// When false, the Register page returns a 404 NotFound (for private sites).
    /// </summary>
    public bool AllowRegistration { get; set; } = true;

    /// <summary>
    /// The role name required for admin pages. Default: "UserAdmin".
    /// </summary>
    public string AdminRoleName { get; set; } = "UserAdmin";

    /// <summary>
    /// Whether to require confirmed email before sign-in. Default: false.
    /// When true, newly registered users must confirm their email before they can log in.
    /// </summary>
    public bool RequireConfirmedEmail { get; set; } = false;

    /// <summary>
    /// Whether to show the "Remember me" checkbox on the login page. Default: true.
    /// When false, the checkbox is hidden and the login uses <see cref="RememberMeDefaultValue"/> for persistence.
    /// </summary>
    public bool ShowRememberMe { get; set; } = true;

    /// <summary>
    /// The default checked state of the "Remember me" checkbox. Default: true.
    /// Also used as the isPersistent value when <see cref="ShowRememberMe"/> is false.
    /// </summary>
    public bool RememberMeDefaultValue { get; set; } = true;

    /// <summary>
    /// The "From" email address used by SmtpCosmosIdentityEmailSender.
    /// Default: "" (empty — must be configured before using the SMTP sender).
    /// </summary>
    public string FromEmailAddress { get; set; } = string.Empty;
}
