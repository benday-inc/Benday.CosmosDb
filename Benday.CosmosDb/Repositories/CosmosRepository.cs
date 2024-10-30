﻿using Benday.CosmosDb.DomainModels;
using Benday.CosmosDb.Utilities;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Benday.CosmosDb.Repositories;

/// <summary>
/// Basic implementation of a Cosmos DB repository. Provides basic CRUD operations for a Cosmos DB entity, manages the container instance, and provides common functionality for custom queries as protected values and methods.
/// </summary>
/// <typeparam name="T">Domain model type managed by this repository</typeparam>
public abstract class CosmosRepository<T> : IRepository<T> where T : class, ICosmosIdentity, new()
{
    /// <summary>
    /// Cosmos DB client instance. For performance reasons, this instance should be shared across the application.
    /// </summary>
    private readonly CosmosClient _Client;

    /// <summary>
    /// Configuration value to create Cosmos DB structures if they don't already exist.
    /// </summary>
    private readonly bool _WithCreateStructures;

    /// <summary>
    /// Reference to the cosmos database instance.
    /// </summary>
    private Database? _Database;

    /// <summary>
    /// Reference to the container instance.
    /// </summary>
    private Microsoft.Azure.Cosmos.Container? _Container;

    /// <summary>
    /// Configuration value for the database name.
    /// </summary>
    private readonly string _DatabaseName;

    /// <summary>
    /// Configuration value for the container in the database.
    /// </summary>
    private readonly string _ContainerName;

    /// <summary>
    /// Instance of the partition key for the container.
    /// </summary>
    private readonly PartitionKey _PartitionKey;

    /// <summary>
    /// Partition key strings for the container. This is used for constructing partition keys for queries.
    /// </summary>
    private readonly List<string> _PartitionKeyStrings = [];

    /// <summary>
    /// Get the discriminator value for the entity. By default this is the class name for the domain model type managed by this repository.
    /// </summary>
    public virtual string DiscriminatorValue => typeof(T).Name;

    /// <summary>
    /// Constructor for the repository.
    /// </summary>
    /// <param name="options">Configuration options</param>
    /// <param name="client">Cosmos Db client instance. NOTE: for performance reasons, this should probably be a singleton in the application.</param>
    /// <exception cref="ArgumentException"></exception>
    public CosmosRepository(IOptions<CosmosRepositoryOptions<T>> options, CosmosClient client)
    {
        var opts = options.Value;

        _Client = client;

        _WithCreateStructures = opts.WithCreateStructures;

        if (string.IsNullOrEmpty(opts.ConnectionString))
        {
            throw new ArgumentException($"{nameof(opts.ConnectionString)} is null or empty.", nameof(options));
        }

        if (string.IsNullOrEmpty(opts.ContainerName))
        {
            throw new ArgumentException($"{nameof(opts.ContainerName)} is null or empty.", nameof(options));
        }

        if (string.IsNullOrEmpty(opts.PartitionKey))
        {
            throw new ArgumentException($"{nameof(opts.PartitionKey)} is null or empty.", nameof(options));
        }

        _DatabaseName = opts.DatabaseName;
        _ContainerName = opts.ContainerName;

        _PartitionKey = CosmosDbUtilities.GetPartitionKey(opts.PartitionKey);
        _PartitionKeyStrings = CosmosDbUtilities.GetPartitionKeyStrings(opts.PartitionKey);
    }

    /// <summary>
    /// Get the container instance. This method will initialize the container if it is null.
    /// </summary>
    /// <returns>Reference to the container</returns>
    /// <exception cref="InvalidOperationException"></exception>
    protected async Task<Microsoft.Azure.Cosmos.Container> GetContainer()
    {
        await Initialize();

        return _Container is null ? throw new InvalidOperationException($"Container instance is null.") : _Container;
    }


    /// <summary>
    /// Delete an item from the Cosmos DB container.
    /// </summary>
    /// <param name="id">Id of the item</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task DeleteAsync(string id)
    {
        var container = await GetContainer();

        var itemToDelete = await GetByIdAsync(id) ?? throw new InvalidOperationException($"Item with id {id} not found.");
        var builder = new PartitionKeyBuilder();

        _ = builder.Add(itemToDelete.PartitionKey);
        _ = builder.Add(itemToDelete.DiscriminatorValue);
        // builder.Add(id);        

        var partitionKey = builder.Build();

        ItemResponse<T> response;
        try
        {
            response = await container.DeleteItemAsync<T>(id, partitionKey);
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Get all items in the repository. NOTE: this almost certainly performs a cross-partition query and should be used with caution.
    /// </summary>
    /// <returns>The matching items</returns>
    public async Task<IEnumerable<T>> GetAllAsync()
    {
        var container = await GetContainer();

        var query = $"SELECT * FROM c where c.{CosmosDbConstants.DiscriminatorPropertyName} = \"" + DiscriminatorValue + "\"";

        // Execute the query
        var resultSetIterator = container.GetItemQueryIterator<T>(query);

        var items = await GetResults(resultSetIterator, nameof(GetAllAsync));

        return items;
    }

    /// <summary>
    /// Gets the results from a query.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="queryDescription"></param>
    /// <returns></returns>
    protected async Task<List<T>> GetResults(
        IOrderedQueryable<T> query, string queryDescription)
    {
        var feedIterator = query.ToFeedIterator();

        var results = await GetResults(feedIterator, queryDescription);

        return results;
    }

    /// <summary>
    /// Gets a description for a query. By default, this will return the type name of the repository and the method name.
    /// </summary>
    /// <param name="methodName">Method that's calling the query</param>
    /// <returns></returns>
    protected string GetQueryDescription(string methodName)
    {
        return GetQueryDescription(GetType().Name, methodName);
    }

    /// <summary>
    /// Gets a description for a query. By default, this will return the type name of the repository and the method name as a formatted string.
    /// </summary>
    /// <param name="typeName">Name of the type</param>
    /// <param name="methodName">Name of the method</param>
    /// <returns>Formatted query description string</returns>
    protected string GetQueryDescription(string typeName, string methodName)
    {
        return $"{typeName} - {methodName}";
    }

    /// <summary>
    /// Get results from a query
    /// </summary>
    /// <param name="resultSetIterator">Feed iterator to read the results from</param>
    /// <param name="queryDescription">Description of this query for logging</param>
    /// <returns></returns>
    protected async Task<List<T>> GetResults(FeedIterator<T> resultSetIterator, string queryDescription)
    {
        var items = new List<T>();

        var totalRequestCharge = 0.0;

        while (resultSetIterator.HasMoreResults)
        {
            var response = await resultSetIterator.ReadNextAsync();

            var diagnostics = response.Diagnostics;
            var ruCharge = response.RequestCharge;

            Console.WriteLine($"Request Charge ({queryDescription}): {ruCharge}");

            totalRequestCharge += ruCharge;
           
            var isCrossPartition = IsCrossPartitionQuery(diagnostics);

            if (isCrossPartition)
            {
                Console.WriteLine($"*** WARNING ***: Cross-partition query for {queryDescription}");
            }

            items.AddRange(response);
        }

        Console.WriteLine($"Total request charge ({queryDescription}): {totalRequestCharge}");

        return items;
    }

    /// <summary>
    /// Attempt to determine if a query is a cross-partition query based on the diagnostics.
    /// </summary>
    /// <param name="diagnostics">Diagnostics for a query response</param>
    /// <returns>True if it detects a cross-partition query.</returns>
    protected bool IsCrossPartitionQuery(CosmosDiagnostics diagnostics)
    {
        // Convert the diagnostics to a string and analyze it
        string diagnosticsString = diagnostics.ToString();

        // Look for indicators of cross-partition query in the diagnostics
        bool isCrossPartition = diagnosticsString.Contains("cross partition", StringComparison.CurrentCultureIgnoreCase)
            || diagnosticsString.Contains("multiple partition key ranges", StringComparison.CurrentCultureIgnoreCase);

        if (isCrossPartition == true)
        {
            Console.WriteLine($"*** WARNING ***: Cross-partition query");
        }

        return isCrossPartition;
    }

    /// <summary>
    /// Get an item by its id. This method will return null if the item is not found.
    /// NOTE: this almost certainly performs a cross-partition query and should be used with caution.
    /// </summary>
    /// <param name="id">Id of the entity</param>
    /// <returns>The first matching entity</returns>
    public async Task<T?> GetByIdAsync(string id)
    {
        var container = await GetContainer();

        try
        {
            var item = container.GetItemLinqQueryable<T>(true).Where(x => x.Id == id && x.DiscriminatorValue == DiscriminatorValue).FirstOrDefault();

            return item;
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Initializes the repository. This method will create the database and container if they don't already exist.
    /// </summary>
    /// <returns></returns>
    protected async Task Initialize()
    {
        if (_Database == null || _Container == null)
        {
            if (_WithCreateStructures == true)
            {
                var createDb = await _Client.CreateDatabaseIfNotExistsAsync(_DatabaseName);

                _Database = createDb.Database;

                // Create a new container with partition key
                Console.WriteLine($"Creating container '{_ContainerName}' in database '{_DatabaseName}' with partition key '{_PartitionKey}'...");

                var containerProperties = new ContainerProperties(
                    id: _ContainerName,
                    partitionKeyPaths: _PartitionKeyStrings
                );

                var container = await _Database.CreateContainerIfNotExistsAsync(containerProperties);

                _Container = container.Container;
            }
            else
            {
                _Database = _Client.GetDatabase(_DatabaseName);
                _Container = _Database.GetContainer(_ContainerName);
            }
        }
    }

    /// <summary>
    /// Save an item to the Cosmos DB container. This method will perform an insert if the item does not exist, otherwise it will perform an update.
    /// </summary>
    /// <param name="saveThis">The item to save</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public virtual async Task<T> SaveAsync(T saveThis)
    {
        var container = await GetContainer();

        if (string.IsNullOrEmpty(saveThis.Id))
        {
            saveThis.Id = Guid.NewGuid().ToString();
        }

        var partitionKey = GetPartitionKey(saveThis);
        try
        {
            var response = await container.UpsertItemAsync(saveThis, partitionKey);

            return response == null
                ? throw new InvalidOperationException($"Response was null")
                : response.StatusCode is System.Net.HttpStatusCode.OK or
                                System.Net.HttpStatusCode.Created
                    ? saveThis
                    : throw new InvalidOperationException($"Response status code was {response.StatusCode}");
        }
        catch (CosmosException)
        {
            throw;
        }
    }

    /// <summary>
    /// Save a list of items to the Cosmos DB container. This method will perform an insert if the item does not exist, otherwise it will perform an update.
    /// Items are saved in batches of 50.
    /// </summary>
    /// <param name="items">Items to save</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public virtual async Task SaveAsync(IList<T> items)
    {
        if (items.Count == 0)
        {
            return;
        }
        else
        {
            var batches = BatchUtility.GetBatches(items, 50);

            foreach (var batch in batches)
            {
                var partitionKey = GetPartitionKey(batch.First());

                var container = await GetContainer();

                var cosmosBatch = container.CreateTransactionalBatch(partitionKey);

                foreach (var item in batch)
                {
                    cosmosBatch.UpsertItem(item);
                }

                using var response = await cosmosBatch.ExecuteAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var responseAsJson = JsonSerializer.Serialize(response);

                    throw new Exception($"Failed to save items.{Environment.NewLine}{responseAsJson}");
                }
            }

            return;
        }
    }

    /// <summary>
    /// Get the partition key for an item.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    protected virtual PartitionKey GetPartitionKey(T item)
    {
        return GetPartitionKey(item.PartitionKey, item.DiscriminatorValue);
    }

    /// <summary>
    /// Get the partition key for an item.
    /// </summary>
    /// <param name="partitionKey">Top-level partition key value</param>
    /// <param name="discriminatorValue">Second-level partition key value</param>
    /// <returns></returns>

    protected virtual PartitionKey GetPartitionKey(
        string partitionKey, string discriminatorValue)
    {
        var builder = new PartitionKeyBuilder();

        _ = builder.Add(partitionKey);
        _ = builder.Add(discriminatorValue);

        return builder.Build();
    }

    /// <summary>
    /// Creates a queryable for the repository with the specified partition key 
    /// configuration. This is the starting point for all custom LINQ queries built 
    /// off of this repository by child repository classes.
    /// </summary>
    /// <param name="firstLevelPartitionKeyValue">Value to use for the first-level partition key. NOTE: this is probably ownerId.</param>
    /// <returns>Starting queryable object for LINQ queries</returns>
    protected virtual async Task<IOrderedQueryable<T>> GetQueryable(
        string firstLevelPartitionKeyValue)
    {
        var pk = new PartitionKeyBuilder().Add(firstLevelPartitionKeyValue).Add(DiscriminatorValue).Build();

        var container = await GetContainer();

        var queryable =
            container.GetItemLinqQueryable<T>(true,
            requestOptions: new QueryRequestOptions() { PartitionKey = pk });

        return queryable ?? 
            throw new InvalidOperationException("Queryable object is null.");
    }
}

