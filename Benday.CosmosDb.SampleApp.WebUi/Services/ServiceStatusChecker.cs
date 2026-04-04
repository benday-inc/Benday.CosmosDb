using System.Net.Sockets;

namespace Benday.CosmosDb.SampleApp.WebUi.Services;

public class ServiceStatusChecker
{
    public bool HasPassed { get; private set; }

    public bool CosmosDbReachable { get; private set; }
    public string? CosmosDbError { get; private set; }

    public bool AzuriteReachable { get; private set; }
    public string? AzuriteError { get; private set; }

    public async Task CheckAsync()
    {
        CosmosDbReachable = await CheckEndpointAsync("localhost", 8081);
        if (!CosmosDbReachable)
        {
            CosmosDbError = "Cannot connect to Cosmos DB Emulator on localhost:8081";
        }

        AzuriteReachable = await CheckEndpointAsync("localhost", 10000);
        if (!AzuriteReachable)
        {
            AzuriteError = "Cannot connect to Azurite Blob Service on localhost:10000";
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
