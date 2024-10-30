using Microsoft.Azure.Cosmos;

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
}
