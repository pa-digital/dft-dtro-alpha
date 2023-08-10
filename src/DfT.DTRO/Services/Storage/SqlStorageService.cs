using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using DfT.DTRO.Attributes;
using DfT.DTRO.DAL;
using DfT.DTRO.Models;
using DfT.DTRO.Models.Filtering;
using DfT.DTRO.Models.Pagination;
using DfT.DTRO.Services.Conversion;
using DfT.DTRO.Services.Data;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.SwaggerGen;
using static DfT.DTRO.Extensions.ExpressionExtensions;

namespace DfT.DTRO.Services.Storage;

/// <summary>
/// An implementation of <see cref="IStorageService"/>
/// that uses an SQL database as its store.
/// </summary>
public class SqlStorageService : IStorageService
{
    private readonly DtroContext _dtroContext;
    private readonly ISpatialProjectionService _projectionService;
    private readonly IDtroMappingService _dtroMappingService;

    /// <summary>
    /// Gets a value indicating whether this <see cref="IStorageService"/>
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
    /// <param name="projectionService">An <see cref="ISpatialProjectionService"/> instance.</param>
    /// <param name="dtroMappingService">An <see cref="IDtroMappingService"/> instance.</param>
    public SqlStorageService(DtroContext dtroContext, ISpatialProjectionService projectionService, IDtroMappingService dtroMappingService)
    {
        _dtroContext = dtroContext;
        _projectionService = projectionService;
        _dtroMappingService = dtroMappingService;
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
        => _dtroContext.Dtros.AnyAsync(it => it.Id == id && !it.Deleted);

    /// <inheritdoc/>
    public async Task<Models.DTRO> GetDtroById(Guid id)
    {
        return await _dtroContext.Dtros.FindAsync(id);
    }

    /// <inheritdoc/>
    public async Task SaveDtroAsJson(Guid id, Models.DTRO data)
    {
        data.Id = id;

        _dtroMappingService.InferIndexFields(data);

        await _dtroContext.Dtros.AddAsync(data);

        await _dtroContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> TryUpdateDtroAsJson(Guid id, Models.DTRO data)
    {
        try
        {
            await UpdateDtroAsJson(id, data);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<PaginatedResult<Models.DTRO>> FindDtros(DtroSearch search)
    {
        IQueryable<Models.DTRO> result = _dtroContext.Dtros;

        var expressionsToDisjunct = new List<Expression<Func<Models.DTRO, bool>>>();

        foreach (var query in search.Queries)
        {
            var expressionsToConjunct = new List<Expression<Func<Models.DTRO, bool>>>();

            if (query.DeletionTime is DateTime deletionTime)
            {
                deletionTime = DateTime.SpecifyKind(deletionTime, DateTimeKind.Utc);
                expressionsToConjunct.Add(it => it.DeletionTime >= deletionTime);
            }
            else
            {
                expressionsToConjunct.Add(it => !it.Deleted);
            }

            if (query.Ta is int ta)
            {
                expressionsToConjunct.Add(it => it.TrafficAuthorityId == ta);
            }

            if (query.PublicationTime is DateTime publicationTime)
            {
                publicationTime = DateTime.SpecifyKind(publicationTime, DateTimeKind.Utc);
                expressionsToConjunct.Add(it => it.Created >= publicationTime);
            }

            if (query.ModificationTime is DateTime modificationTime)
            {
                modificationTime = DateTime.SpecifyKind(modificationTime, DateTimeKind.Utc);
                expressionsToConjunct.Add(it => it.LastUpdated >= modificationTime);
            }

            if (query.TroName is not null)
            {
                expressionsToConjunct.Add(it => it.TroName.ToLower().Contains(query.TroName.ToLower()));
            }

            if (query.VehicleType is not null)
            {
                expressionsToConjunct.Add(it => it.VehicleTypes.Contains(query.VehicleType));
            }

            if (query.RegulationType is not null)
            {
                expressionsToConjunct.Add(it => it.RegulationTypes.Contains(query.RegulationType));
            }

            if (query.OrderReportingPoint is not null)
            {
                expressionsToConjunct.Add(it => it.OrderReportingPoints.Contains(query.OrderReportingPoint));
            }

            if (query.Location is not null)
            {
                var bbox =
                    query.Location.Crs != "osgb36Epsg27700"
                        ? _projectionService.Wgs84ToOsgb36(query.Location.Bbox)
                        : query.Location.Bbox;

                expressionsToConjunct.Add(it => DatabaseMethods.Overlaps(bbox, it.Location));
            }

            if (query.RegulationStart is not null)
            {
                var value = query.RegulationStart.Value;

                Expression<Func<Models.DTRO, bool>> expr = query.RegulationStart.Operator switch
                {
                    ComparisonOperator.Equal => (it) => it.RegulationStart == value,
                    ComparisonOperator.LessThan => (it) => it.RegulationStart < value,
                    ComparisonOperator.LessThanOrEqual => (it) => it.RegulationStart <= value,
                    ComparisonOperator.GreaterThan => (it) => it.RegulationStart > value,
                    ComparisonOperator.GreaterThanOrEqual => (it) => it.RegulationStart >= value,
                    _ => throw new InvalidOperationException("Unsupported comparison operator.")
                };

                expressionsToConjunct.Add(expr);
            }

            if (query.RegulationEnd is not null)
            {
                var value = query.RegulationEnd.Value;

                Expression<Func<Models.DTRO, bool>> expr = query.RegulationEnd.Operator switch
                {
                    ComparisonOperator.Equal => (it) => it.RegulationEnd == value,
                    ComparisonOperator.LessThan => (it) => it.RegulationEnd < value,
                    ComparisonOperator.LessThanOrEqual => (it) => it.RegulationEnd <= value,
                    ComparisonOperator.GreaterThan => (it) => it.RegulationEnd > value,
                    ComparisonOperator.GreaterThanOrEqual => (it) => it.RegulationEnd >= value,
                    _ => throw new InvalidOperationException("Unsupported comparison operator.")
                };

                expressionsToConjunct.Add(expr);
            }

            if (!expressionsToConjunct.Any())
            {
                continue;
            }

            expressionsToDisjunct.Add(AllOf(expressionsToConjunct));
        }

        IQueryable<Models.DTRO> dataQuery = expressionsToDisjunct.Any()
            ? result
                .Where(AnyOf(expressionsToDisjunct))
            : result;

        IQueryable<Models.DTRO> paginatedQuery = dataQuery
            .OrderBy(it => it.Created)
            .Skip((search.Page - 1) * search.PageSize)
            .Take(search.PageSize);

        return new PaginatedResult<Models.DTRO>(await paginatedQuery.ToListAsync(), await dataQuery.CountAsync());
    }

    /// <inheritdoc />
    public async Task<List<Models.DTRO>> FindDtros(DtroEventSearch search)
    {
        IQueryable<Models.DTRO> result = _dtroContext.Dtros;

        var expressionsToConjunct = new List<Expression<Func<Models.DTRO, bool>>>();

        if (search.DeletionTime is DateTime deletionTime)
        {
            deletionTime = DateTime.SpecifyKind(deletionTime, DateTimeKind.Utc);

            expressionsToConjunct.Add(it => it.DeletionTime >= deletionTime);
        }

        if (search.Ta is int ta)
        {
            expressionsToConjunct.Add(it => it.TrafficAuthorityId == ta);
        }

        if (search.Since is DateTime publicationTime)
        {
            publicationTime = DateTime.SpecifyKind(publicationTime, DateTimeKind.Utc);

            expressionsToConjunct.Add(it => it.Created >= publicationTime);
        }

        if (search.ModificationTime is DateTime modificationTime)
        {
            modificationTime = DateTime.SpecifyKind(modificationTime, DateTimeKind.Utc);

            expressionsToConjunct.Add(it => it.LastUpdated >= modificationTime);
        }

        if (search.TroName is not null)
        {
            expressionsToConjunct.Add(it => it.TroName.ToLower().Contains(search.TroName.ToLower()));
        }

        if (search.VehicleType is not null)
        {
            expressionsToConjunct.Add(it => it.VehicleTypes.Contains(search.VehicleType));
        }

        if (search.RegulationType is not null)
        {
            expressionsToConjunct.Add(it => it.RegulationTypes.Contains(search.RegulationType));
        }

        if (search.OrderReportingPoint is not null)
        {
            expressionsToConjunct.Add(it => it.OrderReportingPoints.Contains(search.OrderReportingPoint));
        }

        if (search.Location is not null)
        {
            var bbox =
                search.Location.Crs != "osgb36Epsg27700"
                    ? _projectionService.Wgs84ToOsgb36(search.Location.Bbox)
                    : search.Location.Bbox;

            expressionsToConjunct.Add(it => DatabaseMethods.Overlaps(bbox, it.Location));
        }

        if (search.RegulationStart is not null)
        {
            var value = search.RegulationStart.Value;

            Expression<Func<Models.DTRO, bool>> expr = search.RegulationStart.Operator switch
            {
                ComparisonOperator.Equal => (it) => it.RegulationStart == value,
                ComparisonOperator.LessThan => (it) => it.RegulationStart < value,
                ComparisonOperator.LessThanOrEqual => (it) => it.RegulationStart <= value,
                ComparisonOperator.GreaterThan => (it) => it.RegulationStart > value,
                ComparisonOperator.GreaterThanOrEqual => (it) => it.RegulationStart >= value,
                _ => throw new InvalidOperationException("Unsupported comparison operator.")
            };

            expressionsToConjunct.Add(expr);
        }

        if (search.RegulationEnd is not null)
        {
            var value = search.RegulationEnd.Value;

            Expression<Func<Models.DTRO, bool>> expr = search.RegulationEnd.Operator switch
            {
                ComparisonOperator.Equal => (it) => it.RegulationEnd == value,
                ComparisonOperator.LessThan => (it) => it.RegulationEnd < value,
                ComparisonOperator.LessThanOrEqual => (it) => it.RegulationEnd <= value,
                ComparisonOperator.GreaterThan => (it) => it.RegulationEnd > value,
                ComparisonOperator.GreaterThanOrEqual => (it) => it.RegulationEnd >= value,
                _ => throw new InvalidOperationException("Unsupported comparison operator.")
            };

            expressionsToConjunct.Add(expr);
        }

        if (!expressionsToConjunct.Any())
        {
            return await result.OrderBy(it => it.Id).ToListAsync();
        }

        var sqlQuery = result
            .Where(AllOf(expressionsToConjunct))
            .OrderBy(it => it.Id);

        return await sqlQuery.ToListAsync();
    }

    /// <inheritdoc/>
    public async Task UpdateDtroAsJson(Guid id, Models.DTRO data)
    {
        if (await _dtroContext.Dtros.FindAsync(id) is not Models.DTRO existing || existing.Deleted)
        {
            throw new InvalidOperationException($"There is no DTRO with Id {id}");
        }

        PropertyInfo[] props = typeof(Models.DTRO).GetProperties();

        IEnumerable<PropertyInfo> ignoredProps =
            props.Where(prop => prop.HasAttribute<SaveOnceAttribute>() || prop.HasAttribute<KeyAttribute>());

        foreach (PropertyInfo prop in props.Except(ignoredProps))
        {
            prop.SetValue(existing, prop.GetValue(data));
        }

        _dtroMappingService.InferIndexFields(data);

        await _dtroContext.SaveChangesAsync();
    }
}