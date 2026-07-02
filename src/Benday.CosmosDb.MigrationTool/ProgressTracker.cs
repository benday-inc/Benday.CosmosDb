namespace Benday.CosmosDb.MigrationTool;

/// <summary>
/// Tracks migration progress on the local filesystem so that a resumed run
/// can skip documents that were already written successfully.
///
/// Layout:
///   {progressDir}/success/batch_000001.txt   (one doc ID per line)
///   {progressDir}/failed/batch_000001.txt    (one doc ID per line)
/// </summary>
public class ProgressTracker
{
    private readonly string _successDir;
    private readonly string _failedDir;
    private readonly HashSet<string> _completedIds;
    private int _batchCounter;
    private readonly object _lock = new();

    public int SkippedCount { get; private set; }

    public ProgressTracker(string progressDir)
    {
        _successDir = Path.Combine(progressDir, "success");
        _failedDir = Path.Combine(progressDir, "failed");

        Directory.CreateDirectory(_successDir);
        Directory.CreateDirectory(_failedDir);

        _completedIds = LoadCompletedIds();
        _batchCounter = CountExistingBatchFiles();
    }

    public bool IsAlreadyCompleted(string documentId)
    {
        if (_completedIds.Contains(documentId))
        {
            SkippedCount++;
            return true;
        }
        return false;
    }

    public int PreviouslyCompletedCount => _completedIds.Count;

    public void RecordSuccess(IReadOnlyList<string> documentIds)
    {
        WriteBatchFile(_successDir, documentIds);
    }

    public void RecordFailure(IReadOnlyList<string> documentIds)
    {
        WriteBatchFile(_failedDir, documentIds);
    }

    /// <summary>
    /// Clears previously recorded failures so they can be retried on the next run.
    /// Call this at the start of a resumed migration if you want to re-attempt failed docs.
    /// </summary>
    public void ClearFailures()
    {
        if (Directory.Exists(_failedDir))
        {
            foreach (var file in Directory.GetFiles(_failedDir, "*.txt"))
            {
                File.Delete(file);
            }
        }
    }

    private void WriteBatchFile(string directory, IReadOnlyList<string> documentIds)
    {
        if (documentIds.Count == 0) return;

        int batchNumber;
        lock (_lock)
        {
            batchNumber = ++_batchCounter;
        }

        var fileName = $"batch_{batchNumber:D6}.txt";
        var filePath = Path.Combine(directory, fileName);
        File.WriteAllLines(filePath, documentIds);
    }

    private HashSet<string> LoadCompletedIds()
    {
        var ids = new HashSet<string>();

        if (!Directory.Exists(_successDir)) return ids;

        foreach (var file in Directory.GetFiles(_successDir, "*.txt"))
        {
            foreach (var line in File.ReadLines(file))
            {
                var trimmed = line.Trim();
                if (trimmed.Length > 0)
                {
                    ids.Add(trimmed);
                }
            }
        }

        return ids;
    }

    private int CountExistingBatchFiles()
    {
        var count = 0;

        if (Directory.Exists(_successDir))
            count += Directory.GetFiles(_successDir, "*.txt").Length;

        if (Directory.Exists(_failedDir))
            count += Directory.GetFiles(_failedDir, "*.txt").Length;

        return count;
    }
}
