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
}
