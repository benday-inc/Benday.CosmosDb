using Benday.CosmosDb.Utilities;
using Microsoft.Azure.Cosmos;
using Xunit;

namespace Benday.Identity.CosmosDb.IntegrationTests;

/// <summary>
/// Shared fixture that connects to an already-running Cosmos DB emulator
/// on localhost:8081 using the library's built-in ForEmulator() configuration.
/// Start the emulator before running tests:
///   docker run -d -p 8081:8081 -p 1234:1234 mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview
/// </summary>
public class CosmosEmulatorFixture : IAsyncLifetime
{
    public CosmosClient Client { get; private set; } = null!;
    public CosmosConfig Config { get; private set; } = null!;

    public const string DatabaseName = "IdentityTestDb";
    public const string UsersContainerName = "Users";
    public const string RolesContainerName = "Roles";

    public async Task InitializeAsync()
    {
        Config = new CosmosConfigBuilder()
            .ForEmulator()
            .WithDatabase(DatabaseName)
            .WithContainer(UsersContainerName)
            .WithPartitionKey("/pk,/discriminator", useHierarchical: true)            
            .Build();

        // Use the library's built-in client options which configures
        // SystemTextJsonCosmosSerializer (required for [JsonPropertyName] attributes)
        var clientOptions = CosmosClientOptionsUtilities.GetCosmosDbClientOptions(
            jsonNamingPolicy: null,
            connectionMode: ConnectionMode.Gateway,
            allowBulkExecution: false);

        Client = new CosmosClient(Config.ConnectionString, clientOptions);

        // Create database and containers
        var db = await Client.CreateDatabaseIfNotExistsAsync(DatabaseName);

        await db.Database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(UsersContainerName,
                new List<string> { "/pk", "/discriminator" }));

        await db.Database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(RolesContainerName,
                new List<string> { "/pk", "/discriminator" }));
    }

    public Task DisposeAsync()
    {
        Client?.Dispose();
        return Task.CompletedTask;
    }
}

[CollectionDefinition("CosmosEmulator")]
public class CosmosEmulatorCollection : ICollectionFixture<CosmosEmulatorFixture>
{
}
