using System;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos;

namespace Benday.CosmosDb.Diagnostics;

/// <summary>
/// Structured payload describing a single Cosmos DB query execution event.
/// Delivered to <c>CosmosRepository&lt;T&gt;.OnQueryDiagnostics</c> for point
/// operations, per-page feed responses, and query totals.
/// </summary>
public sealed class CosmosQueryDiagnostics
{
    /// <summary>
    /// Which kind of event this is. Drives how sinks route and aggregate.
    /// </summary>
    public CosmosQueryEventKind EventKind { get; init; }

    /// <summary>
    /// When the event was captured, in UTC.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Name of the repository type (e.g. "CocktailRecipeRepository").
    /// </summary>
    public string RepositoryName { get; init; } = string.Empty;

    /// <summary>
    /// Logging description for the operation, typically the repository
    /// name plus the caller method name, e.g.
    /// "CocktailRecipeRepository - SearchByTitleAsync".
    /// </summary>
    public string QueryDescription { get; init; } = string.Empty;

    /// <summary>
    /// The generated SQL text. Null for <see cref="CosmosQueryEventKind.PointOperation"/>
    /// events (point operations don't have a query text — just an id and a
    /// partition key). Populated for <see cref="CosmosQueryEventKind.FeedResponsePage"/>
    /// and <see cref="CosmosQueryEventKind.QueryTotal"/> events.
    /// </summary>
    public string? QueryText { get; init; }

    /// <summary>
    /// Query parameters. Null for <see cref="CosmosQueryEventKind.PointOperation"/>.
    /// Populated for LINQ and raw-SQL queries from the underlying
    /// <see cref="QueryDefinition"/>.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Parameters { get; init; }

    /// <summary>
    /// Partition key scope for the operation. Uses a default
    /// <see cref="PartitionKey"/> value (no partition) for cross-partition queries.
    /// </summary>
    public PartitionKey PartitionKey { get; init; }

    /// <summary>
    /// RU consumed. For <see cref="CosmosQueryEventKind.QueryTotal"/>, this is the
    /// accumulated charge across all pages. For <see cref="CosmosQueryEventKind.FeedResponsePage"/>,
    /// just that page. For <see cref="CosmosQueryEventKind.PointOperation"/>, the
    /// single-operation charge.
    /// </summary>
    public double RequestCharge { get; init; }

    /// <summary>
    /// Wall-clock duration of the SDK round trip. For
    /// <see cref="CosmosQueryEventKind.QueryTotal"/> this is the aggregated duration
    /// across all pages.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Number of documents or rows returned. For scalar operations this
    /// reflects the scalar result (1 for a successful value, 0 for a
    /// null FirstOrDefault, etc.). For <see cref="CosmosQueryEventKind.FeedResponsePage"/>,
    /// the count for that page. For <see cref="CosmosQueryEventKind.QueryTotal"/> on
    /// a feed, the total across pages.
    /// </summary>
    public int ResultCount { get; init; }

    /// <summary>
    /// True if this event (or any page of a multi-page query) was
    /// detected as a cross-partition execution. For
    /// <see cref="CosmosQueryEventKind.QueryTotal"/> events this is the OR across
    /// all pages.
    /// </summary>
    public bool IsCrossPartition { get; init; }
}
