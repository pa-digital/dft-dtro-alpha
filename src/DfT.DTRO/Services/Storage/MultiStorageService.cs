using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DfT.DTRO.Models;
using DfT.DTRO.Models.Filtering;
using DfT.DTRO.Models.Pagination;
using Microsoft.Extensions.Logging;

namespace DfT.DTRO.Services.Storage;

/// <summary>
/// An implementation of <see cref="IStorageService"/>
/// that delegates its operations to one or more
/// other implementations of <see cref="IStorageService"/>.
/// </summary>
public class MultiStorageService : IStorageService
{
    private readonly IEnumerable<IStorageService> _services;
    private readonly ILogger<MultiStorageService> _logger;

    // TODO: Let's make sure this goes away after we resolve this.
    private readonly bool _writeToBucketOnly;

    /// <summary>
    /// Gets a value indicating whether this <see cref="IStorageService"/>
    /// implementation is capable of searching dtros.
    /// <br/><br/>
    /// The result depends on the services this <see cref="MultiStorageService"/> is using.
    /// </summary>
    public bool CanSearch => _services.Any(it => it.CanSearch);

    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <param name="services">
    /// The collection of other <see cref="IStorageService"/> implementations
    /// that this <see cref="MultiStorageService"/> will delegate to.
    /// </param>
    /// <param name="logger">A logger to be used by this service.</param>
    /// <param name="writeToBucketOnly">Indicates whether Postgres should be used for writing data.</param>
    public MultiStorageService(
        IEnumerable<IStorageService> services,
        ILogger<MultiStorageService> logger,
        bool writeToBucketOnly)
    {
        _services = services;
        _logger = logger;
        _writeToBucketOnly = writeToBucketOnly;
    }

    /// <inheritdoc/>
    public async Task<Models.DTRO> GetDtroById(Guid id)
    {
        var exceptions = new List<Exception>();

        foreach (var service in _services)
        {
            try
            {
                return await service.GetDtroById(id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Getting DTRO with Id {dtroId} from storage service of type '{storageServiceType}' failed.",
                    id,
                    service.GetType().Name);
                exceptions.Add(ex);
            }
        }

        throw new AggregateException(exceptions);
    }

    /// <inheritdoc/>
    public async Task SaveDtroAsJson(Guid id, Models.DTRO data)
    {
        if (_writeToBucketOnly)
        {
            await _services.OfType<FileStorageService>().First().SaveDtroAsJson(id, data);
            return;
        }

        foreach (var service in _services)
        {
            await service.SaveDtroAsJson(id, data);
        }
    }

    /// <inheritdoc/>
    public async Task UpdateDtroAsJson(Guid id, Models.DTRO data)
    {
        if (_writeToBucketOnly)
        {
            await _services.OfType<FileStorageService>().First().UpdateDtroAsJson(id, data);
            return;
        }

        foreach (var service in _services)
        {
            await service.UpdateDtroAsJson(id, data);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> TryUpdateDtroAsJson(Guid id, Models.DTRO data)
    {
        if (_writeToBucketOnly)
        {
            return await _services.OfType<FileStorageService>().First().TryUpdateDtroAsJson(id, data);
        }

        foreach (var service in _services)
        {
            var res = await service.TryUpdateDtroAsJson(id, data);

            if (!res)
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteDtro(Guid id, DateTime? deletionTime = null)
    {
        deletionTime ??= DateTime.UtcNow;

        if (_writeToBucketOnly)
        {
            return await _services.OfType<FileStorageService>().First().DeleteDtro(id, deletionTime);
        }

        foreach (var service in _services)
        {
            var res = await service.DeleteDtro(id, deletionTime);

            if (!res)
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> DtroExists(Guid id)
    {
        List<Exception> exceptions = new ();

        if (_writeToBucketOnly)
        {
            return await _services.OfType<FileStorageService>().First().DtroExists(id);
        }

        foreach (var service in _services)
        {
            try
            {
                return await service.DtroExists(id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Checking if DTRO with Id {dtroId} exists in storage service of type '{storageServiceType}' failed.",
                    id,
                    service.GetType().Name);
                exceptions.Add(ex);
            }
        }

        throw new AggregateException(exceptions);
    }

    /// <inheritdoc/>
    public Task<PaginatedResult<Models.DTRO>> FindDtros(DtroSearch search)
    {
        var service = _services.FirstOrDefault(service => service.CanSearch);

        return service is null
            ? throw new InvalidOperationException("None of the storage services used is capable of search.")
            : service.FindDtros(search);
    }

    /// <inheritdoc />
    public Task<List<Models.DTRO>> FindDtros(DtroEventSearch search)
    {
        var service = _services.FirstOrDefault(service => service.CanSearch);

        return service is null
            ? throw new InvalidOperationException("None of the storage services used is capable of search.")
            : service.FindDtros(search);
    }
}
