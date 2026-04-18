namespace Benday.CosmosDb.Diagnostics;

/// <summary>
/// Default implementation of <see cref="ICosmosQueryLogSink"/> that
/// discards every event. Registered automatically by the library's DI
/// setup as the default sink when no other implementation is provided.
/// </summary>
public sealed class NoOpCosmosQueryLogSink : ICosmosQueryLogSink
{
    /// <summary>
    /// Singleton instance. Use this whenever a no-op sink is needed
    /// (e.g., in the <c>CosmosRepository&lt;T&gt;</c> constructor's
    /// null-coalescing fallback).
    /// </summary>
    public static readonly NoOpCosmosQueryLogSink Instance = new();

    private NoOpCosmosQueryLogSink() { }

    /// <inheritdoc />
    public void Record(CosmosQueryDiagnostics diagnostics)
    {
    }
}
