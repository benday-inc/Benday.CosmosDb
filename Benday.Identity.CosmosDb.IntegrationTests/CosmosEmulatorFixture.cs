using Microsoft.Azure.Cosmos;
using Testcontainers.CosmosDb;
using Xunit;

namespace Benday.Identity.CosmosDb.IntegrationTests;

public class CosmosEmulatorFixture : IAsyncLifetime
{
    private CosmosDbContainer? _container;
    public string ConnectionString { get; private set; } = string.Empty;
    public CosmosClient? Client { get; private set; }

    public const string DatabaseName = "IdentityTestDb";
    public const string UsersContainerName = "Users";
    public const string RolesContainerName = "Roles";

    public async Task InitializeAsync()
    {
        _container = new CosmosDbBuilder(
                "mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview")
            .Build();

        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        var clientOptions = new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            HttpClientFactory = () =>
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
                return new HttpClient(handler);
            }
        };
        Client = new CosmosClient(ConnectionString, clientOptions);

        // Create database and containers with hierarchical partition key
        var db = await Client.CreateDatabaseIfNotExistsAsync(DatabaseName);

        await db.Database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(UsersContainerName,
                new List<string> { "/pk", "/discriminator" }));

        await db.Database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(RolesContainerName,
                new List<string> { "/pk", "/discriminator" }));
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (_container != null)
            await _container.DisposeAsync();
    }
}

[CollectionDefinition("CosmosEmulator")]
public class CosmosEmulatorCollection : ICollectionFixture<CosmosEmulatorFixture>
{
}
