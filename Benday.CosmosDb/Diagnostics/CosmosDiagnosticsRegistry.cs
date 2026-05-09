using System;
using System.Collections.Concurrent;

namespace Benday.CosmosDb.Diagnostics;

/// <summary>
/// Singleton registry that holds per-entity-type diagnostic options for
/// <c>CosmosRepository&lt;T&gt;</c>. Repositories resolve this from DI in
/// their constructor and look up their own options via
/// <see cref="GetFor{TEntity}"/>.
/// </summary>
/// <remarks>
/// Configure through
/// <see cref="Utilities.CosmosRegistrationHelper.ConfigureDiagnostics{TEntity}"/>
/// or <see cref="Utilities.CosmosRegistrationHelper.ConfigureDiagnosticsDefault"/>
/// — those helpers add or fetch the singleton from the service collection
/// and mutate it. Direct use of <see cref="Set{TEntity}"/> /
/// <see cref="SetDefault"/> is supported but not the typical path.
/// </remarks>
public sealed class CosmosDiagnosticsRegistry
{
    private readonly ConcurrentDictionary<Type, CosmosRepositoryDiagnosticsOptions> _byEntity = new();
    private readonly CosmosRepositoryDiagnosticsOptions _default = new();

    /// <summary>
    /// Returns the options for <typeparamref name="TEntity"/>. If the entity
    /// has no per-entity entry, returns the registry default. Never returns
    /// null.
    /// </summary>
    public CosmosRepositoryDiagnosticsOptions GetFor<TEntity>() =>
        _byEntity.TryGetValue(typeof(TEntity), out var options) ? options : _default;

    /// <summary>
    /// Applies <paramref name="configure"/> to the options for
    /// <typeparamref name="TEntity"/>, creating an entry if one doesn't
    /// already exist.
    /// </summary>
    public void Set<TEntity>(Action<CosmosRepositoryDiagnosticsOptions> configure)
    {
        if (configure is null) throw new ArgumentNullException(nameof(configure));

        var options = _byEntity.GetOrAdd(typeof(TEntity), _ => new CosmosRepositoryDiagnosticsOptions());
        configure(options);
    }

    /// <summary>
    /// Applies <paramref name="configure"/> to the registry default. The
    /// default is returned by <see cref="GetFor{TEntity}"/> for any entity
    /// type that has not been configured explicitly.
    /// </summary>
    public void SetDefault(Action<CosmosRepositoryDiagnosticsOptions> configure)
    {
        if (configure is null) throw new ArgumentNullException(nameof(configure));

        configure(_default);
    }
}
