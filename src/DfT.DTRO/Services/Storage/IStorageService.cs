using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DfT.DTRO.Services.Storage;

/// <summary>
/// Service layer implementation for storage.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Informs whether this <see cref="IStorageService"/>
    /// implementation is capable of searching dtros.
    /// </summary>
    public bool CanSearch { get; }

    /// <summary>
    /// Checks if the DTRO exists in the storage.
    /// </summary>
    /// <param name="id">The unique id of the DTRO</param>
    /// <returns>
    /// A <see cref="Task"/> whose result is <see langword="true"/>
    /// if a DTRO with the specified ID exists;
    /// otherwise <see langword="false"/>
    /// </returns>
    Task<bool> DtroExists(Guid id);

    /// <summary>
    /// Saves a DTRO in a JSON format to a storage device.
    /// </summary>
    /// <param name="id">The unique id of the DTRO.</param>
    /// <param name="jsonContent">The DTRO Json content.</param>
    Task SaveDtroAsJson(Guid id, JObject jsonContent);

    /// <summary>
    /// Saves a DTRO provided in <paramref name="data"/> to a storage device
    /// after converting it to a JSON string.
    /// </summary>
    /// <param name="id">The unique id of the DTRO.</param>
    /// <param name="data">The DTRO Json content.</param>
    Task SaveDtroAsJson(Guid id, object data);

    /// <summary>
    /// Gets a DTRO domain object from storage by a quoted id.
    /// </summary>
    /// <param name="id">The unique identifier of the DTRO.</param>
    /// <returns>A <see cref="Models.DTRO"/> instance</returns>
    Task<Models.DTRO> GetDtroById(Guid id);

    /// <summary>
    /// Saves a DTRO in a JSON format to a storage device.
    /// </summary>
    /// <param name="id">The unique id of the DTRO.</param>
    /// <param name="jsonContent">The DTRO Json content.</param>
    Task UpdateDtroAsJson(Guid id, JObject jsonContent);

    /// <summary>
    /// Saves a DTRO provided in <paramref name="data"/> to a storage device
    /// after converting it to a JSON string.
    /// </summary>
    /// <param name="id">The unique id of the DTRO.</param>
    /// <param name="data">The DTRO Json content.</param>
    Task UpdateDtroAsJson(Guid id, object data);

    /// <summary>
    ///     Finds all DTROs published after date specified in <paramref name="minimumPublicationTime" />
    /// </summary>
    /// <param name="minimumPublicationTime">The publication time of the oldest DTRO that should be included in the results.</param>
    Task<List<Models.DTRO>> FindDtros(DateTime? minimumPublicationTime);

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
    Task<bool> TryUpdateDtroAsJson(Guid id, object data);

    /// <summary>
    /// Marks the specified DTRO as deleted (does not delete the DTRO immediately).
    /// </summary>
    /// <param name="id">The unique id of the DTRO.</param>
    /// <param name="deletionTime">The time of deletion. Will default to <see cref="DateTime.UtcNow"/> if not provided.</param>
    /// <returns>
    /// A <see cref="Task"/> that resolves to <see langword="true"/>
    /// if the DTRO was successfully marked dteleted
    /// or <see langword="false"/> if it was not found.
    /// </returns>
    Task<bool> DeleteDtro(Guid id, DateTime? deletionTime = null);
}