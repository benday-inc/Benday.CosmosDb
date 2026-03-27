using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;
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

        _writeLine($"Source container '{_options.SourceContainerName}' verified.");

        // Count documents
        _writeLine("Counting documents in source container...");
        var totalDocs = await CountDocumentsAsync(sourceContainer);
        _writeLine($"Found {totalDocs} documents.");

        if (_options.ValidateOnly)
        {
            // Create destination container if it doesn't exist, then exit
            var destContainerValidate = await CreateDestinationContainerAsync(database);
            if (destContainerValidate == null)
            {
                _writeLine("ERROR: Failed to create or find destination container.");
                return;
            }
            _writeLine($"\n=== Validation Complete ===");
            _writeLine($"Source:      {_options.SourceContainerName} ({totalDocs} documents)");
            _writeLine($"Destination: {_options.DestContainerName} (ready)");
            _writeLine("No data was migrated. Remove /validate-only to run the migration.");
            return;
        }

        // Set up progress tracking
        var progressDir = _options.ProgressDirectory;
        if (string.IsNullOrWhiteSpace(progressDir))
        {
            progressDir = Path.Combine(
                Directory.GetCurrentDirectory(),
                "progress",
                $"{_options.DatabaseName}_{_options.SourceContainerName}_to_{_options.DestContainerName}");
        }

        var tracker = new ProgressTracker(progressDir);
        if (tracker.PreviouslyCompletedCount > 0)
        {
            _writeLine($"Resuming: {tracker.PreviouslyCompletedCount} documents already completed (from {progressDir})");
            tracker.ClearFailures(); // re-attempt previously failed docs
        }

        _writeLine($"Progress directory: {progressDir}");

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

        // Set up adaptive concurrency
        var concurrency = new AdaptiveConcurrencyController(
            _options.MaxConcurrency,
            _writeLine,
            floor: 5,
            ceiling: _options.MaxConcurrencyCeiling);

        _writeLine($"Starting with concurrency={concurrency.CurrentConcurrency}, batch-size={_options.BatchSize}");

        // Channel<T> creates an async pipeline between the reader (producer) and writer (consumer).
        // Think of it as a thread-safe async queue: the producer pushes transformed batches in,
        // the consumer pulls them out and writes to Cosmos. A capacity of 3 means up to 3 batches
        // can be buffered, so the next read starts while the current write is still in progress.
        var channel = Channel.CreateBounded<ReadBatch>(new BoundedChannelOptions(3)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true,
        });

        var stats = new MigrationStats();
        var startTime = DateTime.UtcNow;

        // Producer: reads from source, transforms, pushes into channel
        var producerTask = RunProducerAsync(sourceContainer, channel.Writer, tracker, stats, totalDocs, startTime);

        // Consumer: pulls from channel, writes to destination
        var consumerTask = RunConsumerAsync(
            channel.Reader, destContainer, tracker, concurrency, stats, totalDocs, startTime);

        await Task.WhenAll(producerTask, consumerTask);

        // Final summary
        var elapsed = DateTime.UtcNow - startTime;
        _writeLine("\n=== Migration Summary ===");
        _writeLine($"Total documents read:    {stats.ProcessedCount}");
        _writeLine($"Documents transformed:   {stats.TransformedCount}");
        _writeLine($"Documents written:       {stats.WrittenCount}");
        _writeLine($"Documents skipped:       {tracker.SkippedCount} (already completed)");
        _writeLine($"Transform errors:        {stats.TransformErrorCount}");
        _writeLine($"Write errors:            {stats.WriteErrorCount}");
        _writeLine($"429 throttles seen:      {stats.ThrottleCount}");
        _writeLine($"Total request units:     {stats.TotalRUs:N1}");
        _writeLine($"Final concurrency:       {concurrency.CurrentConcurrency}");
        _writeLine($"Elapsed time:            {FormatTimeSpan(elapsed)}");

        if (_options.DryRun)
        {
            _writeLine("\n[DRY RUN] No documents were written. Run without /dry-run to perform the migration.");
        }
        else
        {
            _writeLine($"\nDocuments written to '{_options.DestContainerName}' in database '{_options.DatabaseName}'.");
            _writeLine($"Progress saved to: {progressDir}");
        }
    }

    private async Task RunProducerAsync(
        Container sourceContainer,
        ChannelWriter<ReadBatch> writer,
        ProgressTracker tracker,
        MigrationStats stats,
        int totalDocs,
        DateTime startTime)
    {
        try
        {
            var query = new QueryDefinition("SELECT * FROM c");
            var requestOptions = new QueryRequestOptions { MaxItemCount = _options.BatchSize };
            var sampleCount = 0;

            using var feedIterator = sourceContainer.GetItemQueryIterator<JsonObject>(query, requestOptions: requestOptions);

            while (feedIterator.HasMoreResults)
            {
                var response = await feedIterator.ReadNextAsync();
                stats.AddReadRUs(response.RequestCharge);

                var items = new List<WriteItem>();

                foreach (var doc in response)
                {
                    Interlocked.Increment(ref stats.ProcessedCount);

                    var docId = doc["id"]?.GetValue<string>() ?? "unknown";

                    // Skip if already completed in a previous run
                    if (tracker.IsAlreadyCompleted(docId))
                    {
                        continue;
                    }

                    var result = _transformer.Transform(doc);

                    if (result.IsError)
                    {
                        Interlocked.Increment(ref stats.TransformErrorCount);
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

                    items.Add(new WriteItem(transformed, tenantId, entityType, docId));
                    Interlocked.Increment(ref stats.TransformedCount);
                }

                if (items.Count > 0)
                {
                    await writer.WriteAsync(new ReadBatch(items, response.RequestCharge));
                }

                // Log read-side progress
                var percentDone = totalDocs > 0 ? (double)stats.ProcessedCount / totalDocs * 100 : 0;
                var elapsed = DateTime.UtcNow - startTime;
                var etaString = EstimateTimeRemaining(stats.ProcessedCount, totalDocs, elapsed);
                _writeLine($"Read: {stats.ProcessedCount}/{totalDocs} ({percentDone:N1}%) | ETA: {etaString}");
            }
        }
        finally
        {
            writer.Complete();
        }
    }

    private async Task RunConsumerAsync(
        ChannelReader<ReadBatch> reader,
        Container? destContainer,
        ProgressTracker tracker,
        AdaptiveConcurrencyController concurrency,
        MigrationStats stats,
        int totalDocs,
        DateTime startTime)
    {
        await foreach (var batch in reader.ReadAllAsync())
        {
            if (_options.DryRun || destContainer == null)
            {
                continue;
            }

            var (writeRUs, writeErrors, throttleCount, successIds, failedIds) =
                await WriteBatchAsync(destContainer, batch.Items, concurrency);

            stats.AddWriteRUs(writeRUs);
            Interlocked.Add(ref stats.WriteErrorCount, writeErrors);
            Interlocked.Add(ref stats.WrittenCount, batch.Items.Count - writeErrors);
            Interlocked.Add(ref stats.ThrottleCount, throttleCount);

            // Persist progress to disk
            if (successIds.Count > 0)
                tracker.RecordSuccess(successIds);
            if (failedIds.Count > 0)
                tracker.RecordFailure(failedIds);

            // Adapt concurrency based on throttling
            concurrency.ReportBatchResult(throttleCount);

            // Log write-side progress
            var totalHandled = stats.WrittenCount + stats.WriteErrorCount + tracker.SkippedCount;
            var percentDone = totalDocs > 0 ? (double)totalHandled / totalDocs * 100 : 0;
            var elapsed = DateTime.UtcNow - startTime;
            var etaString = EstimateTimeRemaining(totalHandled, totalDocs, elapsed);
            _writeLine(
                $"Write: {stats.WrittenCount} ok, {stats.WriteErrorCount} err, {tracker.SkippedCount} skip " +
                $"({percentDone:N1}%) | Batch RUs: {writeRUs:N1} | Concurrency: {concurrency.CurrentConcurrency} | ETA: {etaString}");
        }
    }

    private async Task<(double rUs, int errorCount, int throttleCount, List<string> successIds, List<string> failedIds)>
        WriteBatchAsync(
            Container destContainer,
            List<WriteItem> batch,
            AdaptiveConcurrencyController concurrency)
    {
        var totalRUs = 0.0;
        var ruLock = new object();
        var throttleCount = 0;
        var successIds = new List<string>();
        var failedIds = new List<string>();
        var successLock = new object();
        var failedLock = new object();

        var tasks = batch.Select(async item =>
        {
            await concurrency.WaitAsync();
            try
            {
                var pk = new PartitionKeyBuilder()
                    .Add(item.TenantId)
                    .Add(item.EntityType)
                    .Build();

                var response = await destContainer.UpsertItemAsync(item.Document, pk);
                lock (ruLock)
                {
                    totalRUs += response.RequestCharge;
                }
                lock (successLock)
                {
                    successIds.Add(item.DocumentId);
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == (HttpStatusCode)429)
            {
                // The SDK already retried internally and exhausted its retry budget.
                Interlocked.Increment(ref throttleCount);
                _writeLine($"  THROTTLE [{item.DocumentId}]: 429 after SDK retries exhausted. SubStatus={ex.SubStatusCode}");
                lock (failedLock)
                {
                    failedIds.Add(item.DocumentId);
                }
            }
            catch (Exception ex)
            {
                _writeLine($"  WRITE ERROR [{item.DocumentId}]: {ex.Message}");
                lock (failedLock)
                {
                    failedIds.Add(item.DocumentId);
                }
            }
            finally
            {
                concurrency.Release();
            }
        });

        await Task.WhenAll(tasks);

        return (totalRUs, failedIds.Count, throttleCount, successIds, failedIds);
    }

    private static string EstimateTimeRemaining(int completed, int total, TimeSpan elapsed)
    {
        if (completed <= 0) return "calculating...";
        if (completed >= total) return "done";

        var estimatedTotal = TimeSpan.FromTicks((long)(elapsed.Ticks * ((double)total / completed)));
        var remaining = estimatedTotal - elapsed;
        return FormatTimeSpan(remaining);
    }

    private static string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
        {
            return $"{(int)ts.TotalHours}h {ts.Minutes:D2}m {ts.Seconds:D2}s";
        }
        if (ts.TotalMinutes >= 1)
        {
            return $"{(int)ts.TotalMinutes}m {ts.Seconds:D2}s";
        }
        return $"{ts.Seconds}s";
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
}

/// <summary>
/// A batch of transformed documents ready to be written.
/// </summary>
public record ReadBatch(List<WriteItem> Items, double ReadRUs);

/// <summary>
/// A single document ready for writing, with its partition key components and original ID.
/// </summary>
public record WriteItem(JsonObject Document, string TenantId, string EntityType, string DocumentId);

/// <summary>
/// Thread-safe migration statistics.
/// </summary>
public class MigrationStats
{
    public int ProcessedCount;
    public int TransformedCount;
    public int WrittenCount;
    public int TransformErrorCount;
    public int WriteErrorCount;
    public int ThrottleCount;

    private double _totalRUs;
    private readonly object _ruLock = new();

    public double TotalRUs
    {
        get { lock (_ruLock) return _totalRUs; }
    }

    public void AddReadRUs(double rus)
    {
        lock (_ruLock) _totalRUs += rus;
    }

    public void AddWriteRUs(double rus)
    {
        lock (_ruLock) _totalRUs += rus;
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
    public bool ValidateOnly { get; set; }
    public bool DryRun { get; set; }
    public int BatchSize { get; set; } = 100;
    public int MaxConcurrency { get; set; } = 50;
    public int MaxConcurrencyCeiling { get; set; } = 200;
    public string ProgressDirectory { get; set; } = string.Empty;
}
