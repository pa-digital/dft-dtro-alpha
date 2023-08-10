using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace DfT.DTRO.Caching;

/// <summary>
/// Implements <see cref="IDtroCache"/> in a way that
/// does not do anything when writing
/// and always simulates cache misses when reading.
/// </summary>
[ExcludeFromCodeCoverage /* this class is trivial */]
public class NoopDtroCache : IDtroCache
{
    /// <inheritdoc/>
    public Task CacheDtro(Models.DTRO dtro)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public Task CacheDtroExists(Guid key, bool value)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<Models.DTRO> GetDtro(Guid key)
        => Task.FromResult<Models.DTRO>(null);

    /// <inheritdoc/>
    public Task<bool?> GetDtroExists(Guid key)
        => Task.FromResult<bool?>(null);

    /// <inheritdoc/>
    public Task InvalidateDtro(Guid key)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public Task InvalidateDtroExists(Guid key)
        => Task.CompletedTask;
}
