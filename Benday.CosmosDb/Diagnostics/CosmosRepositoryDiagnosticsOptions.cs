namespace Benday.CosmosDb.Diagnostics;

/// <summary>
/// Per-entity diagnostics toggles read by <c>CosmosRepository&lt;T&gt;</c> at
/// query time. Configure these through the
/// <see cref="Utilities.CosmosRegistrationHelper.ConfigureDiagnostics{TEntity}"/>
/// and <see cref="Utilities.CosmosRegistrationHelper.ConfigureDiagnosticsDefault"/>
/// helpers; the repository looks up its options from
/// <see cref="CosmosDiagnosticsRegistry"/> in its constructor.
/// </summary>
public sealed class CosmosRepositoryDiagnosticsOptions
{
    /// <summary>
    /// When true, the repository sets
    /// <c>QueryRequestOptions.PopulateIndexMetrics = true</c> on every query
    /// it issues, and copies <c>FeedResponse.IndexMetrics</c> into the
    /// <see cref="CosmosQueryDiagnostics.IndexMetrics"/> field for sinks
    /// to record. Defaults to <c>false</c> — Microsoft recommends only
    /// enabling this while debugging slow queries because it adds
    /// measurable RU and latency overhead per request.
    /// </summary>
    public bool CaptureIndexMetrics { get; set; }
}
