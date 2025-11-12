using Benday.CosmosDb.Repositories;

namespace Benday.CosmosDb.Utilities;

/// <summary>
/// Builder pattern for creating CosmosConfig instances with a fluent API.
/// This provides a cleaner way to configure Cosmos DB settings without
/// dealing with multiple constructor parameters.
/// </summary>
public class CosmosConfigBuilder
{
    private readonly CosmosConfig _config;

    /// <summary>
    /// Initializes a new instance of the CosmosConfigBuilder.
    /// </summary>
    public CosmosConfigBuilder()
    {
        _config = new CosmosConfig
        {
            PartitionKey = CosmosDbConstants.DefaultPartitionKey
        };
    }

    /// <summary>
    /// Sets the Cosmos DB endpoint URL.
    /// </summary>
    /// <param name="endpoint">The endpoint URL</param>
    /// <returns>The builder instance for chaining</returns>
    public CosmosConfigBuilder WithEndpoint(string endpoint)
    {
        _config.Endpoint = endpoint;
        return this;
    }

    /// <summary>
    /// Sets the Cosmos DB account key for authentication.
    /// </summary>
    /// <param name="accountKey">The account key</param>
    /// <returns>The builder instance for chaining</returns>
    public CosmosConfigBuilder WithAccountKey(string accountKey)
    {
        _config.AccountKey = accountKey;
        return this;
    }

    /// <summary>
    /// Configures the builder to use DefaultAzureCredential for authentication
    /// instead of an account key.
    /// </summary>
    /// <returns>The builder instance for chaining</returns>
    public CosmosConfigBuilder UseDefaultAzureCredential()
    {
        _config.UseDefaultAzureCredential = true;
        _config.AccountKey = string.Empty;
        return this;
    }

    /// <summary>
    /// Sets the database configuration.
    /// </summary>
    /// <param name="databaseName">The name of the database</param>
    /// <param name="throughput">Optional throughput for the database (default: 400 RU/s)</param>
    /// <returns>The builder instance for chaining</returns>
    public CosmosConfigBuilder WithDatabase(string databaseName, int? throughput = null)
    {
        _config.DatabaseName = databaseName;
        if (throughput.HasValue)
        {
            _config.DatabaseThroughput = throughput.Value;
        }
        return this;
    }

    /// <summary>
    /// Sets the container configuration.
    /// </summary>
    /// <param name="containerName">The name of the container</param>
    /// <returns>The builder instance for chaining</returns>
    public CosmosConfigBuilder WithContainer(string containerName)
    {
        _config.ContainerName = containerName;
        return this;
    }

    /// <summary>
    /// Sets the partition key configuration.
    /// </summary>
    /// <param name="partitionKey">The partition key path(s), comma-separated for hierarchical keys</param>
    /// <param name="useHierarchical">Whether to use hierarchical partition keys</param>
    /// <returns>The builder instance for chaining</returns>
    public CosmosConfigBuilder WithPartitionKey(string partitionKey, bool useHierarchical = false)
    {
        _config.PartitionKey = partitionKey;
        _config.UseHierarchicalPartitionKey = useHierarchical;
        return this;
    }

    /// <summary>
    /// Enables hierarchical partition keys.
    /// </summary>
    /// <returns>The builder instance for chaining</returns>
    public CosmosConfigBuilder UseHierarchicalPartitionKeys()
    {
        _config.UseHierarchicalPartitionKey = true;
        return this;
    }

    /// <summary>
    /// Enables automatic creation of database and container structures if they don't exist.
    /// Note: This should typically be disabled in production environments.
    /// </summary>
    /// <returns>The builder instance for chaining</returns>
    public CosmosConfigBuilder WithCreateStructures()
    {
        _config.CreateStructures = true;
        return this;
    }

    /// <summary>
    /// Enables Gateway connection mode instead of Direct mode.
    /// Gateway mode can be useful for development or firewall-restricted environments.
    /// </summary>
    /// <returns>The builder instance for chaining</returns>
    public CosmosConfigBuilder UseGatewayMode()
    {
        _config.UseGatewayMode = true;
        return this;
    }

    /// <summary>
    /// Configures bulk execution settings.
    /// </summary>
    /// <param name="allowBulkExecution">Whether to allow bulk execution (default: true)</param>
    /// <returns>The builder instance for chaining</returns>
    public CosmosConfigBuilder WithBulkExecution(bool allowBulkExecution = true)
    {
        _config.AllowBulkExecution = allowBulkExecution;
        return this;
    }

    /// <summary>
    /// Creates a CosmosConfigBuilder from an existing CosmosConfig instance.
    /// Useful for modifying existing configurations.
    /// </summary>
    /// <param name="config">The existing configuration</param>
    /// <returns>A new builder with the existing configuration values</returns>
    public static CosmosConfigBuilder FromConfig(CosmosConfig config)
    {
        var builder = new CosmosConfigBuilder();
        builder._config.AccountKey = config.AccountKey;
        builder._config.Endpoint = config.Endpoint;
        builder._config.DatabaseName = config.DatabaseName;
        builder._config.ContainerName = config.ContainerName;
        builder._config.PartitionKey = config.PartitionKey;
        builder._config.CreateStructures = config.CreateStructures;
        builder._config.DatabaseThroughput = config.DatabaseThroughput;
        builder._config.UseGatewayMode = config.UseGatewayMode;
        builder._config.UseHierarchicalPartitionKey = config.UseHierarchicalPartitionKey;
        builder._config.AllowBulkExecution = config.AllowBulkExecution;
        builder._config.UseDefaultAzureCredential = config.UseDefaultAzureCredential;
        return builder;
    }

    /// <summary>
    /// Configures the builder with optimal settings for the Azure Cosmos DB Linux emulator.
    /// This sets Gateway mode (required), disables bulk execution (not supported), 
    /// enables structure creation (convenient for dev), and uses the standard emulator endpoint and key.
    /// </summary>
    /// <returns>The builder instance for chaining</returns>
    public CosmosConfigBuilder ForEmulator()
    {
        return WithEndpoint("https://localhost:8081/")
            .WithAccountKey("C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==")
            .UseGatewayMode()
            .WithBulkExecution(false)
            .WithCreateStructures();
    }

    /// <summary>
    /// Builds the CosmosConfig instance with the configured settings.
    /// </summary>
    /// <returns>The configured CosmosConfig instance</returns>
    /// <exception cref="InvalidOperationException">Thrown when required settings are missing</exception>
    public CosmosConfig Build()
    {
        ValidateConfiguration();
        return _config;
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_config.Endpoint))
        {
            throw new InvalidOperationException("Endpoint must be configured. Use WithEndpoint() to set it.");
        }

        if (!_config.UseDefaultAzureCredential && string.IsNullOrWhiteSpace(_config.AccountKey))
        {
            throw new InvalidOperationException("Either AccountKey must be configured or UseDefaultAzureCredential must be enabled.");
        }

        if (string.IsNullOrWhiteSpace(_config.DatabaseName))
        {
            throw new InvalidOperationException("Database name must be configured. Use WithDatabase() to set it.");
        }

        if (string.IsNullOrWhiteSpace(_config.ContainerName))
        {
            throw new InvalidOperationException("Container name must be configured. Use WithContainer() to set it.");
        }
    }
}