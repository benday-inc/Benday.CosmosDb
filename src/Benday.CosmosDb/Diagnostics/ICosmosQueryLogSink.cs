namespace Benday.CosmosDb.Diagnostics;

/// <summary>
/// A sink for Cosmos query diagnostics. Implementations receive a
/// <see cref="CosmosQueryDiagnostics"/> event for every query execution
/// the library captures — point operations, feed response pages, and
/// query totals.
/// </summary>
/// <remarks>
/// <para>
/// Implementations are registered in the DI container (typically as
/// singletons) and injected into <c>CosmosRepository&lt;T&gt;</c> instances.
/// A default <see cref="NoOpCosmosQueryLogSink"/> is registered
/// automatically if no other sink is configured, so consumers only
/// register a sink when they want non-default behavior.
/// </para>
/// <para>
/// The <see cref="Record"/> method is synchronous and fire-and-forget
/// from the library's perspective. Sinks that need async I/O (file
/// writes, network calls) should buffer internally and flush from a
/// background worker — the library will not await the sink and will
/// not retry on failure.
/// </para>
/// <para>
/// Exceptions thrown from <see cref="Record"/> are caught by the
/// library, logged as warnings to <c>ILogger</c>, and suppressed. A
/// broken sink must not prevent a query from completing.
/// </para>
/// </remarks>
public interface ICosmosQueryLogSink
{
    /// <summary>
    /// Records a diagnostics event. Called synchronously from the
    /// library on the thread that executed the query.
    /// </summary>
    /// <param name="diagnostics">
    /// The diagnostics payload for this event. Never null.
    /// </param>
    void Record(CosmosQueryDiagnostics diagnostics);
}
