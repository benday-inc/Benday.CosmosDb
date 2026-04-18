using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Benday.CosmosDb.Diagnostics;
using Benday.CosmosDb.DomainModels;
using Benday.CosmosDb.Exceptions;
using Benday.CosmosDb.Utilities;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Benday.CosmosDb.Repositories;

/// <summary>
/// Basic implementation of a Cosmos DB repository. Provides basic CRUD operations for a Cosmos DB entity, manages the container instance, and provides common functionality for custom queries as protected values and methods.
/// </summary>
/// <remarks>
/// Derived classes can override <see cref="OnQueryDiagnostics"/> to route
/// structured diagnostics to additional sinks (a file, a test capture, an
/// observability pipeline, etc.). The hook fires for every query execution
/// event — point operations, per-page feed responses, and query totals —
/// distinguished by <see cref="CosmosQueryDiagnostics.EventKind"/>. Overriding
/// the hook does not affect the base <see cref="ILogger"/> output.
/// </remarks>
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
    /// Get the entity type value for this repository. By default this is the class name for the domain model type managed by this repository.
    /// </summary>
    public virtual string EntityType => typeof(T).Name;


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
    protected async Task<Microsoft.Azure.Cosmos.Container> GetContainerAsync()
    {
        await Initialize();

        return _Container is null ? throw new CosmosDbConfigurationException("Container instance is null. Initialization may have failed.") : _Container;
    }


    /// <summary>
    /// Delete an item from the Cosmos DB container.
    /// </summary>
    /// <param name="id">Id of the item</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task DeleteAsync(string id)
    {
        var container = await GetContainerAsync();

        var itemToDelete = await GetByIdAsync(id) ?? throw new CosmosDbItemNotFoundException(id, _Options.ContainerName);

        var partitionKey = GetPartitionKey(itemToDelete);

        ItemResponse<T> response;
        try
        {
            var stopwatch = Stopwatch.StartNew();
            response = await container.DeleteItemAsync<T>(id, partitionKey);
            stopwatch.Stop();

            if (response == null)
            {
                throw new CosmosDbException($"Delete operation for item with id {id} returned null response");
            }
            else
            {
                LogPointOperationDiagnostics(nameof(DeleteAsync), response.RequestCharge, response.Diagnostics, stopwatch.Elapsed);
                if (response.StatusCode is System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.NoContent)
                {
                    return;
                }
                else
                {
                    throw new CosmosDbException($"Response status code was {response.StatusCode}");
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
        var container = await GetContainerAsync();


#pragma warning disable CS0618 // Intentional cross-partition query at the base repository level
        var queryContext = await GetQueryContextAsync();
#pragma warning restore CS0618

        var query = queryContext.Queryable.Where(x => x.EntityType == EntityType);

        var items = await GetResultsAsync(query, nameof(GetAllAsync), queryContext.PartitionKey);

        return items;
    }

    /// <summary>
    /// Executes a LINQ query and returns all results, logging per-page and
    /// total diagnostics through <see cref="OnQueryDiagnostics"/>.
    /// </summary>
    /// <param name="query">query to run</param>
    /// <param name="queryDescription">logging description for the query</param>
    /// <param name="partitionKey">partition key that's configured for this query. NOTE: this is purely to logging purposes</param>
    /// <returns>All matching items.</returns>
    /// <seealso cref="GetResultsAsync(QueryDefinition, string, PartitionKey)"/>
    /// <seealso cref="ExecuteScalarAsync{TResult}(IQueryable{T}, Func{IQueryable{T}, Task{Response{TResult}}}, string, PartitionKey, Func{TResult, int}?)"/>
    protected async Task<List<T>> GetResultsAsync(
        IQueryable<T> query, string queryDescription, PartitionKey partitionKey)
    {
        var queryDefinition = query.ToQueryDefinition();
        var queryText = queryDefinition.QueryText;
        var parameters = ExtractParameters(queryDefinition);

        Logger.LogInformation($"{nameof(CosmosRepository<T>)}.{nameof(GetResultsAsync)} - {queryDescription} query {{\"query\":\"{queryText}\"}} with partition key {partitionKey}");

        var feedIterator = query.ToFeedIterator();

        var results = await GetResultsAsync(
            feedIterator, queryDescription, queryText, parameters, partitionKey);

        return results;
    }

    /// <summary>
    /// Executes a raw Cosmos SQL query and returns all results, logging the
    /// same diagnostics as the LINQ overload so raw-SQL queries and LINQ
    /// queries show up symmetrically in structured logs.
    /// </summary>
    /// <remarks>
    /// Use this when a query can't be expressed in LINQ — cross-apply joins
    /// over nested arrays, dynamic EXISTS clauses, VectorDistance, conditional
    /// aggregation, etc. The library's LINQ support handles the common case;
    /// this overload covers the cases it can't.
    /// </remarks>
    /// <param name="query">The parameterized Cosmos SQL query to run.</param>
    /// <param name="queryDescription">Logging description for the query.</param>
    /// <param name="partitionKey">Partition key scope for the query.</param>
    /// <returns>All matching items.</returns>
    /// <seealso cref="GetResultsAsync(IQueryable{T}, string, PartitionKey)"/>
    protected async Task<List<T>> GetResultsAsync(
        QueryDefinition query, string queryDescription, PartitionKey partitionKey)
    {
        var container = await GetContainerAsync();

        var queryText = query.QueryText;
        var parameters = ExtractParameters(query);

        Logger.LogInformation($"{nameof(CosmosRepository<T>)}.{nameof(GetResultsAsync)} - {queryDescription} query {{\"query\":\"{queryText}\"}} with partition key {partitionKey}");

        using var iterator = container.GetItemQueryIterator<T>(
            query,
            requestOptions: new QueryRequestOptions { PartitionKey = partitionKey });

        return await GetResultsAsync(
            iterator, queryDescription, queryText, parameters, partitionKey);
    }

    /// <summary>
    /// Reads all pages from a feed iterator, timing each page, accumulating
    /// totals, and emitting per-page and total diagnostics through
    /// <see cref="OnQueryDiagnostics"/>.
    /// </summary>
    /// <param name="resultSetIterator">Feed iterator to read the results from.</param>
    /// <param name="queryDescription">Description of this query for logging.</param>
    /// <param name="queryText">The generated SQL text (for diagnostics). Optional.</param>
    /// <param name="parameters">Query parameters (for diagnostics). Optional.</param>
    /// <param name="partitionKey">Partition key scope for the query (for diagnostics).</param>
    /// <returns>All items from the iterator.</returns>
    protected async Task<List<T>> GetResultsAsync(
        FeedIterator<T> resultSetIterator,
        string queryDescription,
        string? queryText = null,
        IReadOnlyDictionary<string, object?>? parameters = null,
        PartitionKey partitionKey = default)
    {
        var items = new List<T>();

        var totalRequestCharge = 0.0;
        var totalDuration = TimeSpan.Zero;
        var anyCrossPartition = false;

        while (resultSetIterator.HasMoreResults)
        {
            var pageStopwatch = Stopwatch.StartNew();
            var response = await resultSetIterator.ReadNextAsync();
            pageStopwatch.Stop();

            totalRequestCharge += response.RequestCharge;
            totalDuration += pageStopwatch.Elapsed;

            var isPageCrossPartition = LogFeedResponseDiagnostics(
                queryDescription,
                response.RequestCharge,
                response.Diagnostics,
                pageStopwatch.Elapsed,
                response.Count,
                queryText,
                parameters,
                partitionKey);

            anyCrossPartition |= isPageCrossPartition;

            items.AddRange(response);
        }

        LogQueryTotalDiagnostics(
            queryDescription,
            totalRequestCharge,
            totalDuration,
            items.Count,
            queryText,
            parameters,
            partitionKey,
            anyCrossPartition);

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
    protected virtual bool IsCrossPartitionQuery(CosmosDiagnostics diagnostics)
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
    /// Logs diagnostics for a point operation (save, delete, point-read)
    /// and fires <see cref="OnQueryDiagnostics"/> with a
    /// <see cref="CosmosQueryEventKind.PointOperation"/> event.
    /// </summary>
    /// <param name="operationName">Name of the operation for log messages.</param>
    /// <param name="requestCharge">RU charge from the response.</param>
    /// <param name="diagnostics">Cosmos diagnostics from the response.</param>
    /// <param name="duration">Wall-clock duration of the SDK round trip.</param>
    protected void LogPointOperationDiagnostics(
        string operationName, double requestCharge, CosmosDiagnostics diagnostics, TimeSpan duration)
    {
        var diagnosticsString = diagnostics.ToString();
        Logger.LogInformation($"Request Charge ({operationName}): {requestCharge}");
        Logger.LogInformation($"Diagnostics ({operationName}): {diagnosticsString}");

        OnQueryDiagnostics(new CosmosQueryDiagnostics
        {
            EventKind = CosmosQueryEventKind.PointOperation,
            Timestamp = DateTimeOffset.UtcNow,
            RepositoryName = GetType().Name,
            QueryDescription = GetQueryDescription(operationName),
            RequestCharge = requestCharge,
            Duration = duration,
        });
    }

    /// <summary>
    /// Logs diagnostics for a single page of feed iterator results (including
    /// cross-partition query detection) and fires <see cref="OnQueryDiagnostics"/>
    /// with a <see cref="CosmosQueryEventKind.FeedResponsePage"/> event.
    /// </summary>
    /// <param name="queryDescription">Description of the query for log messages.</param>
    /// <param name="requestCharge">RU charge for this page.</param>
    /// <param name="diagnostics">Cosmos diagnostics for this page.</param>
    /// <param name="duration">Wall-clock duration of this page's round trip.</param>
    /// <param name="resultCount">Number of documents returned on this page.</param>
    /// <param name="queryText">The generated SQL text.</param>
    /// <param name="parameters">Query parameters.</param>
    /// <param name="partitionKey">Partition key scope for this query.</param>
    /// <returns>Whether the page was detected as a cross-partition execution.</returns>
    protected bool LogFeedResponseDiagnostics(
        string queryDescription,
        double requestCharge,
        CosmosDiagnostics diagnostics,
        TimeSpan duration,
        int resultCount,
        string? queryText,
        IReadOnlyDictionary<string, object?>? parameters,
        PartitionKey partitionKey)
    {
        Logger.LogInformation($"Request Charge ({queryDescription}): {requestCharge}");

        var isCrossPartition = IsCrossPartitionQuery(diagnostics);

        if (isCrossPartition)
        {
            Logger.LogWarning($"Cross-partition query detected for {queryDescription}. This may impact performance.");
        }

        OnQueryDiagnostics(new CosmosQueryDiagnostics
        {
            EventKind = CosmosQueryEventKind.FeedResponsePage,
            Timestamp = DateTimeOffset.UtcNow,
            RepositoryName = GetType().Name,
            QueryDescription = queryDescription,
            QueryText = queryText,
            Parameters = parameters,
            PartitionKey = partitionKey,
            RequestCharge = requestCharge,
            Duration = duration,
            ResultCount = resultCount,
            IsCrossPartition = isCrossPartition,
        });

        return isCrossPartition;
    }

    /// <summary>
    /// Logs the total RU charge for a completed query and fires
    /// <see cref="OnQueryDiagnostics"/> with a
    /// <see cref="CosmosQueryEventKind.QueryTotal"/> event.
    /// </summary>
    /// <param name="queryDescription">Description of the query for log messages.</param>
    /// <param name="totalRequestCharge">Accumulated RU charge across all pages.</param>
    /// <param name="totalDuration">Accumulated round-trip duration across all pages.</param>
    /// <param name="totalResultCount">Total document count across all pages.</param>
    /// <param name="queryText">The generated SQL text.</param>
    /// <param name="parameters">Query parameters.</param>
    /// <param name="partitionKey">Partition key scope for this query.</param>
    /// <param name="isCrossPartition">OR across all pages of the cross-partition flag.</param>
    protected void LogQueryTotalDiagnostics(
        string queryDescription,
        double totalRequestCharge,
        TimeSpan totalDuration,
        int totalResultCount,
        string? queryText,
        IReadOnlyDictionary<string, object?>? parameters,
        PartitionKey partitionKey,
        bool isCrossPartition)
    {
        Logger.LogInformation($"Total request charge ({queryDescription}): {totalRequestCharge}");

        OnQueryDiagnostics(new CosmosQueryDiagnostics
        {
            EventKind = CosmosQueryEventKind.QueryTotal,
            Timestamp = DateTimeOffset.UtcNow,
            RepositoryName = GetType().Name,
            QueryDescription = queryDescription,
            QueryText = queryText,
            Parameters = parameters,
            PartitionKey = partitionKey,
            RequestCharge = totalRequestCharge,
            Duration = totalDuration,
            ResultCount = totalResultCount,
            IsCrossPartition = isCrossPartition,
        });
    }

    /// <summary>
    /// Called for every query execution event: point operations, feed
    /// response pages, and query totals. Override in derived classes to
    /// route diagnostics to additional sinks (structured logging, tests,
    /// an observability pipeline, etc.). The base <see cref="ILogger"/>
    /// output is not affected by overriding this.
    /// </summary>
    /// <param name="diagnostics">Structured payload describing the event.</param>
    protected virtual void OnQueryDiagnostics(CosmosQueryDiagnostics diagnostics)
    {
    }

    /// <summary>
    /// Copies the parameters of a <see cref="QueryDefinition"/> into a
    /// read-only dictionary for inclusion in <see cref="CosmosQueryDiagnostics.Parameters"/>.
    /// </summary>
    private static IReadOnlyDictionary<string, object?> ExtractParameters(
        QueryDefinition definition)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var param in definition.GetQueryParameters())
        {
            dict[param.Name] = param.Value;
        }
        return dict;
    }

    /// <summary>
    /// Executes a scalar Cosmos SDK LINQ operation (such as <c>CountAsync</c>,
    /// <c>FirstOrDefaultAsync</c>, <c>MaxAsync</c>) through the library's
    /// diagnostics pipeline. The SDK's extension methods on <see cref="IQueryable"/>
    /// bypass this pipeline when called directly; this helper lets you call
    /// them while still capturing request charge, timing, query text, and
    /// cross-partition warnings in the same format as list queries.
    /// </summary>
    /// <typeparam name="TResult">The return type of the SDK operation.</typeparam>
    /// <param name="query">The <see cref="IQueryable{T}"/> against which to execute the operation.</param>
    /// <param name="operation">
    /// A delegate that invokes the desired SDK extension method on the queryable
    /// and returns its <see cref="Response{T}"/>.
    /// </param>
    /// <param name="queryDescription">Logging description for the query.</param>
    /// <param name="partitionKey">Partition key scope for the query.</param>
    /// <param name="resultCountSelector">
    /// Optional: a delegate that maps the scalar result to an integer count for
    /// diagnostics purposes. Defaults to 1 if the result is non-null or a value
    /// type; 0 if the result is null. For most scalar operations the default
    /// is fine.
    /// </param>
    /// <returns>The resource value from the SDK's <see cref="Response{T}"/>.</returns>
    /// <example>
    /// <code>
    /// public async Task&lt;int&gt; GetCountAsync(string tenantId)
    /// {
    ///     var queryContext = await GetQueryContextAsync(tenantId);
    ///     return await ExecuteScalarAsync(
    ///         queryContext.Queryable,
    ///         q =&gt; q.CountAsync(),
    ///         GetQueryDescription(nameof(GetCountAsync)),
    ///         queryContext.PartitionKey);
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="GetResultsAsync(IQueryable{T}, string, PartitionKey)"/>
    protected async Task<TResult> ExecuteScalarAsync<TResult>(
        IQueryable<T> query,
        Func<IQueryable<T>, Task<Response<TResult>>> operation,
        string queryDescription,
        PartitionKey partitionKey,
        Func<TResult, int>? resultCountSelector = null)
    {
        var queryDefinition = query.ToQueryDefinition();
        var queryText = queryDefinition.QueryText;
        var parameters = ExtractParameters(queryDefinition);

        Logger.LogInformation($"{nameof(CosmosRepository<T>)}.{nameof(ExecuteScalarAsync)} - {queryDescription} query {{\"query\":\"{queryText}\"}} with partition key {partitionKey}");

        var stopwatch = Stopwatch.StartNew();
        var response = await operation(query);
        stopwatch.Stop();

        var isCrossPartition = IsCrossPartitionQuery(response.Diagnostics);

        Logger.LogInformation($"Request Charge ({queryDescription}): {response.RequestCharge}");
        if (isCrossPartition)
        {
            Logger.LogWarning($"Cross-partition query detected for {queryDescription}. This may impact performance.");
        }
        Logger.LogInformation($"Total request charge ({queryDescription}): {response.RequestCharge}");

        var resultCount = resultCountSelector != null
            ? resultCountSelector(response.Resource)
            : (response.Resource == null ? 0 : 1);

        OnQueryDiagnostics(new CosmosQueryDiagnostics
        {
            EventKind = CosmosQueryEventKind.QueryTotal,
            Timestamp = DateTimeOffset.UtcNow,
            RepositoryName = GetType().Name,
            QueryDescription = queryDescription,
            QueryText = queryText,
            Parameters = parameters,
            PartitionKey = partitionKey,
            RequestCharge = response.RequestCharge,
            Duration = stopwatch.Elapsed,
            ResultCount = resultCount,
            IsCrossPartition = isCrossPartition,
        });

        return response.Resource;
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
        var container = await GetContainerAsync();

        try
        {

#pragma warning disable CS0618 // Intentional cross-partition query at the base repository level
            var queryContext = await GetQueryContextAsync();
#pragma warning restore CS0618

            var query = queryContext.Queryable.Where(x => x.Id == id && x.EntityType == EntityType);

            var result = await GetResultsAsync(query, nameof(GetByIdAsync), queryContext.PartitionKey);

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
            catch (CosmosException ex) when (
                ex.StatusCode == System.Net.HttpStatusCode.Conflict ||
                (ex.StatusCode == System.Net.HttpStatusCode.InternalServerError &&
                 (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
                  ex.Message.Contains("E23505", StringComparison.OrdinalIgnoreCase))))
            {
                // Another thread/process already created the container.
                // The vnext-preview emulator returns 500 with PostgresError E23505
                // (unique constraint violation) instead of the expected 409 Conflict.
                Logger.LogInformation($"Container '{_Options.ContainerName}' was created by another process.");

                return _Database.GetContainer(_Options.ContainerName);
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

            try
            {
                var response = await _Client.CreateDatabaseAsync(_Options.DatabaseName, throughput: _Options.DatabaseThroughput);

                Logger.LogInformation($"Database '{_Options.DatabaseName}' created.");

                return response.Database;
            }
            catch (CosmosException ex) when (
                ex.StatusCode == System.Net.HttpStatusCode.Conflict ||
                (ex.StatusCode == System.Net.HttpStatusCode.InternalServerError &&
                 (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
                  ex.Message.Contains("E23505", StringComparison.OrdinalIgnoreCase))))
            {
                // Another thread/process already created the database.
                // The vnext-preview emulator returns 500 with PostgresError E23505
                // (unique constraint violation) instead of the expected 409 Conflict.
                Logger.LogInformation($"Database '{_Options.DatabaseName}' was created by another process.");

                return _Client.GetDatabase(_Options.DatabaseName);
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
        var container = await GetContainerAsync();

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

            var stopwatch = Stopwatch.StartNew();
            response = await container.UpsertItemAsync(
                saveThis, partitionKey, requestOptions);
            stopwatch.Stop();

            if (response == null)
            {
                throw new CosmosDbException($"Save operation for item with id {saveThis.Id} returned null response");
            }
            else
            {
                if (response.Resource != null)
                {
                    // update the etag and timestamp values from the response
                    saveThis.Etag = response.Resource.Etag;
                    saveThis.Timestamp = response.Resource.Timestamp;
                    saveThis.TimestampUnixStyle = response.Resource.TimestampUnixStyle;
                }

                LogPointOperationDiagnostics(nameof(SaveAsync), response.RequestCharge, response.Diagnostics, stopwatch.Elapsed);

                if (response.StatusCode is
                    System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.Created)
                {
                    return saveThis;
                }
                else
                {
                    throw new CosmosDbException($"Response status code was {response.StatusCode}");
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
            Logger.LogError($"Error saving {saveThis.EntityType} item {saveThis.Id} to container {_Options.ContainerName} in database {_Options.DatabaseName}.  {ex}");

            throw;
        }
    }

    /// <summary>
    /// Batch size for saving items to the Cosmos DB container.
    /// This is used to limit the number of items saved in a single batch.
    /// Default is 50 items per batch.
    /// </summary>
    protected virtual int BatchSize
    {
        get;
        set;
    } = 50;

    /// <summary>
    /// Save a list of items to the Cosmos DB container. This method will perform an insert if the item does not exist, otherwise it will perform an update.
    /// Items are saved in batches of 50 by default.
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
            var batches = BatchUtility.GetBatches(items, BatchSize);

            var batchCount = batches.Count;

            var currentBatch = 0;

            foreach (var batch in batches)
            {
                currentBatch++;
                using var response =
                    await SaveBatchAsync(batchCount, currentBatch, batch);
            }

            return;
        }
    }

    protected virtual async Task<TransactionalBatchResponse> SaveBatchAsync(int batchCount, int currentBatch, T[] batch)
    {
        var partitionKey = GetPartitionKey(batch.First());

        var container = await GetContainerAsync();

        var cosmosBatch = container.CreateTransactionalBatch(partitionKey);

        foreach (var item in batch)
        {
            cosmosBatch.UpsertItem(item);
        }

        await BeforeSaveBatch(cosmosBatch, batch, currentBatch, batchCount);
        var response = await cosmosBatch.ExecuteAsync();

        if (response.IsSuccessStatusCode)
        {
            for (int i = 0; i < batch.Length; i++)
            {
                // Get the individual response for each item in the batch
                var itemResponse = response.GetOperationResultAtIndex<T>(i);

                if (itemResponse != null && itemResponse.Resource != null)
                {
                    // Update the original item with the new ETag and timestamp values
                    batch[i].Etag = itemResponse.Resource.Etag;
                    batch[i].TimestampUnixStyle = itemResponse.Resource.TimestampUnixStyle;

                    Logger.LogInformation($"Updated item {batch[i].Id} with ETag: {itemResponse.Resource.Etag}");
                }
            }
        }

        await AfterSaveBatch(response, batch, currentBatch, batchCount);

        if (!response.IsSuccessStatusCode)
        {
            var responseAsJson = JsonSerializer.Serialize(response);

            throw new CosmosDbBatchOperationException(currentBatch, batchCount, batch.Length, $"Failed to save items. Response: {responseAsJson}");
        }

        return response;
    }

    protected virtual Task AfterSaveBatch(TransactionalBatchResponse response, T[] batch, int currentBatch, int batchCount)
    {
        return Task.CompletedTask;
    }

    protected virtual Task BeforeSaveBatch(TransactionalBatch cosmosBatch, T[] batch, int currentBatch, int batchCount)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Get the partition key for an item.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    protected virtual PartitionKey GetPartitionKey(T item)
    {
        return GetPartitionKey(item.TenantId, item.EntityType);
    }

    /// <summary>
    /// Get the partition key for an item.
    /// </summary>
    /// <param name="tenantId">Top-level partition key value (tenant id)</param>
    /// <param name="entityType">Second-level partition key value (entity type)</param>
    /// <returns></returns>

    protected virtual PartitionKey GetPartitionKey(
        string tenantId, string entityType)
    {
        var builder = new PartitionKeyBuilder();

        _ = builder.Add(tenantId);

        if (_Options.UseHierarchicalPartitionKey == true)
        {
            _ = builder.Add(entityType);
        }

        return builder.Build();
    }

    /// <summary>
    /// Creates a query context for the repository with the specified partition key
    /// configuration. This is the starting point for all custom LINQ queries built
    /// off of this repository by child repository classes.
    /// </summary>
    /// <param name="tenantId">Value to use for the first-level partition key (tenant id).</param>
    /// <returns>A query context containing the LINQ queryable and its configured partition key.</returns>
    protected virtual async Task<QueryContext<T>> GetQueryContextAsync(
        string tenantId)
    {
        return await GetQueryContextAsync(tenantId, EntityType);
    }

    /// <summary>
    /// Creates a query context for the repository with the specified partition key
    /// configuration. This is the starting point for all custom LINQ queries built
    /// off of this repository by child repository classes.
    /// </summary>
    /// <param name="tenantId">Value to use for the first-level partition key (tenant id).</param>
    /// <param name="entityType">Entity type value for the second-level partition key.</param>
    /// <returns>A query context containing the LINQ queryable and its configured partition key.</returns>
    protected virtual async Task<QueryContext<T>> GetQueryContextAsync(
        string tenantId, string entityType)
    {
        var builder = new PartitionKeyBuilder();

        builder.Add(tenantId);

        if (_Options.UseHierarchicalPartitionKey == true)
        {
            builder.Add(entityType);
        }
        var pk = builder.Build();

        var container = await GetContainerAsync();

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

        var info = new QueryContext<T>(pk, queryable);

        return info;
    }

    /// <summary>
    /// Creates a query context for the repository WITHOUT a partition key, resulting in a cross-partition query.
    /// Prefer the overload that accepts a tenantId for partition-scoped queries.
    /// </summary>
    /// <returns>A query context containing the LINQ queryable with an empty partition key.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the underlying LINQ queryable cannot be created.</exception>
    [Obsolete("This overload performs a cross-partition query without a partition key. Use GetQueryContextAsync(string tenantId) instead unless you explicitly need a cross-partition scan.")]
    protected virtual async Task<QueryContext<T>> GetQueryContextAsync()
    {
        var container = await GetContainerAsync();

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

        var info = new QueryContext<T>(new PartitionKey(), queryable);

        return info;
    }

    /// <summary>
    /// Gets a page of results with continuation support for efficient large result set retrieval.
    /// </summary>
    /// <param name="pageSize">Maximum number of items to return in this page</param>
    /// <param name="continuationToken">Continuation token from previous query (null for first page)</param>
    /// <returns>A page of results with continuation information</returns>
    [Obsolete("This overload performs a cross-partition query without a partition key. Use GetPagedAsync(string tenantId, ...) instead unless you explicitly need a cross-partition scan.")]
    public virtual async Task<PagedResults<T>> GetPagedAsync(int pageSize = 100, string? continuationToken = null)
    {
        if (pageSize <= 0)
        {
            throw new ArgumentException("Page size must be greater than zero.", nameof(pageSize));
        }

        var container = await GetContainerAsync();
        var queryContext = await GetQueryContextAsync();

        var query = queryContext.Queryable
            .Where(x => x.EntityType == EntityType)
            .Take(pageSize);

        var feedIterator = query.ToFeedIterator();

        if (!string.IsNullOrEmpty(continuationToken))
        {
            // Create a new query with the continuation token
            var queryRequestOptions = new QueryRequestOptions
            {
                MaxItemCount = pageSize,
                PartitionKey = queryContext.PartitionKey
            };

            var queryDefinition = new QueryDefinition(query.ToQueryDefinition().QueryText);
            feedIterator = container.GetItemQueryIterator<T>(
                queryDefinition,
                continuationToken,
                queryRequestOptions);
        }

        var items = new List<T>();
        var totalRequestCharge = 0.0;
        string? newContinuationToken = null;
        bool hasMoreResults = false;

        if (feedIterator.HasMoreResults)
        {
            var pageStopwatch = Stopwatch.StartNew();
            var response = await feedIterator.ReadNextAsync();
            pageStopwatch.Stop();

            items.AddRange(response);
            totalRequestCharge += response.RequestCharge;
            newContinuationToken = response.ContinuationToken;
            hasMoreResults = !string.IsNullOrEmpty(newContinuationToken);

            LogFeedResponseDiagnostics(
                nameof(GetPagedAsync),
                response.RequestCharge,
                response.Diagnostics,
                pageStopwatch.Elapsed,
                response.Count,
                queryText: null,
                parameters: null,
                partitionKey: queryContext.PartitionKey);
        }

        return new PagedResults<T>(items, newContinuationToken, hasMoreResults, totalRequestCharge);
    }

    /// <summary>
    /// Gets a page of results for a specific partition with continuation support.
    /// </summary>
    /// <param name="tenantId">Value to use for the first-level partition key (tenant id)</param>
    /// <param name="pageSize">Maximum number of items to return in this page</param>
    /// <param name="continuationToken">Continuation token from previous query (null for first page)</param>
    /// <returns>A page of results with continuation information</returns>
    protected virtual async Task<PagedResults<T>> GetPagedAsync(
        string tenantId,
        int pageSize = 100,
        string? continuationToken = null)
    {
        if (pageSize <= 0)
        {
            throw new ArgumentException("Page size must be greater than zero.", nameof(pageSize));
        }

        var container = await GetContainerAsync();
        var queryContext = await GetQueryContextAsync(tenantId);

        var query = queryContext.Queryable
            .Where(x => x.EntityType == EntityType)
            .Take(pageSize);

        var queryRequestOptions = new QueryRequestOptions
        {
            MaxItemCount = pageSize,
            PartitionKey = queryContext.PartitionKey
        };

        var queryDefinition = new QueryDefinition(query.ToQueryDefinition().QueryText);
        var feedIterator = container.GetItemQueryIterator<T>(
            queryDefinition,
            continuationToken,
            queryRequestOptions);

        var items = new List<T>();
        var totalRequestCharge = 0.0;
        string? newContinuationToken = null;
        bool hasMoreResults = false;

        if (feedIterator.HasMoreResults)
        {
            var pageStopwatch = Stopwatch.StartNew();
            var response = await feedIterator.ReadNextAsync();
            pageStopwatch.Stop();

            items.AddRange(response);
            totalRequestCharge += response.RequestCharge;
            newContinuationToken = response.ContinuationToken;
            hasMoreResults = !string.IsNullOrEmpty(newContinuationToken);

            LogFeedResponseDiagnostics(
                nameof(GetPagedAsync),
                response.RequestCharge,
                response.Diagnostics,
                pageStopwatch.Elapsed,
                response.Count,
                queryText: queryDefinition.QueryText,
                parameters: ExtractParameters(queryDefinition),
                partitionKey: queryContext.PartitionKey);
        }

        return new PagedResults<T>(items, newContinuationToken, hasMoreResults, totalRequestCharge);
    }
}
