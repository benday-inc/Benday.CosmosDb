using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Benday.CommandsFramework;
using Microsoft.Azure.Cosmos;

namespace Benday.CosmosDb.MigrationTool;

[Command(
    Name = "copycontainer",
    IsAsync = true,
    Description = "Copy all documents from a remote Cosmos DB container to the local emulator. " +
                  "Useful for setting up test data before running the migrate command.")]
public class CopyContainerCommand : AsynchronousCommand
{
    public CopyContainerCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider)
        : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString("endpoint")
            .AsRequired()
            .WithDescription("Source Cosmos DB endpoint URL");

        args.AddString("account-key")
            .AsNotRequired()
            .WithDescription("Source Cosmos DB account key");

        args.AddBoolean("use-managed-identity")
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription("Use DefaultAzureCredential for source authentication");

        args.AddString("database")
            .AsRequired()
            .WithDescription("Source database name");

        args.AddString("container")
            .AsRequired()
            .WithDescription("Source container name");

        args.AddString("dest-database")
            .AsNotRequired()
            .WithDescription("Destination database name on emulator (defaults to source database name)");

        args.AddString("dest-container")
            .AsNotRequired()
            .WithDescription("Destination container name on emulator (defaults to source container name)");

        args.AddInt32("batch-size")
            .AsNotRequired()
            .WithDescription("Documents per page (default: 100)")
            .WithDefaultValue(100);

        args.AddInt32("max-concurrency")
            .AsNotRequired()
            .WithDescription("Max concurrent write operations (default: 5)")
            .WithDefaultValue(5);

        return args;
    }

    protected override async Task OnExecute()
    {
        var sourceEndpoint = Arguments.GetStringValue("endpoint");
        var sourceAccountKey = Arguments.ContainsKey("account-key") && Arguments["account-key"].HasValue
            ? Arguments.GetStringValue("account-key")
            : string.Empty;
        var useManagedIdentity = Arguments.ContainsKey("use-managed-identity") && Arguments["use-managed-identity"].HasValue;
        var databaseName = Arguments.GetStringValue("database");
        var containerName = Arguments.GetStringValue("container");

        var destDatabaseName = Arguments.ContainsKey("dest-database") && Arguments["dest-database"].HasValue
            ? Arguments.GetStringValue("dest-database")
            : databaseName;
        var destContainerName = Arguments.ContainsKey("dest-container") && Arguments["dest-container"].HasValue
            ? Arguments.GetStringValue("dest-container")
            : containerName;

        var batchSize = Arguments.ContainsKey("batch-size") && Arguments["batch-size"].HasValue
            ? Arguments.GetInt32Value("batch-size")
            : 100;
        var maxConcurrency = Arguments.ContainsKey("max-concurrency") && Arguments["max-concurrency"].HasValue
            ? Arguments.GetInt32Value("max-concurrency")
            : 5;

        if (!useManagedIdentity && string.IsNullOrWhiteSpace(sourceAccountKey))
        {
            throw new KnownException("Source account key is required. Provide /account-key or /use-managed-identity.");
        }

        // Create source client (Azure)
        _OutputProvider.WriteLine($"Connecting to source: {sourceEndpoint}");
        using var sourceClient = CosmosClientFactory.Create(
            sourceEndpoint, sourceAccountKey, useManagedIdentity);

        // Create destination client (emulator)
        _OutputProvider.WriteLine($"Connecting to emulator: {CosmosClientFactory.EmulatorEndpoint}");
        using var destClient = CosmosClientFactory.CreateForEmulator();

        // Verify source
        var sourceDb = sourceClient.GetDatabase(databaseName);
        var sourceContainer = sourceDb.GetContainer(containerName);

        ContainerProperties sourceProps;
        try
        {
            var containerResponse = await sourceContainer.ReadContainerAsync();
            sourceProps = containerResponse.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new KnownException($"Source container '{containerName}' not found in database '{databaseName}'.");
        }

        // Read partition key paths from source container
        var partitionKeyPaths = sourceProps.PartitionKeyPaths?.ToList() ?? new List<string>();
        _OutputProvider.WriteLine($"Source partition key paths: {string.Join(", ", partitionKeyPaths)}");

        // Create destination database and container on emulator
        _OutputProvider.WriteLine($"Creating database '{destDatabaseName}' on emulator...");
        var destDb = (await destClient.CreateDatabaseIfNotExistsAsync(destDatabaseName)).Database;

        _OutputProvider.WriteLine($"Creating container '{destContainerName}' on emulator with same partition key paths...");
        var destContainerProps = partitionKeyPaths.Count <= 1
            ? new ContainerProperties(destContainerName, partitionKeyPaths.FirstOrDefault() ?? "/id")
            : new ContainerProperties(destContainerName, partitionKeyPaths);

        var destContainer = (await destDb.CreateContainerIfNotExistsAsync(destContainerProps)).Container;

        // Count source documents
        _OutputProvider.WriteLine("Counting documents...");
        var totalDocs = await CountDocumentsAsync(sourceContainer);
        _OutputProvider.WriteLine($"Found {totalDocs} documents to copy.");

        // Copy documents
        var processedCount = 0;
        var writtenCount = 0;
        var errorCount = 0;
        var totalReadRUs = 0.0;
        var totalWriteRUs = 0.0;

        var query = new QueryDefinition("SELECT * FROM c");
        var requestOptions = new QueryRequestOptions { MaxItemCount = batchSize };

        using var feedIterator = sourceContainer.GetItemQueryIterator<JsonObject>(query, requestOptions: requestOptions);

        while (feedIterator.HasMoreResults)
        {
            var response = await feedIterator.ReadNextAsync();
            totalReadRUs += response.RequestCharge;

            var batch = new List<JsonObject>();

            foreach (var doc in response)
            {
                processedCount++;

                // Remove _etag so upsert doesn't fail with precondition errors
                doc.Remove("_etag");

                batch.Add(doc);
            }

            // Write batch to emulator
            if (batch.Count > 0)
            {
                var writeRUs = await WriteBatchAsync(
                    destContainer, batch, partitionKeyPaths, maxConcurrency);
                totalWriteRUs += writeRUs;
                writtenCount += batch.Count;
            }

            _OutputProvider.WriteLine(
                $"Progress: {processedCount}/{totalDocs} read, {writtenCount} written, {errorCount} errors. " +
                $"Read RUs: {totalReadRUs:N1}, Write RUs: {totalWriteRUs:N1}");
        }

        _OutputProvider.WriteLine("\n=== Copy Summary ===");
        _OutputProvider.WriteLine($"Documents read:       {processedCount}");
        _OutputProvider.WriteLine($"Documents written:    {writtenCount}");
        _OutputProvider.WriteLine($"Errors:               {errorCount}");
        _OutputProvider.WriteLine($"Source read RUs:      {totalReadRUs:N1}");
        _OutputProvider.WriteLine($"Emulator write RUs:   {totalWriteRUs:N1}");
        _OutputProvider.WriteLine($"\nDocuments copied to emulator: {destDatabaseName}/{destContainerName}");
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

    private async Task<double> WriteBatchAsync(
        Container destContainer,
        List<JsonObject> batch,
        List<string> partitionKeyPaths,
        int maxConcurrency)
    {
        var totalRUs = 0.0;
        var ruLock = new object();

        using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

        var tasks = batch.Select(async doc =>
        {
            await semaphore.WaitAsync();
            try
            {
                var pk = BuildPartitionKey(doc, partitionKeyPaths);
                var response = await destContainer.UpsertItemAsync(doc, pk);
                lock (ruLock)
                {
                    totalRUs += response.RequestCharge;
                }
            }
            catch (Exception ex)
            {
                var docId = doc["id"]?.GetValue<string>() ?? "unknown";
                _OutputProvider.WriteLine($"  WRITE ERROR [{docId}]: {ex.Message}");
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return totalRUs;
    }

    private static PartitionKey BuildPartitionKey(JsonObject doc, List<string> partitionKeyPaths)
    {
        if (partitionKeyPaths.Count == 0)
        {
            return PartitionKey.None;
        }

        if (partitionKeyPaths.Count == 1)
        {
            var propName = partitionKeyPaths[0].TrimStart('/');
            var value = doc[propName]?.GetValue<string>() ?? string.Empty;
            return new PartitionKey(value);
        }

        var builder = new PartitionKeyBuilder();
        foreach (var path in partitionKeyPaths)
        {
            var propName = path.TrimStart('/');
            var value = doc[propName]?.GetValue<string>() ?? string.Empty;
            builder.Add(value);
        }
        return builder.Build();
    }
}
