using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Benday.CosmosDb.Diagnostics;

/// <summary>
/// An <see cref="ICosmosQueryLogSink"/> that appends each diagnostics
/// event as a single line of JSON (NDJSON / JSON Lines) to a file.
/// </summary>
/// <remarks>
/// <para>
/// Events are handed to an in-memory queue from <see cref="Record"/> and
/// written to disk by a single background thread, so the library's
/// query-execution path is never blocked on file I/O. Dispose the sink
/// (or let the host shut it down) to flush any queued events.
/// </para>
/// <para>
/// If the queue fills past <see cref="CosmosFileLogSinkOptions.QueueCapacity"/>
/// (default 10,000), new events are dropped. This keeps a stuck disk
/// from growing memory without bound.
/// </para>
/// </remarks>
public sealed class FileCosmosQueryLogSink : ICosmosQueryLogSink, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    private readonly string _filePath;
    private readonly int _queueCapacity;
    private readonly BlockingCollection<CosmosQueryDiagnostics> _queue;
    private readonly Thread _worker;
    private readonly CancellationTokenSource _shutdownCts = new();
    private int _droppedCount;
    private bool _disposed;

    public FileCosmosQueryLogSink(string filePath)
        : this(new CosmosFileLogSinkOptions { FilePath = filePath })
    {
    }

    public FileCosmosQueryLogSink()
        : this(new CosmosFileLogSinkOptions())
    {
    }

    /// <summary>
    /// DI-friendly constructor. Resolves options via the
    /// <see cref="IOptions{TOptions}"/> pattern so consumers can register
    /// <see cref="CosmosFileLogSinkOptions"/> with
    /// <c>services.Configure&lt;CosmosFileLogSinkOptions&gt;(...)</c> or bind
    /// from <c>IConfiguration</c>.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public FileCosmosQueryLogSink(IOptions<CosmosFileLogSinkOptions> options)
        : this(options?.Value ?? throw new ArgumentNullException(nameof(options)))
    {
    }

    public FileCosmosQueryLogSink(CosmosFileLogSinkOptions options)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));

        _filePath = string.IsNullOrWhiteSpace(options.FilePath)
            ? CosmosFileLogSinkOptions.GetDefaultFilePath()
            : options.FilePath;
        _queueCapacity = options.QueueCapacity;
        _queue = new BlockingCollection<CosmosQueryDiagnostics>(_queueCapacity);

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory) == false)
        {
            Directory.CreateDirectory(directory);
        }

        _worker = new Thread(ProcessQueue)
        {
            IsBackground = true,
            Name = nameof(FileCosmosQueryLogSink)
        };
        _worker.Start();
    }

    /// <summary>
    /// Number of events dropped because the queue was full. Useful for
    /// surfacing back-pressure in tests or health checks.
    /// </summary>
    public int DroppedCount => Volatile.Read(ref _droppedCount);

    /// <inheritdoc />
    public void Record(CosmosQueryDiagnostics diagnostics)
    {
        if (diagnostics is null) return;
        if (_queue.IsAddingCompleted) return;

        if (_queue.TryAdd(diagnostics) == false)
        {
            Interlocked.Increment(ref _droppedCount);
        }
    }

    private void ProcessQueue()
    {
        try
        {
            foreach (var diagnostics in _queue.GetConsumingEnumerable(_shutdownCts.Token))
            {
                try
                {
                    var line = Serialize(diagnostics);
                    File.AppendAllText(_filePath, line + Environment.NewLine, Encoding.UTF8);
                }
                catch
                {
                    // Per ICosmosQueryLogSink contract: a broken sink must
                    // not prevent queries from completing. The library
                    // catches Record() exceptions, but since writes happen
                    // off-thread here we swallow them ourselves.
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static string Serialize(CosmosQueryDiagnostics d)
    {
        var payload = new Dictionary<string, object?>
        {
            ["eventKind"] = d.EventKind.ToString(),
            ["timestamp"] = d.Timestamp.ToUniversalTime().ToString("o"),
            ["repositoryName"] = d.RepositoryName,
            ["queryDescription"] = d.QueryDescription,
            ["queryText"] = d.QueryText,
            ["parameters"] = d.Parameters,
            ["partitionKey"] = d.PartitionKey.ToString(),
            ["requestCharge"] = d.RequestCharge,
            ["durationMs"] = d.Duration.TotalMilliseconds,
            ["resultCount"] = d.ResultCount,
            ["isCrossPartition"] = d.IsCrossPartition
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    /// <summary>
    /// Stops accepting new events and blocks until the background writer
    /// has flushed everything currently queued.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _queue.CompleteAdding();
        _worker.Join(TimeSpan.FromSeconds(5));
        _shutdownCts.Cancel();
        _shutdownCts.Dispose();
        _queue.Dispose();
    }
}
