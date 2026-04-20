namespace Benday.CosmosDb.Diagnostics;

/// <summary>
/// Configuration options for <see cref="FileCosmosQueryLogSink"/>.
/// </summary>
public sealed class CosmosFileLogSinkOptions
{
    /// <summary>
    /// Default file name used when <see cref="FilePath"/> is not set.
    /// Resolved relative to <see cref="AppContext.BaseDirectory"/>.
    /// </summary>
    public const string DefaultFileName = "cosmos-queries.ndjson";

    /// <summary>
    /// Default subdirectory (under <see cref="AppContext.BaseDirectory"/>)
    /// used when <see cref="FilePath"/> is not set.
    /// </summary>
    public const string DefaultDirectoryName = "logs";

    /// <summary>
    /// Resolves the default output path:
    /// <c>{AppContext.BaseDirectory}/logs/cosmos-queries.ndjson</c>.
    /// The directory is created on first write if it does not exist.
    /// </summary>
    public static string GetDefaultFilePath() =>
        Path.Combine(AppContext.BaseDirectory, DefaultDirectoryName, DefaultFileName);

    /// <summary>
    /// Absolute or relative path to the NDJSON log file. The directory
    /// will be created if it does not already exist. When null or empty,
    /// falls back to <see cref="GetDefaultFilePath"/>.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Maximum number of diagnostics events that can be queued awaiting
    /// background write. When the queue is full, additional events are
    /// dropped and counted against <see cref="FileCosmosQueryLogSink.DroppedCount"/>.
    /// Defaults to 10,000.
    /// </summary>
    public int QueueCapacity { get; set; } = 10_000;
}
