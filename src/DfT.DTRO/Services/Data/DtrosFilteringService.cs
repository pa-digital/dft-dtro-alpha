using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using DfT.DTRO.Extensions;
using DfT.DTRO.Models;
using DfT.DTRO.Models.Filtering;
using DfT.DTRO.Models.Pagination;
using DfT.DTRO.Services.Conversion;
using Microsoft.Extensions.Configuration;

namespace DfT.DTRO.Services.Data;

/// <inheritdoc cref="IDtrosFilteringService"/>
public class DtrosFilteringService : IDtrosFilteringService
{
    private readonly IConfiguration _configuration;
    private readonly ISpatialProjectionService _spatialProjectionService;

    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <param name="configuration">The application's configuration.</param>
    /// <param name="spatialProjectionService">An <see cref="ISpatialProjectionService"/> instance.</param>
    public DtrosFilteringService(IConfiguration configuration, ISpatialProjectionService spatialProjectionService)
    {
        _configuration = configuration;
        _spatialProjectionService = spatialProjectionService;
    }

    /// <inheritdoc />
    public PaginatedResponse<DtroSearchResult> Filter(IEnumerable<Models.DTRO> dtros,
        SearchCriteria searchCriteria)
    {
        // Warning:
        // This implementation is intended only for prototype use.
        // Data is filtered in-memory instead of on the database side due to the limitations of current database.

        List<Models.DTRO> dtrosList = dtros.Where(dtro => !dtro.Deleted).ToList();

        if (!dtrosList.Any())
        {
            return new PaginatedResponse<DtroSearchResult>(
                Array.Empty<DtroSearchResult>(),
                searchCriteria.Page,
                0
            );
        }

        if (searchCriteria.Page > Math.Ceiling((decimal)dtrosList.Count / searchCriteria.PageSize))
        {
            throw new InvalidOperationException("Requested page does not exist.");
        }

        var extracted = FilterAndExtract(dtrosList, searchCriteria);

        List<DtroSearchResult> results = new();

        foreach (var (_, (dtro, data)) in extracted)
        {
            results.Add(new DtroSearchResult
            {
                TroName = dtro.Data.GetValueOrDefault<string>("source.troName"),
                HighwayAuthorityId = dtro.Data.GetValueOrDefault<int>("source.ha"),
                PublicationTime = dtro.Created.Value,
                RegulationType = data.RegulationTypes,
                VehicleType = data.VehicleTypes,
                OrderReportingPoint = data.OrderReportingPoints,
                RegulationStart = data.PeriodStartDates,
                RegulationEnd = data.PeriodEndDates,
                Links = new Links
                {
                    Self = $"{_configuration.GetSection("SearchServiceUrl").Value}/v1/dtros/{dtro.Id}"
                }
            }
            );
        }

        return
            new PaginatedResponse<DtroSearchResult>(
                results
                    .Skip(searchCriteria.PageSize * (searchCriteria.Page - 1))
                    .Take(searchCriteria.PageSize)
                    .ToList(),
                searchCriteria.Page,
                results.Count
            );
    }

    /// <inheritdoc/>
    public DtroEventSearchResult Filter(IEnumerable<Models.DTRO> dtros, DtroEventSearch search)
    {
        var queries = new List<SearchQuery>()
        {
            new()
            {
                RegulationType = search.RegulationType,
                VehicleType = search.VehicleType,
                Ha = search.Ha,
                OrderReportingPoint = search.OrderReportingPoint,
                TroName = search.TroName,
                Location = search.Location,
                RegulationStart = search.RegulationStart,
                RegulationEnd = search.RegulationEnd
            }
        };

        var extracted = FilterAndExtract(dtros, new() { Queries = queries, Page = search.Page.Value, PageSize = search.PageSize.Value });

        var events = new List<DtroEvent>();

        foreach (var (_, (dtro, data)) in extracted)
        {
            if (dtro.Created > search.Since)
            {
                events.Add(CreateDtroEvent(dtro, data, DtroEventType.Create, dtro.Created));
            }
            if (dtro.LastUpdated > search.Since && dtro.LastUpdated != dtro.Created)
            {
                events.Add(CreateDtroEvent(dtro, data, DtroEventType.Update, dtro.LastUpdated));
            }
            if (dtro.DeletionTime > search.Since)
            {
                events.Add(CreateDtroEvent(dtro, data, DtroEventType.Delete, dtro.DeletionTime));
            }
        }

        return new DtroEventSearchResult
        {
            PageSize = search.PageSize.Value,
            Page = search.Page.Value,
            TotalCount = events.Count,
            Events = events
                .OrderBy(it => it.EventTime)
                .Skip(search.PageSize.Value * (search.Page.Value - 1))
                .Take(search.PageSize.Value)
                .ToList()
        };
    }

    private Dictionary<string, (Models.DTRO, DtroExtractedData)> FilterAndExtract(IEnumerable<Models.DTRO> dtros,
        SearchCriteria searchCriteria)
    {
        // Warning:
        // This implementation is intended only for prototype use.
        // Data is filtered in-memory instead of on the database side due to the limitations of current database.

        Dictionary<string, (Models.DTRO, DtroExtractedData)> dataExtractedFromDtro = new();

        List<Models.DTRO> dtrosList = dtros.ToList();

        foreach (dynamic dtroDyn in dtrosList)
        {
            var dtro = dtroDyn as Models.DTRO;

            IEnumerable<ExpandoObject> provisionsList = this.GetList(dtroDyn.Data.source, "provision");
            List<ExpandoObject> regulationsList =
                provisionsList.SelectMany(provision =>
                    GetList(provision, "regulations")).ToList();
            List<ExpandoObject> conditionsList =
                regulationsList.SelectMany(regulation => GetList(regulation, "conditions")).ToList();
            List<ExpandoObject> vehicleCharacteristicsList =
                conditionsList.Select(condition => GetObj(condition, "vehicleCharacteristics"))
                    .Where(expandoObject => expandoObject is not null).ToList();
            List<ExpandoObject> overallPeriodsList =
                regulationsList.Select(regulation => GetObj(regulation, "overallPeriod"))
                    .Where(overallPeriod => overallPeriod is not null)
                    .ToList();

            List<string> orderReportingPoints =
                provisionsList.Select(provision => GetString(provision, "orderReportingPoint"))
                    .Where(str => str is not null)
                    .Distinct()
                    .ToList()!;
            List<string> vehicleTypes =
                vehicleCharacteristicsList.SelectMany(characteristic => GetStringsList(characteristic, "vehicleType"))
                    .Distinct()
                    .ToList();
            List<string> regulationTypes =
                regulationsList.Select(regulation => GetString(regulation, "regulationType"))
                    .Where(str => str is not null)
                    .Distinct()
                    .ToList();
            List<string> speedLimitRegulationTypes =
                regulationsList.Select(regulation => GetString(regulation, "type"))
                    .Where(str => str is not null)
                    .Distinct()
                    .ToList();
            bool doesProvisionIncludeOffListRegulation =
                regulationsList
                    .Select(regulation => GetString(regulation, "regulationFullText")).Any(str => str is not null);
            List<DateTime> periodStartDates =
                overallPeriodsList.Select(period => GetDateTime(period, "start"))
                    .Where(date => date is not null)
                    .Cast<DateTime>()
                    .ToList();
            List<DateTime> periodEndDates =
                overallPeriodsList.Select(period => GetDateTime(period, "end"))
                    .Where(date => date is not null)
                    .Cast<DateTime>()
                    .ToList();

            // regulationType query parameter is applied also to some types regulations without `regulationType` field
            List<string> allRegulationTypes =
                regulationTypes.Concat(speedLimitRegulationTypes).ToList();
            if (doesProvisionIncludeOffListRegulation)
            {
                allRegulationTypes.Add("offListRegulation");
            }

            var coords = provisionsList.SelectMany(it => GetList(it, "regulatedPlace"))
                .Select(it => GetObj(it, "geometry"))
                .Select(geometry => Tuple.Create(geometry, geometry.GetValue<string>("crs")))
                .Select(it => (it.Item1.GetExpando("coordinates"), it.Item2))
                .Select(it => (it.Item1.GetList("coordinates").OfType<IList<object>>(), it.Item2))
                .SelectMany(it => it.Item1.Select(l => Tuple.Create(l, it.Item2)))
                .SelectMany(it => it.Item1.OfType<IList<object>>().Select(l => Tuple.Create(l, it.Item2))
                .Select(it => (it.Item1.Select(coord => coord is long l ? l : coord is double d ? d : default), it.Item2))
                .Select(it => (new Coordinates(it.Item1.ElementAt(0), it.Item1.ElementAt(1)), it.Item2.ToLowerInvariant())));

            foreach (SearchQuery searchCriteriaQuery in searchCriteria.Queries)
            {
                if (searchCriteriaQuery.TroName is not null
                    && !((string)dtroDyn.Data.source.troName).ToLower().Contains(searchCriteriaQuery.TroName.ToLower()))
                {
                    continue;
                }

                if (searchCriteriaQuery.OrderReportingPoint is not null
                    && orderReportingPoints.All(reportingPoint =>
                        reportingPoint != searchCriteriaQuery.OrderReportingPoint))
                {
                    continue;
                }

                if (searchCriteriaQuery.RegulationType is not null
                    && allRegulationTypes.All(regulationType =>
                        regulationType != searchCriteriaQuery.RegulationType))
                {
                    continue;
                }

                if (searchCriteriaQuery.VehicleType is not null
                    && vehicleTypes.All(vehicleType => vehicleType != searchCriteriaQuery.VehicleType))
                {
                    continue;
                }

                if (searchCriteriaQuery.Ha is not null
                    && dtro.Data.GetValueOrDefault<int>("source.ha") != searchCriteriaQuery.Ha)
                {
                    continue;
                }

                if (searchCriteriaQuery.RegulationStart is not null
                    && periodStartDates.All(periodStart => !searchCriteriaQuery.RegulationStart.IsSatisfied(periodStart)))
                {
                    continue;
                }

                if (searchCriteriaQuery.RegulationEnd is not null
                    && periodEndDates.All(periodEnd => !searchCriteriaQuery.RegulationEnd.IsSatisfied(periodEnd)))
                {
                    continue;
                }

                if (searchCriteriaQuery.Location is Location location)
                {
                    bool matched = false;

                    BoundingBox? projectedBbox = null;
                    var locationCrs = location.Crs.ToLowerInvariant();

                    foreach (var (coord, crs) in coords)
                    {
                        if (locationCrs == crs && location.Bbox.Contains(coord))
                        {
                            matched = true;
                            break;
                        }
                        else if (locationCrs != crs && locationCrs == "osgb36epsg27700")
                        {
                            var projectedCoord = _spatialProjectionService.Wgs84ToOsgb36(coord);
                            if (location.Bbox.Contains(projectedCoord))
                            {
                                matched = true;
                                break;
                            }
                        }
                        else
                        {
                            projectedBbox ??= _spatialProjectionService.Wgs84ToOsgb36(location.Bbox);
                            if (projectedBbox.Value.Contains(coord))
                            {
                                matched = true;
                                break;
                            }
                        }
                    }

                    if (!matched)
                    {
                        continue;
                    }
                }

                dataExtractedFromDtro[dtroDyn.Id.ToString()] =
                    (dtroDyn as Models.DTRO,
                    new DtroExtractedData
                    (
                        vehicleTypes,
                        allRegulationTypes,
                        orderReportingPoints,
                        periodStartDates,
                        periodEndDates
                    ));
            }
        }

        return dataExtractedFromDtro;
    }

    private DtroEvent CreateDtroEvent(
        Models.DTRO dtro,
        DtroExtractedData data,
        DtroEventType dtroEventType,
        DateTime? eventTime
    )
    {
        return new DtroEvent
        {
            EventType = dtroEventType,
            EventTime = eventTime.Value,
            PublicationTime = dtro.Created.Value,
            OrderReportingPoint = data.OrderReportingPoints.ToList(),
            VehicleType = data.VehicleTypes.ToList(),
            HighwayAuthorityId = dtro.Data.GetValueOrDefault<int>("source.ha"),
            RegulationType = data.RegulationTypes.ToList(),
            TroName = dtro.Data.GetValueOrDefault<string>("source.troName"),
            RegulationStart = data.PeriodStartDates.ToList(),
            RegulationEnd = data.PeriodEndDates.ToList(),
            Links = new Links { Self = $"{_configuration.GetSection("SearchServiceUrl").Value}/v1/dtros/{dtro.Id}" }
        };
    }

    private string GetString(ExpandoObject data, string key)
    {
        IDictionary<string, object> dictionary = data;

        return dictionary.TryGetValue(key, out object value) && value is string str ? str : null;
    }

    private long? GetNumber(ExpandoObject data, string key)
    {
        IDictionary<string, object> dictionary = data;

        return dictionary.TryGetValue(key, out object value) && value is long number ? number : null;
    }

    private DateTime? GetDateTime(ExpandoObject data, string key)
    {
        IDictionary<string, object> dictionary = data;

        return dictionary.TryGetValue(key, out object value) && value is DateTime date ? date : null;
    }

    private ExpandoObject GetObj(ExpandoObject data, string key)
    {
        IDictionary<string, object> dictionary = data;

        return dictionary.TryGetValue(key, out object outValue) && outValue is ExpandoObject value
            ? value
            : null;
    }

    private IEnumerable<ExpandoObject> GetList(ExpandoObject data, string key)
    {
        IDictionary<string, object> dictionary = data;

        if (dictionary.TryGetValue(key, out object outValue))
        {
            return ((IEnumerable<object>)outValue ?? Enumerable.Empty<object>())
                .OfType<ExpandoObject>();
        }

        return Enumerable.Empty<ExpandoObject>();
    }

    private IEnumerable<string> GetStringsList(ExpandoObject data, string key)
    {
        IDictionary<string, object> dictionary = data;

        if (dictionary.TryGetValue(key, out object outValue))
        {
            return ((IEnumerable<object>)outValue ?? Enumerable.Empty<object>())
                .OfType<string>();
        }

        return Enumerable.Empty<string>();
    }
}