using System.Collections.Generic;

namespace Benday.CosmosDb.Repositories;

/// <summary>
/// Represents a page of results from a query with continuation support.
/// </summary>
/// <typeparam name="T">The type of items in the result set</typeparam>
public class PagedResults<T>
{
    /// <summary>
    /// Initializes a new instance of the PagedResults class.
    /// </summary>
    /// <param name="items">The items in this page</param>
    /// <param name="continuationToken">Token for retrieving the next page</param>
    /// <param name="hasMoreResults">Whether more results are available</param>
    /// <param name="totalRequestCharge">The total request charge in RUs for this query</param>
    public PagedResults(
        IEnumerable<T> items, 
        string? continuationToken = null, 
        bool hasMoreResults = false,
        double totalRequestCharge = 0)
    {
        Items = items ?? new List<T>();
        ContinuationToken = continuationToken;
        HasMoreResults = hasMoreResults;
        TotalRequestCharge = totalRequestCharge;
    }

    /// <summary>
    /// Gets the items in this page of results.
    /// </summary>
    public IEnumerable<T> Items { get; }

    /// <summary>
    /// Gets the continuation token for retrieving the next page.
    /// Null if there are no more results.
    /// </summary>
    public string? ContinuationToken { get; }

    /// <summary>
    /// Gets a value indicating whether more results are available.
    /// </summary>
    public bool HasMoreResults { get; }

    /// <summary>
    /// Gets the total request charge in RUs for this query.
    /// </summary>
    public double TotalRequestCharge { get; }
}