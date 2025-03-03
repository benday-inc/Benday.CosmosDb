using Benday.CosmosDb.DomainModels;
using Benday.CosmosDb.Repositories;
using Benday.CosmosDb.ServiceLayers;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benday.CosmosDb.Utilities;


public class CosmosRegistrationHelper
{
    public string ConnectionString { get; private set; }

    public bool WithCreateStructures { get; private set; }
    public bool UseGatewayMode { get; private set; }

    public string DatabaseName { get; private set; } = string.Empty;

    public string ContainerName { get; private set; } = string.Empty;

    public string PartitionKey { get; private set; } = CosmosDbConstants.DefaultPartitionKey;

    private IServiceCollection _Services;

    public CosmosRegistrationHelper(IServiceCollection services, CosmosConfig config) : this(
        services,
        config.ConnectionString,
        config.DatabaseName,
        config.ContainerName,
        config.CreateStructures,
        config.PartitionKey, 
        config.UseGatewayMode)
    {
        
    }

    public CosmosRegistrationHelper(
        IServiceCollection services,
        string connectionString,
        string databaseName,
        string containerName,
        bool createStructures,
        string? partitionKey = null, 
        bool useGatewayMode = false)
    {        
        _Services = services;
        ConnectionString = connectionString;
        DatabaseName = databaseName;
        ContainerName = containerName;
        WithCreateStructures = createStructures;
        UseGatewayMode = useGatewayMode;

        if (partitionKey != null)
        {
            PartitionKey = partitionKey;
        }

        ConfigureClient();
    }

    /// <summary>
    /// Registers a repository for a specific domain model entity type.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public void RegisterRepository<TEntity>()
        where TEntity : OwnedItemBase, new()
    {
        _Services.ConfigureRepository<TEntity>(
            ConnectionString, DatabaseName, ContainerName, PartitionKey, WithCreateStructures);
    }

    /// <summary>
    /// Registers a repository for a specific domain model entity type using a custom implementation of the repository.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TInterface"></typeparam>
    /// <typeparam name="TImplementation"></typeparam>
    public void RegisterRepository<TEntity, TInterface, TImplementation>()
        where TEntity : OwnedItemBase, new()
        where TImplementation : class, TInterface
        where TInterface : class
    {
        _Services.ConfigureRepository<TEntity, TInterface, TImplementation>(
            ConnectionString, DatabaseName, ContainerName, PartitionKey, WithCreateStructures);
    }

    /// <summary>
    /// Registers a repository and service for a domain model entity type using default implementation of CosmosOwnedItemRepository and OwnedItemServiceBase.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public void RegisterRepositoryAndService<TEntity>()
        where TEntity : OwnedItemBase, new()
    {
        _Services.ConfigureRepository<TEntity>(
            ConnectionString, DatabaseName, ContainerName, PartitionKey, WithCreateStructures);

        _Services.AddTransient<IOwnedItemServiceBase<TEntity>, OwnedItemServiceBase<TEntity>>();
    }
    
    private void ConfigureClient()
    {
        _Services.ConfigureCosmosClient(
            ConnectionString, UseGatewayMode);
    }
}