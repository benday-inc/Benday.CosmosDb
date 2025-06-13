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

    public CosmosRegistrationHelper(
        IServiceCollection services,
        string connectionString,
        string databaseName,
        string containerName,
        bool createStructures,
        string? partitionKey = null, 
        bool useGatewayMode = false,
        bool useHierarchicalPartitionKey = false,
        bool allowBulkExecution = true, 
        bool useDefaultAzureCredential = false)
    {        
        _Services = services;
        ConnectionString = connectionString;
        DatabaseName = databaseName;
        ContainerName = containerName;
        WithCreateStructures = createStructures;
        UseGatewayMode = useGatewayMode;
        UseHierarchicalPartitionKey = useHierarchicalPartitionKey;
        AllowBulkExecution = allowBulkExecution;
        UseDefaultAzureCredential = useDefaultAzureCredential;

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