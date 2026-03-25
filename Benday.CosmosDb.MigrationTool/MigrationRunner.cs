using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Cosmos;

namespace Benday.CosmosDb.MigrationTool;

public class MigrationRunner
{
    private readonly CosmosClient _client;
    private readonly MigrationOptions _options;
    private readonly Action<string> _writeLine;
    private readonly DocumentTransformer _transformer = new();

    public MigrationRunner(CosmosClient client, MigrationOptions options, Action<string> writeLine)
    {
        _client = client;
        _options = options;
        _writeLine = writeLine;
    }

    public async Task RunAsync()
    {
        var database = _client.GetDatabase(_options.DatabaseName);

        // Verify source container exists
        var sourceContainer = database.GetContainer(_options.SourceContainerName);
        try
        {
            await sourceContainer.ReadContainerAsync();
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _writeLine($"ERROR: Source container '{_options.SourceContainerName}' not found in database '{_options.DatabaseName}'.");
            return;
        }

        // Count documents
        _writeLine("Counting documents in source container...");
        var totalDocs = await CountDocumentsAsync(sourceContainer);
        _writeLine($"Found {totalDocs} documents.");

        // Create destination container (unless dry-run)
        Container? destContainer = null;
        if (!_options.DryRun)
        {
            destContainer = await CreateDestinationContainerAsync(database);
            if (destContainer == null) return;
        }
        else
        {
            _writeLine("[DRY RUN] Skipping destination container creation.");
        }

        // Read, transform, and write documents
        var processedCount = 0;
        var transformedCount = 0;
        var errorCount = 0;
        var totalRUs = 0.0;
        var sampleCount = 0;

        var query = new QueryDefinition("SELECT * FROM c");
        var requestOptions = new QueryRequestOptions { MaxItemCount = _options.BatchSize };

        using var feedIterator = sourceContainer.GetItemQueryIterator<JsonObject>(query, requestOptions: requestOptions);

        while (feedIterator.HasMoreResults)
        {
            var response = await feedIterator.ReadNextAsync();
            totalRUs += response.RequestCharge;

            var batch = new List<(JsonObject doc, string tenantId, string entityType)>();

            foreach (var doc in response)
            {
                processedCount++;

                var result = _transformer.Transform(doc);

                if (result.IsError)
                {
                    errorCount++;
                    var docId = doc["id"]?.GetValue<string>() ?? "unknown";
                    _writeLine($"  ERROR [{docId}]: {string.Join("; ", result.Errors)}");
                    continue;
                }

                foreach (var warning in result.Warnings)
                {
                    _writeLine($"  WARNING: {warning}");
                }

                var transformed = result.TransformedDocument!;
                var tenantId = transformed["tenantId"]!.GetValue<string>();
                var entityType = transformed["entityType"]!.GetValue<string>();

                // Show sample in dry-run mode
                if (_options.DryRun && sampleCount < 3)
                {
                    sampleCount++;
                    _writeLine($"\n--- Sample document {sampleCount} ---");
                    _writeLine(transformed.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                }

                batch.Add((transformed, tenantId, entityType));
                transformedCount++;
            }

            // Write batch to destination
            if (!_options.DryRun && destContainer != null && batch.Count > 0)
            {
                var (writeRUs, writeErrors) = await WriteBatchAsync(destContainer, batch);
                totalRUs += writeRUs;
                errorCount += writeErrors;
            }

            _writeLine($"Progress: {processedCount}/{totalDocs} documents processed ({transformedCount} transformed, {errorCount} errors). RUs: {totalRUs:N1}");
        }

        // Final summary
        _writeLine("\n=== Migration Summary ===");
        _writeLine($"Total documents read:    {processedCount}");
        _writeLine($"Documents transformed:   {transformedCount}");
        _writeLine($"Documents with errors:   {errorCount}");
        _writeLine($"Total request units:     {totalRUs:N1}");

        if (_options.DryRun)
        {
            _writeLine("\n[DRY RUN] No documents were written. Run without /dry-run to perform the migration.");
        }
        else
        {
            _writeLine($"\nDocuments written to '{_options.DestContainerName}' in database '{_options.DatabaseName}'.");
        }
    }

    private async Task<int> CountDocumentsAsync(Container container)
    {
        var countQuery = new QueryDefinition("SELECT VALUE COUNT(1) FROM c");
        using var iterator = container.GetItemQueryIterator<int>(countQuery);

        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            return response.FirstOrDefault();
        }

        return 0;
    }

    private async Task<Container?> CreateDestinationContainerAsync(Database database)
    {
        // Check if destination already exists
        var containers = database.GetContainerQueryIterator<ContainerProperties>();
        while (containers.HasMoreResults)
        {
            var response = await containers.ReadNextAsync();
            foreach (var containerProps in response)
            {
                if (containerProps.Id == _options.DestContainerName)
                {
                    _writeLine($"Destination container '{_options.DestContainerName}' already exists. Resuming migration.");
                    return database.GetContainer(_options.DestContainerName);
                }
            }
        }

        _writeLine($"Creating destination container '{_options.DestContainerName}' with partition key paths /tenantId,/entityType...");

        var properties = new ContainerProperties(
            id: _options.DestContainerName,
            partitionKeyPaths: new List<string> { "/tenantId", "/entityType" }
        );

        var containerResponse = await database.CreateContainerAsync(properties);
        _writeLine($"Container '{_options.DestContainerName}' created.");
        return containerResponse.Container;
    }

    private async Task<(double rUs, int errorCount)> WriteBatchAsync(
        Container destContainer,
        List<(JsonObject doc, string tenantId, string entityType)> batch)
    {
        var totalRUs = 0.0;
        var ruLock = new object();
        var failedItems = new List<(string id, Exception ex)>();

        using var semaphore = new SemaphoreSlim(_options.MaxConcurrency, _options.MaxConcurrency);

        var tasks = batch.Select(async item =>
        {
            await semaphore.WaitAsync();
            try
            {
                var pk = new PartitionKeyBuilder()
                    .Add(item.tenantId)
                    .Add(item.entityType)
                    .Build();

                var response = await destContainer.UpsertItemAsync(item.doc, pk);
                lock (ruLock)
                {
                    totalRUs += response.RequestCharge;
                }
            }
            catch (Exception ex)
            {
                var docId = item.doc["id"]?.GetValue<string>() ?? "unknown";
                lock (failedItems)
                {
                    failedItems.Add((docId, ex));
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        if (failedItems.Count > 0)
        {
            foreach (var (id, ex) in failedItems)
            {
                _writeLine($"  WRITE ERROR [{id}]: {ex.Message}");
            }
        }

        return (totalRUs, failedItems.Count);
    }
}

public class MigrationOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string AccountKey { get; set; } = string.Empty;
    public bool UseEmulator { get; set; }
    public bool UseManagedIdentity { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public string SourceContainerName { get; set; } = string.Empty;
    public string DestContainerName { get; set; } = string.Empty;
    public bool UseGatewayMode { get; set; }
    public bool DryRun { get; set; }
    public int BatchSize { get; set; } = 100;
    public int MaxConcurrency { get; set; } = 5;
}
