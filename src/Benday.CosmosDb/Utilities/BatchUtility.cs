namespace Benday.CosmosDb.Utilities;

/// <summary>
/// Utility class for working with batches of items.
/// </summary>
public static class BatchUtility
{
    /// <summary>
    /// Gets a list of arrays of items that are batched.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="values">Values to convert to batches</param>
    /// <param name="batchSize">Number of items per batch</param>
    /// <returns></returns>
    public static List<T[]> GetBatches<T>(IEnumerable<T> values, int batchSize)
    {
        var batchCount = GetBatchCount(values.Count(), batchSize);

        var batches = new List<T[]>();

        for (int i = 1; i <= batchCount; i++)
        {
            var batch = CreateArrayForBatch(values.ToList(), batchSize, i);

            batches.Add(batch);
        }

        return batches;
    }

    /// <summary>
    /// Creates an array of items for a specific batch number.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="results"></param>
    /// <param name="batchSize"></param>
    /// <param name="batchNumber"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static T[] CreateArrayForBatch<T>(
        List<T> results, int batchSize, int batchNumber)
    {
        if (batchNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(batchNumber), "Value cannot be less than 1.");
        }

        if (batchSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Value cannot be less than 1.");
        }

        var items = results.Skip((batchNumber - 1) * batchSize).Take(batchSize);

        var itemsToReturn = items.ToArray();

        return itemsToReturn;
    }

    /// <summary>
    /// Gets the number of batches that will be created.
    /// </summary>
    /// <param name="itemCount"></param>
    /// <param name="batchSize"></param>
    /// <returns></returns>
    public static int GetBatchCount(int itemCount, int batchSize)
    {
        int numberOfBatches = itemCount / batchSize;
        int leftovers = itemCount % batchSize;

        if (leftovers != 0)
        {
            numberOfBatches++;
        }

        return numberOfBatches;
    }
}