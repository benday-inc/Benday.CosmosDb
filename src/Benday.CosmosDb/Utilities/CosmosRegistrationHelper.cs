using Benday.CosmosDb.Diagnostics;
using Benday.CosmosDb.DomainModels;
using Benday.CosmosDb.Repositories;
using Benday.CosmosDb.ServiceLayers;
using Benday.Common;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benday.CosmosDb.Utilities;


/// <summary>
/// Registers Cosmos DB repositories and services with the dependency injection
/// container. Reads default connection, database, container, and partition-key
/// settings from a <see cref="CosmosConfig"/> and exposes them as defaults that
/// each per-entity registration call can override.
/// </summary>
public class CosmosRegistrationHelper
{
    /// <summary>
    /// Cosmos DB account endpoint (or connection string) used when registering repositories.
    /// </summary>
    public string ConnectionString { get; private set; }

    /// <summary>
    /// When true, repositories create the database and container structures on startup if missing.
    /// </summary>
    public bool WithCreateStructures { get; private set; }

    /// <summary>
    /// When true, the Cosmos client connects in gateway mode instead of direct mode.
    /// </summary>
    public bool UseGatewayMode { get; private set; }

    /// <summary>
    /// When true, the Cosmos client enables bulk execution for higher-throughput batch operations. Defaults to true.
    /// </summary>
    public bool AllowBulkExecution { get; private set; } = true;

    /// <summary>
    /// When true, containers are created with a hierarchical partition key (default path <c>/tenantId,/entityType</c>).
    /// </summary>
    public bool UseHierarchicalPartitionKey { get; private set; }

    /// <summary>
    /// When true, the Cosmos client authenticates with <c>DefaultAzureCredential</c> instead of a connection-string key.
    /// </summary>
    public bool UseDefaultAzureCredential { get; private set; }

    /// <summary>
    /// Default database name used when registering repositories.
    /// </summary>
    public string DatabaseName { get; private set; } = string.Empty;

    /// <summary>
    /// Default container name used when registering repositories.
    /// </summary>
    public string ContainerName { get; private set; } = string.Empty;

    /// <summary>
    /// Default partition key path used when registering repositories.
    /// </summary>
    public string PartitionKey { get; private set; } = CosmosDbConstants.DefaultPartitionKey;

    private IServiceCollection _Services;

    /// <summary>
    /// The configuration this helper was constructed from, or null if constructed without one.
    /// </summary>
    public CosmosConfig? Configuration { get; private set; }

    /// <summary>
    /// Initializes a new instance that reads its defaults from <paramref name="config"/> and
    /// registers the shared Cosmos client on <paramref name="services"/>.
    /// </summary>
    /// <param name="services">The service collection to register repositories and the Cosmos client into.</param>
    /// <param name="config">Connection and container settings used as registration defaults.</param>
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
    /// <typeparam name="TEntity">Entity type that inherits from <see cref="TenantItemBase"/>.</typeparam>
    public void RegisterRepository<TEntity>()
        where TEntity : TenantItemBase, new()
    {
        _Services.ConfigureRepository<TEntity>(
            ConnectionString, DatabaseName, ContainerName, PartitionKey, WithCreateStructures,
            UseHierarchicalPartitionKey, UseDefaultAzureCredential);
    }

    /// <summary>
    /// Registers a repository for a specific domain model entity type using a custom implementation of the repository.
    /// </summary>
    /// <typeparam name="TEntity">Entity type that inherits from <see cref="TenantItemBase"/>.</typeparam>
    /// <typeparam name="TInterface">The repository interface to register.</typeparam>
    /// <typeparam name="TImplementation">The repository implementation to register.</typeparam>
    public void RegisterRepository<TEntity, TInterface, TImplementation>()
        where TEntity : TenantItemBase, new()
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
    /// <typeparam name="TEntity">Entity type that inherits from <see cref="TenantItemBase"/>.</typeparam>
    /// <typeparam name="TInterface">The repository interface to register.</typeparam>
    /// <typeparam name="TImplementation">The repository implementation to register.</typeparam>
    /// <param name="connectionString">Overrides the helper's connection string when set.</param>
    /// <param name="databaseName">Overrides the helper's database name when set.</param>
    /// <param name="containerName">Overrides the helper's container name when set.</param>
    /// <param name="partitionKey">Overrides the helper's partition key when set.</param>
    /// <param name="useHierarchicalPartitionKey">Overrides the helper's hierarchical partition key flag when set.</param>
    /// <param name="useDefaultAzureCredential">Overrides the helper's DefaultAzureCredential flag when set.</param>
    /// <param name="withCreateStructures">Overrides the helper's create-structures flag when set.</param>
    public void RegisterRepository<TEntity, TInterface, TImplementation>(
        string? connectionString = null,
        string? databaseName = null,
        string? containerName = null,
        string? partitionKey = null,
        bool? useHierarchicalPartitionKey = null,
        bool? useDefaultAzureCredential = null,
        bool? withCreateStructures = null
    )
        where TEntity : TenantItemBase, new()
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
    /// Registers a repository and service for a domain model entity type using default implementations.
    /// </summary>
    /// <typeparam name="TEntity">Entity type that inherits from <see cref="TenantItemBase"/>.</typeparam>
    public void RegisterRepositoryAndService<TEntity>()
        where TEntity : TenantItemBase, new()
    {
        _Services.ConfigureRepository<TEntity>(
            ConnectionString, DatabaseName, ContainerName, PartitionKey, WithCreateStructures, UseHierarchicalPartitionKey,
            UseDefaultAzureCredential);

        _Services.AddTransient<ITenantItemService<TEntity>, TenantItemService<TEntity>>();
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
    /// Registers a repository and service for a parented item entity type using default implementations.
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

    /// <summary>
    /// Registers a custom <see cref="ICosmosQueryLogSink"/> to receive
    /// query diagnostics from every repository. If no sink is explicitly
    /// registered, the default <see cref="NoOpCosmosQueryLogSink"/> is used.
    /// </summary>
    /// <typeparam name="TSink">
    /// The sink implementation type. Registered as a singleton.
    /// </typeparam>
    /// <returns>This helper, for fluent chaining.</returns>
    public CosmosRegistrationHelper WithQueryLogSink<TSink>()
        where TSink : class, ICosmosQueryLogSink
    {
        _Services.AddSingleton<ICosmosQueryLogSink, TSink>();
        return this;
    }

    /// <summary>
    /// Registers a specific <see cref="ICosmosQueryLogSink"/> instance to
    /// receive query diagnostics. Useful for test scenarios where a
    /// pre-constructed capture sink should be used.
    /// </summary>
    /// <param name="sink">The sink instance. Registered as a singleton.</param>
    /// <returns>This helper, for fluent chaining.</returns>
    public CosmosRegistrationHelper WithQueryLogSink(ICosmosQueryLogSink sink)
    {
        if (sink is null) throw new ArgumentNullException(nameof(sink));
        _Services.AddSingleton<ICosmosQueryLogSink>(sink);
        return this;
    }

    /// <summary>
    /// Configures per-entity diagnostic options for repositories that manage
    /// <typeparamref name="TEntity"/>. The options are looked up by the
    /// repository in its constructor via <see cref="CosmosDiagnosticsRegistry"/>.
    /// </summary>
    /// <typeparam name="TEntity">The repository's entity type.</typeparam>
    /// <param name="configure">Callback that receives the entity's options
    /// for mutation. Called once per call.</param>
    /// <returns>This helper, for fluent chaining.</returns>
    /// <example>
    /// <code>
    /// helper.ConfigureDiagnostics&lt;Note&gt;(o =&gt; o.CaptureIndexMetrics = true);
    /// </code>
    /// </example>
    public CosmosRegistrationHelper ConfigureDiagnostics<TEntity>(
        Action<CosmosRepositoryDiagnosticsOptions> configure)
    {
        if (configure is null) throw new ArgumentNullException(nameof(configure));

        GetOrAddDiagnosticsRegistry().Set<TEntity>(configure);
        return this;
    }

    /// <summary>
    /// Configures the registry default that applies to any entity type that
    /// does not have an explicit <see cref="ConfigureDiagnostics{TEntity}"/>
    /// entry. Useful for turning a flag on globally and then disabling it
    /// for hot-path repositories.
    /// </summary>
    /// <param name="configure">Callback that mutates the default options.</param>
    /// <returns>This helper, for fluent chaining.</returns>
    public CosmosRegistrationHelper ConfigureDiagnosticsDefault(
        Action<CosmosRepositoryDiagnosticsOptions> configure)
    {
        if (configure is null) throw new ArgumentNullException(nameof(configure));

        GetOrAddDiagnosticsRegistry().SetDefault(configure);
        return this;
    }

    private CosmosDiagnosticsRegistry GetOrAddDiagnosticsRegistry()
    {
        // ConfigureCosmosClient registers a default singleton via TryAddSingleton,
        // but ConfigureDiagnostics may be called before that path runs in tests
        // or in callers that wire DI manually. Look up the existing registration;
        // if none, add one and return it.
        foreach (var descriptor in _Services)
        {
            if (descriptor.ServiceType == typeof(CosmosDiagnosticsRegistry) &&
                descriptor.ImplementationInstance is CosmosDiagnosticsRegistry existing)
            {
                return existing;
            }
        }

        var registry = new CosmosDiagnosticsRegistry();
        _Services.AddSingleton(registry);
        return registry;
    }
}
