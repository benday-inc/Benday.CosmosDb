namespace Benday.CosmosDb.Utilities;

public static class BatchUtility
{
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