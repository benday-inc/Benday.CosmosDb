using Benday.CosmosDb.Repositories;
using Benday.CosmosDb.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace Benday.Identity.CosmosDb;

/// <summary>
/// Extension methods for registering Cosmos Identity services.
/// </summary>
public static class CosmosIdentityServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Cosmos DB infrastructure for identity: CosmosClient, repository options,
    /// and user/role store implementations. Call this before adding Identity framework services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="cosmosConfig">Cosmos DB configuration.</param>
    /// <param name="configureOptions">Optional action to configure identity-specific options (container names, etc.).</param>
    /// <returns>The resolved <see cref="CosmosIdentityOptions"/> for use by callers.</returns>
    public static CosmosIdentityOptions AddCosmosIdentityStores(
        this IServiceCollection services,
        CosmosConfig cosmosConfig,
        Action<CosmosIdentityOptions>? configureOptions = null)
    {
        var options = new CosmosIdentityOptions();

        // Default container names from the Cosmos config; callers can still override via configureOptions
        options.UsersContainerName = cosmosConfig.ContainerName;
        options.RolesContainerName = cosmosConfig.ContainerName;

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

        return options;
    }

    /// <summary>
    /// Registers Cosmos Identity services including user/role stores,
    /// ASP.NET Core Identity Core, and claims principal factory.
    /// Does not configure authentication or cookies — use AddCosmosIdentityWithUI()
    /// from Benday.Identity.CosmosDb.UI for cookie and Razor Pages support.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="cosmosConfig">Cosmos DB configuration.</param>
    /// <param name="configureOptions">Optional action to configure identity-specific options (container names, etc.).</param>
    /// <param name="configureIdentity">Optional action to configure ASP.NET Core Identity options (password rules, lockout, etc.).</param>
    /// <returns>An <see cref="IdentityBuilder"/> for further configuration (e.g., AddDefaultTokenProviders).</returns>
    public static IdentityBuilder AddCosmosIdentity(
        this IServiceCollection services,
        CosmosConfig cosmosConfig,
        Action<CosmosIdentityOptions>? configureOptions = null,
        Action<IdentityOptions>? configureIdentity = null)
    {
        services.AddCosmosIdentityStores(cosmosConfig, configureOptions);

        // Register Identity core services (does not configure authentication/cookies)
        var builder = services.AddIdentityCore<CosmosIdentityUser>(identityOptions =>
        {
            configureIdentity?.Invoke(identityOptions);
        })
        .AddRoles<CosmosIdentityRole>();

        // Register claims principal factory
        services.AddScoped<IUserClaimsPrincipalFactory<CosmosIdentityUser>,
            DefaultUserClaimsPrincipalFactory>();

        return builder;
    }
}
