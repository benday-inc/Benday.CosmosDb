using Benday.AzureStorage.Configuration;
using Benday.CosmosDb.Utilities;
using System.Net.Sockets;

namespace Benday.CosmosDb.SampleApp.WebUi.Services;

public class ServiceStatusChecker
{
    private readonly CosmosConfig _CosmosConfig;
    private readonly AzureStorageConfig _AzureStorageConfig;

    public ServiceStatusChecker(
        IConfiguration configuration,
        AzureStorageConfig azureStorageConfig)
    {
        _CosmosConfig = configuration.GetCosmosConfig();
        _AzureStorageConfig = azureStorageConfig;
    }

    public bool HasPassed { get; private set; }

    public bool CosmosDbReachable { get; private set; }
    public string? CosmosDbError { get; private set; }

    public bool AzuriteReachable { get; private set; }
    public string? AzuriteError { get; private set; }

    public async Task CheckAsync()
    {
        var cosmosUri = new Uri(_CosmosConfig.Endpoint);

        CosmosDbReachable = await CheckEndpointAsync(cosmosUri.Host, cosmosUri.Port);
        if (!CosmosDbReachable)
        {
            CosmosDbError = $"Cannot connect to Cosmos DB Emulator on {cosmosUri.Host}:8081";
        }

        // this assumes that the cosmos container also hosts azurite
        AzuriteReachable = await CheckEndpointAsync(cosmosUri.Host, 10000);
        if (!AzuriteReachable)
        {
            AzuriteError = $"Cannot connect to Azurite Blob Service on {cosmosUri.Host}:10000";
        }

        HasPassed = CosmosDbReachable && AzuriteReachable;
    }

    private static async Task<bool> CheckEndpointAsync(string host, int port)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(host, port);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(2));

            var completed = await Task.WhenAny(connectTask, timeoutTask);
            return completed == connectTask && client.Connected;
        }
        catch
        {
            return false;
        }
    }
}
