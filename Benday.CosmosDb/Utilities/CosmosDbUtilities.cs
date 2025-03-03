using Benday.CosmosDb.Repositories;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Benday.CosmosDb.Utilities;

/// <summary>
/// Utility class for working with CosmosDb.
/// </summary>
public static class CosmosDbUtilities
{
    public static PartitionKey GetPartitionKey(
        string keyString, 
        bool useHierarchicalPartitionKey)
    {
        var keys = keyString.Split(',');

        if (keys.Length == 0)
        {
            throw new InvalidOperationException("No keys found in partition key string.");
        }

        if (useHierarchicalPartitionKey == false)
        {
            return new PartitionKey(keys[0]);
        }
        else
        {
            var partionKeyBuilder = new PartitionKeyBuilder();

            foreach (var key in keys)
            {
                _ = partionKeyBuilder.Add(key);
            }
            var temp = partionKeyBuilder.Build();
            return temp;
        }
    }

    public static List<string> GetPartitionKeyStrings(string keyString)
    {
        var keys = keyString.Split(',').ToList();

        return keys;
    }
}
