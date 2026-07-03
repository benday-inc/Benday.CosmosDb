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
    /// <summary>
    /// Builds a <see cref="PartitionKey"/> from a comma-separated key string. When
    /// <paramref name="useHierarchicalPartitionKey"/> is false, only the first component is used;
    /// otherwise all components are combined into a hierarchical key.
    /// </summary>
    /// <param name="keyString">Comma-separated partition key values.</param>
    /// <param name="useHierarchicalPartitionKey">Whether to build a hierarchical (multi-level) key.</param>
    /// <returns>The constructed partition key.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="keyString"/> yields no components.</exception>
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

    /// <summary>Splits a comma-separated partition key string into its individual component values.</summary>
    /// <param name="keyString">Comma-separated partition key values.</param>
    /// <returns>The list of component values.</returns>
    public static List<string> GetPartitionKeyStrings(string keyString)
    {
        var keys = keyString.Split(',').ToList();

        return keys;
    }
}
