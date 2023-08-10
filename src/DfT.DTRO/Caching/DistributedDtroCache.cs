using System;
using System.Threading.Tasks;
using DfT.DTRO.Extensions;
using Microsoft.Extensions.Caching.Distributed;

namespace DfT.DTRO.Caching;

/// <summary>
/// Provides methods that support caching DTRO extraction API response data using a distributed cache.
/// </summary>
public class DistributedDtroCache : IDtroCache
{
    private readonly IDistributedCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedDtroCache"/> class.
    /// </summary>
    /// <param name="cache">An <see cref="IDistributedCache"/> instance.</param>
    public DistributedDtroCache(IDistributedCache cache)
    {
        _cache = cache;
    }

    /// <inheritdoc/>
    public Task CacheDtro(Models.DTRO dtro)
        => _cache.SetValueAsync(dtro.Id.ToString(), dtro);

    /// <inheritdoc/>
    public Task CacheDtroExists(Guid key, bool value)
        => _cache.SetBoolAsync(DtroExistsCacheKey(key), value);

    /// <inheritdoc/>
    public Task<Models.DTRO> GetDtro(Guid key)
        => _cache.GetValueAsync<Models.DTRO>(key.ToString());

    /// <inheritdoc/>
    public Task<bool?> GetDtroExists(Guid key)
        => _cache.GetBoolAsync(DtroExistsCacheKey(key));

    /// <inheritdoc/>
    public Task InvalidateDtro(Guid key)
        => _cache.RemoveAsync(key.ToString());

    /// <inheritdoc/>
    public Task InvalidateDtroExists(Guid key)
        => _cache.RemoveAsync(DtroExistsCacheKey(key));

    private static string DtroExistsCacheKey(Guid key)
        => $"exists_{key}";
}
