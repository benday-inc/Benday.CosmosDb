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
            .WithDefaultValue(false)
            .AllowEmptyValue()
            .WithDescription("Use Azure Cosmos DB Emulator (localhost:8081)");

        args.AddString("endpoint")
            .AsNotRequired()
            .WithDefaultValue(string.Empty)
            .WithDescription("Cosmos DB endpoint URL");

        args.AddString("account-key")
            .AsNotRequired()
            .WithDefaultValue(string.Empty)
            .WithDescription("Cosmos DB account key");

        args.AddBoolean("use-managed-identity")
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDefaultValue(false)
            .WithDescription("Use DefaultAzureCredential for authentication");

        args.AddBoolean("gateway-mode")
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDefaultValue(false)
            .WithDescription("Use gateway connection mode");

        args.AddBoolean("validate-only")
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDefaultValue(false)
            .WithDescription("Validate source container exists, create destination if needed, then exit — no data migration");

        args.AddBoolean("dry-run")
            .AsNotRequired()
            .AllowEmptyValue()
            .WithDefaultValue(true)
            .WithDescription("Read and transform only — do not write to destination");

        args.AddInt32("batch-size")
            .AsNotRequired()
            .WithDescription("Documents per page (default: 100)")
            .WithDefaultValue(100);

        args.AddInt32("max-concurrency")
            .AsNotRequired()
            .WithDescription("Initial concurrent write operations (default: 50, adapts automatically)")
            .WithDefaultValue(50);

        args.AddInt32("max-concurrency-ceiling")
            .AsNotRequired()
            .WithDescription("Upper limit for adaptive concurrency (default: 200)")
            .WithDefaultValue(200);

        args.AddString("progress-dir")
            .AsNotRequired()
            .WithDefaultValue(string.Empty)
            .WithDescription("Directory for tracking progress (enables resume). Defaults to ./progress/{db}_{src}_to_{dest}");

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
        var useEmulator = Arguments.GetBooleanValue("use-emulator");

        var options = new MigrationOptions
        {
            DatabaseName = Arguments.GetStringValue("database"),
            SourceContainerName = Arguments.GetStringValue("source-container"),
            DestContainerName = Arguments.GetStringValue("dest-container"),
            UseEmulator = useEmulator,
            UseManagedIdentity = Arguments.GetBooleanValue("use-managed-identity"),
            UseGatewayMode = Arguments.GetBooleanValue("gateway-mode"),
            ValidateOnly = Arguments.GetBooleanValue("validate-only"),
            DryRun = Arguments.GetBooleanValue("dry-run"),
            BatchSize = Arguments.GetInt32Value("batch-size"),
            MaxConcurrency = Arguments.GetInt32Value("max-concurrency"),
            MaxConcurrencyCeiling = Arguments.GetInt32Value("max-concurrency-ceiling"),
            Endpoint = Arguments.GetStringValue("endpoint"),
            AccountKey = Arguments.GetStringValue("account-key"),
            ProgressDirectory = Arguments.GetStringValue("progress-dir"),
        };

        if (useEmulator)
        {
            options.Endpoint = CosmosClientFactory.EmulatorEndpoint;
            options.AccountKey = CosmosClientFactory.EmulatorAccountKey;
            options.UseGatewayMode = true;
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
        return CosmosClientFactory.Create(
            options.Endpoint,
            options.AccountKey,
            options.UseManagedIdentity,
            options.UseGatewayMode);
    }
}
