using Benday.CosmosDb.DomainModels;
using Benday.CosmosDb.Repositories;
using Benday.CosmosDb.ServiceLayers;
using Benday.Common;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benday.CosmosDb.Utilities;


public class CosmosRegistrationHelper
{
    public string ConnectionString { get; private set; }

    public bool WithCreateStructures { get; private set; }
    public bool UseGatewayMode { get; private set; }
    public bool AllowBulkExecution { get; private set; } = true;
    public bool UseHierarchicalPartitionKey { get; private set; }
    public bool UseDefaultAzureCredential { get; private set; }

    public string DatabaseName { get; private set; } = string.Empty;

    public string ContainerName { get; private set; } = string.Empty;

    public string PartitionKey { get; private set; } = CosmosDbConstants.DefaultPartitionKey;

    private IServiceCollection _Services;
    
    public CosmosConfig? Configuration { get; private set; }

    public CosmosRegistrationHelper(IServiceCollection services, CosmosConfig config)
    {
        _Services = services;
        Configuration = config;
        ConnectionString = config.Endpoint;
        DatabaseName = config.DatabaseName;
        ContainerName = config.ContainerName;
        PartitionKey = config.PartitionKey;
        WithCreateStructures = config.CreateStructures;
        UseGatewayMode = config.UseGatewayMode;
        UseHierarchicalPartitionKey = config.UseHierarchicalPartitionKey;
        AllowBulkExecution = config.AllowBulkExecution;
        UseDefaultAzureCredential = config.UseDefaultAzureCredential;

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
            ConnectionString, DatabaseName, ContainerName, PartitionKey, WithCreateStructures,
            UseHierarchicalPartitionKey, UseDefaultAzureCredential);
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
            ConnectionString, DatabaseName, ContainerName, PartitionKey, WithCreateStructures,
            UseHierarchicalPartitionKey, UseDefaultAzureCredential);
    }

    /// <summary>
    /// Registers a repository for a specific domain model entity type using a custom configuration values.
    /// Null or empty values will use the defaults from the helper instance.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TInterface"></typeparam>
    /// <typeparam name="TImplementation"></typeparam>
    /// <param name="connectionString"></param>
    /// <param name="databaseName"></param>
    /// <param name="containerName"></param>
    /// <param name="partitionKey"></param>
    /// <param name="useHierarchicalPartitionKey"></param>
    /// <param name="useDefaultAzureCredential"></param>
    /// <param name="withCreateStructures"></param>
    public void RegisterRepository<TEntity, TInterface, TImplementation>(
        string? connectionString = null,
        string? databaseName = null,
        string? containerName = null, 
        string? partitionKey = null,
        bool? useHierarchicalPartitionKey = null,
        bool? useDefaultAzureCredential = null,
        bool? withCreateStructures = null
    )
        where TEntity : OwnedItemBase, new()
        where TImplementation : class, TInterface
        where TInterface : class
    {
        if (connectionString.IsNullOrWhitespace() == true)
        {
            connectionString = ConnectionString;
        }

        if (databaseName.IsNullOrWhitespace() == true)
        {
            databaseName = DatabaseName;
        }

        if (containerName.IsNullOrWhitespace() == true)
        {
            containerName = ContainerName;
        }

        if (partitionKey.IsNullOrWhitespace() == true)
        {
            partitionKey = PartitionKey;
        }

        if (useHierarchicalPartitionKey.HasValue == false)
        {
            useHierarchicalPartitionKey = UseHierarchicalPartitionKey;
        }

        if (useDefaultAzureCredential.HasValue == false)
        {
            useDefaultAzureCredential = UseDefaultAzureCredential;
        }

        if (withCreateStructures.HasValue == false)
        {
            withCreateStructures = WithCreateStructures;
        }

        _Services.ConfigureRepository<TEntity, TInterface, TImplementation>(
            connectionString: connectionString, 
            databaseName: databaseName, 
            containerName: containerName, 
            partitionKey: partitionKey, 
            createStructures: withCreateStructures.Value,
            useHierarchicalPartitionKey: useHierarchicalPartitionKey.Value, 
            useDefaultAzureCredential: useDefaultAzureCredential.Value);
    }

    /// <summary>
    /// Registers a repository and service for a domain model entity type using default implementation of CosmosOwnedItemRepository and OwnedItemServiceBase.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public void RegisterRepositoryAndService<TEntity>()
        where TEntity : OwnedItemBase, new()
    {
        _Services.ConfigureRepository<TEntity>(
            ConnectionString, DatabaseName, ContainerName, PartitionKey, WithCreateStructures, UseHierarchicalPartitionKey,
            UseDefaultAzureCredential);

        _Services.AddTransient<IOwnedItemService<TEntity>, OwnedItemService<TEntity>>();
    }

    /// <summary>
    /// Registers a repository for a specific parented item entity type using default implementation of CosmosDbParentedItemRepository.
    /// </summary>
    /// <typeparam name="TEntity">Entity type that inherits from ParentedItemBase</typeparam>
    public void RegisterParentedRepository<TEntity>()
        where TEntity : ParentedItemBase, new()
    {
        _Services.ConfigureParentedRepository<TEntity>(
            ConnectionString, DatabaseName, ContainerName, PartitionKey, WithCreateStructures,
            UseHierarchicalPartitionKey, UseDefaultAzureCredential);
    }

    /// <summary>
    /// Registers a repository for a specific parented item entity type using a custom implementation of the repository.
    /// </summary>
    /// <typeparam name="TEntity">Entity type that inherits from ParentedItemBase</typeparam>
    /// <typeparam name="TInterface">Repository interface</typeparam>
    /// <typeparam name="TImplementation">Repository implementation</typeparam>
    public void RegisterParentedRepository<TEntity, TInterface, TImplementation>()
        where TEntity : ParentedItemBase, new()
        where TImplementation : class, TInterface
        where TInterface : class
    {
        _Services.ConfigureParentedRepository<TEntity, TInterface, TImplementation>(
            ConnectionString, DatabaseName, ContainerName, PartitionKey, WithCreateStructures,
            UseHierarchicalPartitionKey, UseDefaultAzureCredential);
    }

    /// <summary>
    /// Registers a repository and service for a parented item entity type using default implementation of CosmosDbParentedItemRepository and ParentedItemService.
    /// </summary>
    /// <typeparam name="TEntity">Entity type that inherits from ParentedItemBase</typeparam>
    public void RegisterParentedRepositoryAndService<TEntity>()
        where TEntity : ParentedItemBase, new()
    {
        _Services.ConfigureParentedRepository<TEntity>(
            ConnectionString, DatabaseName, ContainerName, PartitionKey, WithCreateStructures, UseHierarchicalPartitionKey,
            UseDefaultAzureCredential);

        _Services.AddTransient<IParentedItemService<TEntity>, ParentedItemService<TEntity>>();
    }

    private void ConfigureClient()
    {
        if (Configuration != null)
        {
            _Services.ConfigureCosmosClient(Configuration);
        }
        else
        {
            _Services.ConfigureCosmosClient(
                ConnectionString, UseGatewayMode, AllowBulkExecution);
        }
    }
}