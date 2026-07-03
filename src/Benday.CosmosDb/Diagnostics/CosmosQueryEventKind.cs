namespace Benday.CosmosDb.Diagnostics;

/// <summary>
/// Distinguishes the three kinds of events that <see cref="CosmosQueryDiagnostics"/>
/// can represent. Sinks use the kind to route, filter, and aggregate events.
/// </summary>
public enum CosmosQueryEventKind
{
    /// <summary>
    /// A single-document operation: save, delete, or point-read.
    /// </summary>
    PointOperation,

    /// <summary>
    /// One page of results from a multi-page feed iterator read.
    /// Emitted once per page for both LINQ queries and raw-SQL queries.
    /// </summary>
    FeedResponsePage,

    /// <summary>
    /// The aggregate total for a completed query. Emitted exactly once
    /// per top-level query execution, regardless of how many
    /// <see cref="FeedResponsePage"/> events preceded it. For scalar operations
    /// (<c>CountAsync</c>, <c>FirstOrDefaultAsync</c> via <c>ExecuteScalarAsync</c>)
    /// this is the only event emitted.
    /// </summary>
    QueryTotal
}
