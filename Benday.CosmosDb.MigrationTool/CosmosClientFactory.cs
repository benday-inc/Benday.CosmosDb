using System.Text.Json;
using Azure.Identity;
using Microsoft.Azure.Cosmos;

namespace Benday.CosmosDb.MigrationTool;

/// <summary>
/// Shared factory for creating CosmosClient instances.
/// </summary>
public static class CosmosClientFactory
{
    public const string EmulatorEndpoint = "https://localhost:8081/";
    public const string EmulatorAccountKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    public static CosmosClient Create(
        string endpoint,
        string accountKey,
        bool useManagedIdentity = false,
        bool useGatewayMode = false,
        bool allowBulkExecution = true,
        bool isEmulator = false)
    {
        var connectionMode = useGatewayMode
            ? ConnectionMode.Gateway
            : ConnectionMode.Direct;

        var options = new CosmosClientOptions
        {
            AllowBulkExecution = allowBulkExecution,
            ConnectionMode = connectionMode,
            Serializer = new SystemTextJsonCosmosSerializer(new JsonSerializerOptions()),
        };

        if (isEmulator)
        {
            options.HttpClientFactory = () =>
            {
                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback =
                    (message, cert, chain, errors) => true;
                return new HttpClient(handler);
            };
        }

        if (useManagedIdentity)
        {
            return new CosmosClient(endpoint, new DefaultAzureCredential(), options);
        }
        else
        {
            var connectionString = $"AccountEndpoint={endpoint};AccountKey={accountKey};";
            return new CosmosClient(connectionString, options);
        }
    }

    public static CosmosClient CreateForEmulator()
    {
        return Create(EmulatorEndpoint, EmulatorAccountKey, useGatewayMode: true, isEmulator: true);
    }
}
