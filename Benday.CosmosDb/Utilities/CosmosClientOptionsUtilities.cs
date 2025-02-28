using Benday.CosmosDb.DomainModels;
using Benday.CosmosDb.Repositories;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Benday.CosmosDb.Utilities;


public static class CosmosClientOptionsUtilities
{
    /// <summary>
    /// Gets the default CosmosClientOptions for a Cosmos DB client that uses System.Text.Json.
    /// This version of the method uses the default JsonNamingPolicy for the JsonSerializerOptions
    /// </summary>
    /// <returns>Client options object</returns>
    public static CosmosClientOptions GetCosmosDbClientOptions()
    {
        return GetCosmosDbClientOptions(null);
    }

    /// <summary>
    /// Gets the default CosmosClientOptions for a Cosmos DB client that uses System.Text.Json
    /// and provides an option to customize the JsonNamingPolicy for the JsonSerializerOptions.
    /// </summary>
    /// <param name="jsonNamingPolicy">Naming policy or null to not use a policy</param>
    /// <returns></returns>
    public static CosmosClientOptions GetCosmosDbClientOptions(JsonNamingPolicy? jsonNamingPolicy)
    {
        var options = new CosmosClientOptions
        {
            Serializer = new SystemTextJsonCosmosSerializer(new JsonSerializerOptions
            {
                PropertyNamingPolicy = jsonNamingPolicy,
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                // Add additional JsonSerializerOptions settings as needed
            })
        };

        return options;
    }

    /// <summary>
    /// Configures a CosmosClient instance in the service collection.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="connectionString"></param>
    /// <param name="databaseName"></param>
    /// <param name="containerName"></param>
    /// <param name="partitionKey"></param>
    /// <param name="createStructures"></param>
    /// <param name="jsonNamingPolicy"></param>
    public static void ConfigureCosmosClient(
        this IServiceCollection services,
        string connectionString,
        string databaseName,
        string containerName,
        string partitionKey,
        bool createStructures,
        JsonNamingPolicy? jsonNamingPolicy = null)
    {
        var options = GetCosmosDbClientOptions(jsonNamingPolicy);

        services.AddSingleton(new CosmosClient(connectionString, options));
    }

    /// <summary>
    /// Configures a repository for a specific domain model entity type using default implementation of CosmosOwnedItemRepository.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="services">Services collection</param>
    /// <param name="connectionString">Connection string for cosmos db</param>
    /// <param name="databaseName">Database name</param>
    /// <param name="containerName">Container name</param>
    /// <param name="partitionKey">Partition key definition string</param>
    /// <param name="createStructures">Create structures as part of the instantiation of this repository class (NOTE: this should probably be false in production)</param>
    public static void ConfigureRepository<TEntity>(
        this IServiceCollection services,
        string connectionString,
        string databaseName,
        string containerName,
        string partitionKey,
        bool createStructures) where TEntity : OwnedItemBase, new()
    {
        services.RegisterOptionsForRepository<TEntity>(
            connectionString, databaseName, containerName, partitionKey, createStructures);

        services.AddTransient<IOwnedItemRepository<TEntity>, CosmosOwnedItemRepository<TEntity>>();
    }

    /// <summary>
    /// Configures a repository for a specific domain model entity type using a custom implementation of the repository.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TInterface"></typeparam>
    /// <typeparam name="TImplementation"></typeparam>
    /// <param name="services"></param>
    /// <param name="connectionString"></param>
    /// <param name="databaseName"></param>
    /// <param name="containerName"></param>
    /// <param name="partitionKey"></param>
    /// <param name="createStructures"></param>
    public static void ConfigureRepository<TEntity, TInterface, TImplementation>(
        this IServiceCollection services,
        string connectionString,
        string databaseName,
        string containerName,
        string partitionKey,
        bool createStructures)
        where TImplementation : class, TInterface
        where TInterface : class
    {
        services.RegisterOptionsForRepository<TEntity>(
            connectionString, databaseName, containerName, partitionKey, createStructures);

        services.AddTransient<TInterface, TImplementation>();
    }

    /// <summary>
    /// Registers options for a repository.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="services"></param>
    /// <param name="connectionString"></param>
    /// <param name="databaseName"></param>
    /// <param name="containerName"></param>
    /// <param name="partitionKey"></param>
    /// <param name="createStructures"></param>
    public static void RegisterOptionsForRepository<T>(
        this IServiceCollection services,
        string connectionString,
        string databaseName,
        string containerName,
        string partitionKey,
        bool createStructures)
    {
        services.AddOptions<CosmosRepositoryOptions<T>>().Configure(options =>
        {
            options.ConnectionString = connectionString;
            options.DatabaseName = databaseName;
            options.ContainerName = containerName;
            options.PartitionKey = partitionKey;
            options.WithCreateStructures = createStructures;
        });

        services.AddTransient<CosmosRepositoryOptions<T>>();
    }
}
