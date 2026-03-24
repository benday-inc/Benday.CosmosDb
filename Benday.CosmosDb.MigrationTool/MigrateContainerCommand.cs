using System.Text.Json;
using Azure.Identity;
using Benday.CommandsFramework;
using Microsoft.Azure.Cosmos;

namespace Benday.CosmosDb.MigrationTool;

[Command(
    Name = "migrate",
    IsAsync = true,
    Description = "Migrate a Cosmos DB container from v5 schema (pk/discriminator) to v6 schema (tenantId/entityType) with camelCase property names.")]
public class MigrateContainerCommand : AsynchronousCommand
{
    public MigrateContainerCommand(
        CommandExecutionInfo info, ITextOutputProvider outputProvider)
        : base(info, outputProvider)
    {
    }

    public override ArgumentCollection GetArguments()
    {
        var args = new ArgumentCollection();

        args.AddString("database")
            .AsRequired()
            .WithDescription("Cosmos DB database name");

        args.AddString("source-container")
            .AsRequired()
            .WithDescription("Source container name (old schema)");

        args.AddString("dest-container")
            .AsRequired()
            .WithDescription("Destination container name (new schema)");

        args.AddBoolean("use-emulator")
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription("Use Azure Cosmos DB Emulator (localhost:8081)");

        args.AddString("endpoint")
            .AsNotRequired()
            .WithDescription("Cosmos DB endpoint URL");

        args.AddString("account-key")
            .AsNotRequired()
            .WithDescription("Cosmos DB account key");

        args.AddBoolean("use-managed-identity")
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription("Use DefaultAzureCredential for authentication");

        args.AddBoolean("gateway-mode")
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription("Use gateway connection mode");

        args.AddBoolean("dry-run")
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDescription("Read and transform only — do not write to destination");

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
        var options = BuildMigrationOptions();

        ValidateOptions(options);

        var client = CreateCosmosClient(options);

        var runner = new MigrationRunner(client, options, _OutputProvider.WriteLine);

        await runner.RunAsync();
    }

    private MigrationOptions BuildMigrationOptions()
    {
        var options = new MigrationOptions
        {
            DatabaseName = Arguments.GetStringValue("database"),
            SourceContainerName = Arguments.GetStringValue("source-container"),
            DestContainerName = Arguments.GetStringValue("dest-container"),
        };

        if (Arguments.ContainsKey("use-emulator") && Arguments["use-emulator"].HasValue)
        {
            options.UseEmulator = true;
            options.Endpoint = "https://localhost:8081/";
            options.AccountKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
            options.UseGatewayMode = true;
        }

        if (Arguments.ContainsKey("endpoint") && Arguments["endpoint"].HasValue)
        {
            options.Endpoint = Arguments.GetStringValue("endpoint");
        }

        if (Arguments.ContainsKey("account-key") && Arguments["account-key"].HasValue)
        {
            options.AccountKey = Arguments.GetStringValue("account-key");
        }

        if (Arguments.ContainsKey("use-managed-identity") && Arguments["use-managed-identity"].HasValue)
        {
            options.UseManagedIdentity = true;
        }

        if (Arguments.ContainsKey("gateway-mode") && Arguments["gateway-mode"].HasValue)
        {
            options.UseGatewayMode = true;
        }

        if (Arguments.ContainsKey("dry-run") && Arguments["dry-run"].HasValue)
        {
            options.DryRun = true;
        }

        if (Arguments.ContainsKey("batch-size") && Arguments["batch-size"].HasValue)
        {
            options.BatchSize = Arguments.GetInt32Value("batch-size");
        }

        if (Arguments.ContainsKey("max-concurrency") && Arguments["max-concurrency"].HasValue)
        {
            options.MaxConcurrency = Arguments.GetInt32Value("max-concurrency");
        }

        return options;
    }

    private void ValidateOptions(MigrationOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Endpoint))
        {
            throw new KnownException("Endpoint is required. Use /use-emulator or provide /endpoint.");
        }

        if (!options.UseManagedIdentity && string.IsNullOrWhiteSpace(options.AccountKey))
        {
            throw new KnownException("Account key is required. Use /account-key, /use-emulator, or /use-managed-identity.");
        }

        if (options.SourceContainerName == options.DestContainerName)
        {
            throw new KnownException("Source and destination container names must be different.");
        }
    }

    private CosmosClient CreateCosmosClient(MigrationOptions options)
    {
        var connectionMode = options.UseGatewayMode
            ? ConnectionMode.Gateway
            : ConnectionMode.Direct;

        var cosmosOptions = new CosmosClientOptions
        {
            AllowBulkExecution = true,
            ConnectionMode = connectionMode,
            HttpClientFactory = () =>
            {
                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback =
                    (message, cert, chain, errors) => true;
                return new HttpClient(handler);
            }
        };

        if (options.UseManagedIdentity)
        {
            return new CosmosClient(options.Endpoint, new DefaultAzureCredential(), cosmosOptions);
        }
        else
        {
            var connectionString = $"AccountEndpoint={options.Endpoint};AccountKey={options.AccountKey};";
            return new CosmosClient(connectionString, cosmosOptions);
        }
    }
}
