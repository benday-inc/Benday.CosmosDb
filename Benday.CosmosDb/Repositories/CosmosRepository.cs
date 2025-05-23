﻿using System;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Benday.CosmosDb.DomainModels;
using Benday.CosmosDb.Utilities;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    /// Reference to the cosmos database instance.
    /// </summary>
    private Database? _Database;

    /// <summary>
    /// Reference to the container instance.
    /// </summary>
    private Microsoft.Azure.Cosmos.Container? _Container;

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


    protected ILogger Logger { get; }

    protected readonly CosmosRepositoryOptions<T> _Options;

    /// <summary>
    /// Constructor for the repository.
    /// </summary>
    /// <param name="options">Configuration options</param>
    /// <param name="client">Cosmos Db client instance. NOTE: for performance reasons, this should probably be a singleton in the application.</param>
    /// <exception cref="ArgumentException"></exception>
    public CosmosRepository(IOptions<CosmosRepositoryOptions<T>> options, CosmosClient client, ILogger logger)
    {
        Logger = logger;

        _Options = options.Value;

        _Client = client;

        _PartitionKey = CosmosDbUtilities.GetPartitionKey(
            _Options.PartitionKey, _Options.UseHierarchicalPartitionKey);

        _PartitionKeyStrings =
            CosmosDbUtilities.GetPartitionKeyStrings(_Options.PartitionKey);
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

            if (response == null)
            {
                throw new InvalidOperationException($"Response was null");
            }
            else
            {
                // print diagnostics
                var diagnostics = response.Diagnostics;
                var diagnosticsString = diagnostics.ToString();
                Logger.LogInformation($"Request Charge (DeleteAsync): {response.RequestCharge}");
                Logger.LogInformation($"Diagnostics (DeleteAsync): {diagnosticsString}");
                if (response.StatusCode is System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.NoContent)
                {
                    return;
                }
                else
                {
                    throw new InvalidOperationException($"Response status code was {response.StatusCode}");
                }
            }
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

        var queryable = await GetQueryable();

        var query = queryable.Queryable.Where(x => x.DiscriminatorValue == DiscriminatorValue);

        var items = await GetResults(query, nameof(GetAllAsync), queryable.PartitionKey);

        return items;
    }

    /// <summary>
    /// Gets the results from a query.
    /// </summary>
    /// <param name="query">query to run</param>
    /// <param name="queryDescription">logging description for the query</param>
    /// <param name="partitionKey">partition key that's configured for this query. NOTE: this is purely to logging purposes</param>
    /// <returns></returns>
    protected async Task<List<T>> GetResults(
        IQueryable<T> query, string queryDescription, PartitionKey partitionKey)
    {
        Logger.LogInformation($"{nameof(CosmosRepository<T>)}.{nameof(GetResults)} - {queryDescription} query {query} with partition key {partitionKey}");

        var feedIterator = query.ToFeedIterator();

        var results = await GetResults(feedIterator, queryDescription);

        return results;
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

            Logger.LogInformation($"Request Charge ({queryDescription}): {ruCharge}");

            totalRequestCharge += ruCharge;

            var isCrossPartition = IsCrossPartitionQuery(diagnostics);

            if (isCrossPartition)
            {
                Logger.LogInformation($"*** WARNING ***: Cross-partition query for {queryDescription}");
            }

            items.AddRange(response);
        }

        Logger.LogInformation($"Total request charge ({queryDescription}): {totalRequestCharge}");

        return items;
    }

    /// <summary>
    /// Gets a description for a query. By default, this will return the type 
    /// name of the repository and the method name. By default, detect and use 
    /// the method name of the caller.
    /// </summary>
    /// <param name="methodName">Method that's calling the query</param>
    /// <returns></returns>
    protected string GetQueryDescription([CallerMemberName] string methodName = "")
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
            Logger.LogWarning($"*** WARNING ***: Cross-partition query");
        }

        return isCrossPartition;
    }

    /// <summary>
    /// Get an item by its id. This method will return null if the item is not found.
    /// NOTE: this almost certainly performs a cross-partition query and should be used with caution because 
    /// it does not use a partition key.
    /// </summary>
    /// <param name="Id">Id of the entity</param>
    /// <returns>The first matching entity</returns>
    public async Task<T?> GetByIdAsync(string id)
    {
        var container = await GetContainer();

        try
        {
            var queryable = await GetQueryable();

            var query = queryable.Queryable.Where(x => x.Id == id && x.DiscriminatorValue == DiscriminatorValue);

            var result = await GetResults(query, nameof(GetByIdAsync), queryable.PartitionKey);

            var item = result.FirstOrDefault();

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
            if (_Options.WithCreateStructures == true)
            {
                try
                {
                    _Database = await CreateDatabaseIfNotExistsAsync();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error creating database '{_Options.DatabaseName}'.  {ex}");

                    throw;
                }

                try
                {
                    _Container = await CreateContainerIfNotExistsAsync();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error creating container '{_Options.ContainerName}' in database '{_Options.DatabaseName}'.  {ex}");

                    throw;
                }
            }
            else
            {
                _Database = _Client.GetDatabase(_Options.DatabaseName);
                _Container = _Database.GetContainer(_Options.ContainerName);
            }
        }
    }

    private async Task<Container> CreateContainerIfNotExistsAsync()
    {
        if (_Database == null)
        {
            throw new InvalidOperationException($"Database instance is null.");
        }

        // get list of containers

        var containers = _Database.GetContainerQueryIterator<ContainerProperties>();

        Container? match = null;

        while (containers.HasMoreResults == true && match == null)
        {
            var response = await containers.ReadNextAsync();

            foreach (var item in response)
            {
                if (item.Id == _Options.ContainerName)
                {
                    match = _Database.GetContainer(_Options.ContainerName);
                    break;
                }
            }
        }

        if (match != null)
        {
            Logger.LogInformation($"Container '{_Options.ContainerName}' already exists.");

            return match;
        }
        else
        {
            Logger.LogInformation($"Creating container '{_Options.ContainerName}' in database '{_Options.DatabaseName}' with partition key '{_PartitionKey}'...");

            ContainerProperties properties;

            if (_PartitionKeyStrings.Count == 0)
            {
                throw new InvalidOperationException($"Partition key strings is empty.");
            }
            else if (_PartitionKeyStrings.Count == 1 || _Options.UseHierarchicalPartitionKey == false)
            {
                Logger.LogInformation($"Creating container with partition key path '{_PartitionKeyStrings[0]}'.");

                properties = new ContainerProperties(
                    id: _Options.ContainerName,
                    partitionKeyPath: _PartitionKeyStrings[0]);
            }
            else
            {
                Logger.LogInformation($"Creating container with partition key paths '{string.Join(",", _PartitionKeyStrings)}'.");

                properties = new ContainerProperties(
                    id: _Options.ContainerName,
                    partitionKeyPaths: _PartitionKeyStrings
                );
            }

            try
            {
                var container = await _Database.CreateContainerAsync(properties);

                Logger.LogInformation($"Container '{_Options.ContainerName}' created.");

                return container;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error creating container '{_Options.ContainerName}' in database '{_Options.DatabaseName}'.  {ex}");

                throw;
            }
        }
    }

    private async Task<Database> CreateDatabaseIfNotExistsAsync()
    {
        // get the list of databases

        var databases = _Client.GetDatabaseQueryIterator<DatabaseProperties>();

        Database? match = null;

        while (databases.HasMoreResults)
        {
            var response = await databases.ReadNextAsync();

            foreach (var db in response)
            {
                if (db.Id == _Options.DatabaseName)
                {
                    match = _Client.GetDatabase(_Options.DatabaseName);
                }
            }
        }

        if (match != null)
        {
            Logger.LogInformation($"Database '{_Options.DatabaseName}' already exists.");

            return match;
        }
        else
        {
            Logger.LogInformation($"Creating database '{_Options.DatabaseName}'...");

            var response = await _Client.CreateDatabaseAsync(_Options.DatabaseName, throughput: _Options.DatabaseThroughput);

            Logger.LogInformation($"Database '{_Options.DatabaseName}' created.");

            return response.Database;
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
        ItemResponse<T>? response;

        try
        {
            var requestOptions = new ItemRequestOptions
            {
                IfMatchEtag = saveThis.Etag
            };

            response = await container.UpsertItemAsync(
                saveThis, partitionKey, requestOptions);

            if (response == null)
            {
                throw new InvalidOperationException($"Response was null");
            }
            else
            {
                // print diagnostics
                var diagnostics = response.Diagnostics;
                var diagnosticsString = diagnostics.ToString();

                Logger.LogInformation($"Request Charge (SaveAsync): {response.RequestCharge}");

                Logger.LogInformation($"Diagnostics (SaveAsync): {diagnosticsString}");

                if (response.StatusCode is
                    System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.Created)
                {
                    return saveThis;
                }
                else
                {
                    throw new InvalidOperationException($"Response status code was {response.StatusCode}");
                }
            }
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
        {
            Logger.LogWarning($"Precondition failed for item {saveThis.Id} in container {_Options.ContainerName} in database {_Options.DatabaseName}.  {ex}");

            throw new OptimisticConcurrencyException(
                $"Precondition failed for item {saveThis.Id} in container {_Options.ContainerName} in database {_Options.DatabaseName}.",
                ex);
        }
        catch (CosmosException ex)
        {
            Logger.LogError($"Error saving {saveThis.DiscriminatorValue} item {saveThis.Id} to container {_Options.ContainerName} in database {_Options.DatabaseName}.  {ex}");

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

        if (_Options.UseHierarchicalPartitionKey == true)
        {
            _ = builder.Add(discriminatorValue);
        }

        return builder.Build();
    }

    /// <summary>
    /// Creates a queryable for the repository with the specified partition key 
    /// configuration. This is the starting point for all custom LINQ queries built 
    /// off of this repository by child repository classes.
    /// </summary>
    /// <param name="firstLevelPartitionKeyValue">Value to use for the first-level partition key. NOTE: this is probably ownerId.</param>
    /// <returns>Queryable and it's configured partition key</returns>
    protected virtual async Task<QueryableInfo<T>> GetQueryable(
        string firstLevelPartitionKeyValue)
    {
        return await GetQueryable(firstLevelPartitionKeyValue, DiscriminatorValue);
    }

    /// <summary>
    /// Creates a queryable for the repository with the specified partition key 
    /// configuration. This is the starting point for all custom LINQ queries built 
    /// off of this repository by child repository classes.
    /// </summary>
    /// <param name="firstLevelPartitionKeyValue">Value to use for the first-level partition key. NOTE: this is probably ownerId.</param>
    /// <param name="discriminatorValue">Discriminator value</param>
    /// <returns>Queryable and it's configured partition key</returns>
    protected virtual async Task<QueryableInfo<T>> GetQueryable(
        string firstLevelPartitionKeyValue, string discriminatorValue)
    {
        var builder = new PartitionKeyBuilder();

        builder.Add(firstLevelPartitionKeyValue);

        if (_Options.UseHierarchicalPartitionKey == true)
        {
            builder.Add(discriminatorValue);
        }
        var pk = builder.Build();

        var container = await GetContainer();

        var queryable =
            container.GetItemLinqQueryable<T>(true,
            requestOptions: new QueryRequestOptions() { PartitionKey = pk },
            linqSerializerOptions: new CosmosLinqSerializerOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.Default
            });


        if (queryable == null)
        {
            throw new InvalidOperationException("Queryable object is null.");
        }

        var info = new QueryableInfo<T>(pk, queryable);

        return info;
    }

    /// <summary>
    /// Get the queryable object for the repository. This method will create a queryable object WITHOUT a partition key configuration for the repository.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    protected virtual async Task<QueryableInfo<T>> GetQueryable()
    {
        var container = await GetContainer();

        var queryable =
            container.GetItemLinqQueryable<T>(true,
            linqSerializerOptions: new CosmosLinqSerializerOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.Default
            });

        if (queryable == null)
        {
            throw new InvalidOperationException("Queryable object is null.");
        }

        var info = new QueryableInfo<T>(new PartitionKey(), queryable);

        return info;
    }
}
