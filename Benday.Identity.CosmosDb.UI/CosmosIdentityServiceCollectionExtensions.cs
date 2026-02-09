using Benday.CosmosDb.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Benday.Identity.CosmosDb.UI;

/// <summary>
/// Extension methods for registering Cosmos Identity services with UI support.
/// </summary>
public static class CosmosIdentityUIServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Cosmos Identity services including user/role stores,
    /// ASP.NET Core Identity, claims principal factory, application cookie,
    /// token providers, email sender, and admin authorization policy.
    /// Use this method when your application includes the pre-built Razor Pages
    /// from this package.
    /// For core-only registration without cookies, use AddCosmosIdentity() from Benday.Identity.CosmosDb.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="cosmosConfig">Cosmos DB configuration.</param>
    /// <param name="configureOptions">Optional action to configure identity-specific options (container names, cookie settings, etc.).</param>
    /// <param name="configureIdentity">Optional action to configure ASP.NET Core Identity options (password rules, lockout, etc.).</param>
    /// <returns>An <see cref="IdentityBuilder"/> for further configuration.</returns>
    public static IdentityBuilder AddCosmosIdentityWithUI(
        this IServiceCollection services,
        CosmosConfig cosmosConfig,
        Action<CosmosIdentityOptions>? configureOptions = null,
        Action<IdentityOptions>? configureIdentity = null)
    {
        // Register Cosmos infrastructure (CosmosClient, stores, repository options)
        var options = services.AddCosmosIdentityStores(cosmosConfig, configureOptions);

        // Register no-op email sender; consumers can override by registering their own
        // ICosmosIdentityEmailSender before calling this method
        services.TryAddSingleton<ICosmosIdentityEmailSender, NoOpCosmosIdentityEmailSender>();

        // Register full ASP.NET Core Identity with authentication and cookie support
        var builder = services.AddIdentity<CosmosIdentityUser, CosmosIdentityRole>(identityOptions =>
        {
            // Apply RequireConfirmedEmail from options (consumer callback can still override)
            identityOptions.SignIn.RequireConfirmedEmail = options.RequireConfirmedEmail;
            configureIdentity?.Invoke(identityOptions);
        });

        // Add token providers for password reset and email confirmation
        builder.AddDefaultTokenProviders();

        // Register claims principal factory
        services.AddScoped<IUserClaimsPrincipalFactory<CosmosIdentityUser>,
            DefaultUserClaimsPrincipalFactory>();

        // Configure application cookie (requires Microsoft.AspNetCore.App)
        services.ConfigureApplicationCookie(cookieOptions =>
        {
            cookieOptions.Cookie.Name = options.CookieName;
            cookieOptions.LoginPath = options.LoginPath;
            cookieOptions.LogoutPath = options.LogoutPath;
            cookieOptions.AccessDeniedPath = options.AccessDeniedPath;
            cookieOptions.ExpireTimeSpan = options.CookieExpiration;
            cookieOptions.SlidingExpiration = options.SlidingExpiration;
        });

        // Register admin authorization policy
        services.AddAuthorization(authOptions =>
        {
            authOptions.AddPolicy("CosmosIdentityAdmin", policy =>
                policy.RequireRole(options.AdminRoleName));
        });

        return builder;
    }
}
