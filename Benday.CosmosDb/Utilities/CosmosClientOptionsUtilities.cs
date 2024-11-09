using Benday.CosmosDb.Repositories;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json;

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
                WriteIndented = true
                // Add additional JsonSerializerOptions settings as needed
            })
        };

        return options;
    }

    public static void ConfigureCosmosClient(
        this IServiceCollection services,
        string connectionString,
        string databaseName,
        string containerName,
        string partitionKey,
        bool createStructures)
    {
        var options = GetCosmosDbClientOptions();

        services.AddSingleton(new CosmosClient(connectionString, options));
    }

    public static void ConfigureRepository<TInterface, TImplementation>(
        this IServiceCollection services,
        string connectionString,
        string databaseName,
        string containerName,
        string partitionKey,
        bool createStructures) 
        where TImplementation : class, TInterface
        where TInterface : class
    {
        services.RegisterOptionsForRepository<TImplementation>(
            connectionString, databaseName, containerName, partitionKey, createStructures);

        services.AddTransient<TInterface, TImplementation>();
    }

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
