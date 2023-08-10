using System;
using System.Threading.Tasks;

namespace DfT.DTRO.Caching;

/// <summary>
/// Provides methods that support caching DTRO extraction API response data.
/// </summary>
public interface IDtroCache
{
    Task<Models.DTRO> GetDtro(Guid key);

    Task CacheDtro(Models.DTRO dtro);

    Task<bool?> GetDtroExists(Guid key);

    Task CacheDtroExists(Guid key, bool value);

    Task InvalidateDtro(Guid key);

    Task InvalidateDtroExists(Guid key);
}
