using Benday.CosmosDb.DomainModels;
using Benday.CosmosDb.Utilities;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Benday.CosmosDb.Repositories;



public abstract class CosmosRepository<T> : IRepository<T> where T : class, ICosmosIdentity, new()
{
    private readonly CosmosClient _Client;
    private readonly bool _WithCreateStructures;
    private Database? _Database;
    private Container? _Container;
    private readonly string _DatabaseName;
    private readonly string _ContainerName;
    private readonly PartitionKey _PartitionKey;
    private readonly List<string> _PartitionKeyStrings = [];

    public virtual string DiscriminatorValue => typeof(T).Name;

    protected async Task<Container> GetContainer()
    {
        await Initialize();

        return _Container is null ? throw new InvalidOperationException($"Container instance is null.") : _Container;
    }

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

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        var container = await GetContainer();

        var query = $"SELECT * FROM c where c.{CosmosDbConstants.DiscriminatorPropertyName} = \"" + DiscriminatorValue + "\"";

        // Execute the query
        var resultSetIterator = container.GetItemQueryIterator<T>(query);

        var items = await GetResults(resultSetIterator, nameof(GetAllAsync));

        return items;
    }

    public async Task<IEnumerable<T>> GetAllTerribleQueryAsync(
        string nameSearchString)
    {
        var container = await GetContainer();

        // var query = $"SELECT * FROM c where c.Name LIKE '%st%'";
        // var query = $"SELECT * FROM c";

        var query = 
            "SELECT * FROM c WHERE CONTAINS(LOWER(c.Name), LOWER(@nameSearch))";

        var queryDefinition = new QueryDefinition(query)
            .WithParameter("@nameSearch", nameSearchString);

        // Execute the query
        var resultSetIterator = 
            container.GetItemQueryIterator<T>(
                queryDefinition);

        var items = await GetResults(resultSetIterator, nameof(GetAllTerribleQueryAsync));

        return items;
    }

    /// <summary>
    /// Get results from a query
    /// </summary>
    /// <param name="resultSetIterator"></param>
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

    protected virtual PartitionKey GetPartitionKey(T saveThis)
    {
        return GetPartitionKey(saveThis.PartitionKey, saveThis.DiscriminatorValue);
    }

    protected virtual PartitionKey GetPartitionKey(string partitionKey, string discriminatorValue)
    {
        var builder = new PartitionKeyBuilder();

        _ = builder.Add(partitionKey);
        _ = builder.Add(discriminatorValue);

        return builder.Build();
    }
}

