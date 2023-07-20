using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using DfT.DTRO.Attributes;
using DfT.DTRO.DAL;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DfT.DTRO.Services.Storage;

/// <summary>
/// An implementation of <see cref="IStorageService"/>
/// that uses an SQL database as its store.
/// </summary>
public class SqlStorageService : IStorageService
{
    private readonly DtroContext _dtroContext;


    /// <summary>
    /// Informs whether this <see cref="IStorageService"/>
    /// implementation is capable of searching dtros.
    /// <br/><br/>
    /// Always <see langword="true"/> in case of <see cref="SqlStorageService"/>.
    /// </summary>
    public bool CanSearch => true;

    /// <summary>
    /// The default constructor.
    /// </summary>
    /// <param name="dtroContext">
    /// An instance of <see cref="DtroContext"/>
    /// representing the current database session.
    /// </param>
    public SqlStorageService(DtroContext dtroContext)
    {
        _dtroContext = dtroContext;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteDtro(Guid id, DateTime? deletionTime = null)
    {
        deletionTime ??= DateTime.UtcNow;

        if ((await _dtroContext.Dtros.FindAsync(id)) is not Models.DTRO existing || existing.Deleted)
        {
            return false;
        }

        existing.Deleted = true;
        existing.DeletionTime = deletionTime;

        await _dtroContext.SaveChangesAsync();

        return true;
    }

    /// <inheritdoc/>
    public Task<bool> DtroExists(Guid id)
        => _dtroContext.Dtros.AnyAsync(it => it.Id == id);

    /// <inheritdoc/>
    public Task<List<Models.DTRO>> FindDtros(DateTime? minimumPublicationTime)
    {
        IQueryable<Models.DTRO> dtros = _dtroContext.Dtros;

        if (minimumPublicationTime is DateTime minPublicationTime)
        {
            dtros = dtros.Where(it => it.Created >= minPublicationTime);
        }

        return dtros.ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<Models.DTRO> GetDtroById(Guid id)
    {
        return await _dtroContext.Dtros.FindAsync(id);
    }

    /// <inheritdoc/>
    public async Task SaveDtroAsJson(Guid id, JObject jsonContent)
    {
        var dtro = JsonConvert.DeserializeObject<Models.DTRO>(jsonContent.ToString());

        dtro.Id = id;

        await _dtroContext.Dtros.AddAsync(dtro);

        await _dtroContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task SaveDtroAsJson(Guid id, object data)
    {
        if (data is not Models.DTRO dtro)
        {
            throw new InvalidOperationException($"Parameter '{nameof(data)}' must be of type {nameof(Models.DTRO)}.");
        }

        dtro.Id = id;

        await _dtroContext.Dtros.AddAsync(dtro);

        await _dtroContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> TryUpdateDtroAsJson(Guid id, object data)
    {
        if (data is not Models.DTRO dtro)
        {
            throw new InvalidOperationException($"Parameter '{nameof(data)}' must be of type {nameof(Models.DTRO)}.");
        }

        try
        {
            await UpdateDtro(id, dtro);
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public Task UpdateDtroAsJson(Guid id, JObject jsonContent)
        => UpdateDtro(id, JsonConvert.DeserializeObject<Models.DTRO>(jsonContent.ToString()));

    /// <inheritdoc/>
    public async Task UpdateDtroAsJson(Guid id, object data)
    {
        if (data is not Models.DTRO dtro)
        {
            throw new InvalidOperationException($"Parameter '{nameof(data)}' must be of type {nameof(Models.DTRO)}.");
        }

        await UpdateDtro(id, dtro);
    }

    /// <inheritdoc/>
    public async Task UpdateDtro(Guid id, Models.DTRO dtro)
    {
        if ((await _dtroContext.Dtros.FindAsync(id)) is not Models.DTRO existing || existing.Deleted)
        {
            throw new InvalidOperationException($"There is no DTRO with Id {id}");
        }

        var props = typeof(Models.DTRO).GetProperties();

        var ignoredProps = props.Where(prop => prop.HasAttribute<SaveOnceAttribute>() || prop.HasAttribute<KeyAttribute>());

        foreach (var prop in props.Except(ignoredProps))
        {
            prop.SetValue(existing, prop.GetValue(dtro));
        }

        await _dtroContext.SaveChangesAsync();
    }
}
