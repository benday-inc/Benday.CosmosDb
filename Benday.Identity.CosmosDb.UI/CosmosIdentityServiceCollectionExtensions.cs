using Benday.CosmosDb.Repositories;
using Benday.CosmosDb.Utilities;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace Benday.Identity.CosmosDb.UI;

/// <summary>
/// Extension methods for registering Cosmos Identity services.
/// </summary>
public static class CosmosIdentityServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Cosmos Identity services including user/role stores,
    /// ASP.NET Core Identity, claims principal factory, and application cookie.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="cosmosConfig">Cosmos DB configuration.</param>
    /// <param name="configureOptions">Optional action to configure identity-specific options (container names, cookie settings, etc.).</param>
    /// <param name="configureIdentity">Optional action to configure ASP.NET Core Identity options (password rules, lockout, etc.).</param>
    /// <returns>An <see cref="IdentityBuilder"/> for further configuration (e.g., AddDefaultTokenProviders).</returns>
    public static IdentityBuilder AddCosmosIdentity(
        this IServiceCollection services,
        CosmosConfig cosmosConfig,
        Action<CosmosIdentityOptions>? configureOptions = null,
        Action<IdentityOptions>? configureIdentity = null)
    {
        var options = new CosmosIdentityOptions();
        configureOptions?.Invoke(options);

        // Register CosmosClient singleton if not already registered
        if (!services.Any(s => s.ServiceType == typeof(CosmosClient)))
        {
            services.ConfigureCosmosClient(cosmosConfig);
        }

        // Register repository options for users container
        services.RegisterOptionsForRepository<CosmosIdentityUser>(
            cosmosConfig.ConnectionString,
            cosmosConfig.DatabaseName,
            options.UsersContainerName,
            CosmosDbConstants.DefaultPartitionKey,
            cosmosConfig.CreateStructures,
            true,
            cosmosConfig.UseDefaultAzureCredential);

        // Register repository options for roles container
        services.RegisterOptionsForRepository<CosmosIdentityRole>(
            cosmosConfig.ConnectionString,
            cosmosConfig.DatabaseName,
            options.RolesContainerName,
            CosmosDbConstants.DefaultPartitionKey,
            cosmosConfig.CreateStructures,
            true,
            cosmosConfig.UseDefaultAzureCredential);

        // Register stores
        services.AddTransient<IUserStore<CosmosIdentityUser>, CosmosDbUserStore>();
        services.AddTransient<IRoleStore<CosmosIdentityRole>, CosmosDbRoleStore>();

        // Register ASP.NET Core Identity
        var builder = services.AddIdentity<CosmosIdentityUser, CosmosIdentityRole>(identityOptions =>
        {
            configureIdentity?.Invoke(identityOptions);
        });

        // Register claims principal factory
        services.AddScoped<IUserClaimsPrincipalFactory<CosmosIdentityUser>,
            DefaultUserClaimsPrincipalFactory>();

        // Configure application cookie
        services.ConfigureApplicationCookie(cookieOptions =>
        {
            cookieOptions.Cookie.Name = options.CookieName;
            cookieOptions.LoginPath = options.LoginPath;
            cookieOptions.LogoutPath = options.LogoutPath;
            cookieOptions.AccessDeniedPath = options.AccessDeniedPath;
            cookieOptions.ExpireTimeSpan = options.CookieExpiration;
            cookieOptions.SlidingExpiration = options.SlidingExpiration;
        });

        return builder;
    }
}
