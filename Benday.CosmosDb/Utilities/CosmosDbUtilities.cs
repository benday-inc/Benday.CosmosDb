using Microsoft.Azure.Cosmos;
using System.Text.Json;

namespace Benday.CosmosDb.Utilities;

/// <summary>
/// Utility class for working with CosmosDb.
/// </summary>
public static class CosmosDbUtilities
{
    public static PartitionKey GetPartitionKey(string keyString)
    {
        var keys = keyString.Split(',');

        var partionKeyBuilder = new PartitionKeyBuilder();

        foreach (var key in keys)
        {
            _ = partionKeyBuilder.Add(key);
        }
        var temp = partionKeyBuilder.Build();
        return temp;
    }

    public static List<string> GetPartitionKeyStrings(string keyString)
    {
        var keys = keyString.Split(',').ToList();

        return keys;
    }

    /// <summary>
    /// Gets the default CosmosClientOptions for a Cosmos DB client that uses System.Text.Json.
    /// This version of the method uses the default JsonNamingPolicy for the JsonSerializerOptions
    /// </summary>
    /// <returns>Client options object</returns>
    public static CosmosClientOptions GetCosmosDbClientOptions()
    {
        return GetCosmosDbClientOptions(null);
    }

    /// <summary>
    /// Gets the default CosmosClientOptions for a Cosmos DB client that uses System.Text.Json
    /// and provides an option to customize the JsonNamingPolicy for the JsonSerializerOptions.
    /// </summary>
    /// <param name="jsonNamingPolicy">Naming policy or null to not use a policy</param>
    /// <returns></returns>
    public static CosmosClientOptions GetCosmosDbClientOptions(JsonNamingPolicy? jsonNamingPolicy)
    {
        var options = new CosmosClientOptions
        {
            Serializer = new SystemTextJsonCosmosSerializer(new JsonSerializerOptions
            {
                PropertyNamingPolicy = jsonNamingPolicy,
                WriteIndented = true
                // Add additional JsonSerializerOptions settings as needed
            })
        };

        return options;
    }
}
