using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DfT.DTRO.Models;
using DfT.DTRO.Models.Filtering;
using DfT.DTRO.Models.Pagination;

namespace DfT.DTRO.Services.Storage;

/// <summary>
/// Service layer implementation for storage.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Gets a value indicating whether this <see cref="IStorageService"/>
    /// implementation is capable of searching dtros.
    /// </summary>
    public bool CanSearch { get; }

    /// <summary>
    /// Checks if the DTRO exists in the storage.
    /// </summary>
    /// <param name="id">The unique id of the DTRO.</param>
    /// <returns>
    /// A <see cref="Task"/> whose result is <see langword="true"/>
    /// if a DTRO with the specified ID exists;
    /// otherwise <see langword="false"/>.
    /// </returns>
    Task<bool> DtroExists(Guid id);

    /// <summary>
    /// Saves a DTRO provided in <paramref name="data"/> to a storage device
    /// after converting it to a JSON string.
    /// </summary>
    /// <param name="id">The unique id of the DTRO.</param>
    /// <param name="data">The DTRO Json content.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous save operation.</returns>
    Task SaveDtroAsJson(Guid id, Models.DTRO data);

    /// <summary>
    /// Gets a DTRO domain object from storage by a quoted id.
    /// </summary>
    /// <param name="id">The unique identifier of the DTRO.</param>
    /// <returns>A <see cref="Models.DTRO"/> instance.</returns>
    Task<Models.DTRO> GetDtroById(Guid id);

    /// <summary>
    /// Saves a DTRO provided in <paramref name="data"/> to a storage device
    /// after converting it to a JSON string.
    /// </summary>
    /// <param name="id">The unique id of the DTRO.</param>
    /// <param name="data">The DTRO Json content.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous update operation.</returns>
    Task UpdateDtroAsJson(Guid id, Models.DTRO data);

    /// <summary>
    /// Tries to update the DTRO.
    /// </summary>
    /// <param name="id">The unique id of the DTRO.</param>
    /// <param name="data">The DTRO Json content.</param>
    /// <returns>
    /// A <see cref="Task"/> that resolves to <see langword="true"/>
    /// if the DTRO was successfully updated
    /// or <see langword="false"/> if it was not found.
    /// </returns>
    Task<bool> TryUpdateDtroAsJson(Guid id, Models.DTRO data);

    /// <summary>
    /// Marks the specified DTRO as deleted (does not delete the DTRO immediately).
    /// </summary>
    /// <param name="id">The unique id of the DTRO.</param>
    /// <param name="deletionTime">The time of deletion. Will default to <see cref="DateTime.UtcNow"/> if not provided.</param>
    /// <returns>
    /// A <see cref="Task"/> that resolves to <see langword="true"/>
    /// if the DTRO was successfully marked deleted
    /// or <see langword="false"/> if it was not found.
    /// </returns>
    Task<bool> DeleteDtro(Guid id, DateTime? deletionTime = null);

    /// <summary>
    /// Finds all DTROs that match the criteria specified in <paramref name="search"/>.
    /// </summary>
    /// <param name="search">The search criteria.</param>
    /// <returns>A <see cref="Task"/> that resolves to the paginated list of <see cref="Models.DTRO"/> that match the criteria.</returns>
    Task<PaginatedResult<Models.DTRO>> FindDtros(DtroSearch search);

    /// <summary>
    /// Finds all DTRO events that match the criteria specified in <paramref name="search"/>.
    /// </summary>
    /// <param name="search">The search criteria.</param>
    /// <returns>A <see cref="Task"/> that resolves to a collection of <see cref="Models.DTRO"/> that match the criteria.</returns>
    Task<List<Models.DTRO>> FindDtros(DtroEventSearch search);
}