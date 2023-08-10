using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using DfT.DTRO.Extensions;
using DfT.DTRO.Models;
using DfT.DTRO.Services.Conversion;
using Microsoft.Extensions.Configuration;

namespace DfT.DTRO.Services.Data;

/// <inheritdoc cref="IDtroMappingService"/>
public class DtroMappingService : IDtroMappingService
{
    private readonly IConfiguration _configuration;
    private readonly ISpatialProjectionService _projectionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DtroMappingService"/> class.
    /// </summary>
    /// <param name="configuration">An <see cref="IConfiguration"/> instance.</param>
    /// <param name="projectionService">An <see cref="ISpatialProjectionService"/> instance.</param>
    public DtroMappingService(IConfiguration configuration, ISpatialProjectionService projectionService)
    {
        _configuration = configuration;
        _projectionService = projectionService;
    }

    /// <inheritdoc/>
    public IEnumerable<DtroEvent> MapToEvents(IEnumerable<Models.DTRO> dtros)
    {
        var results = new List<DtroEvent>();

        var baseUrl = _configuration.GetSection("SearchServiceUrl").Value;

        foreach (var dtro in dtros)
        {
            var periods = dtro.Data
                .GetValueOrDefault<IList<object>>("source.provision")
                .OfType<ExpandoObject>()
                .SelectMany(it => it.GetValueOrDefault<IList<object>>("regulations"))
                .Where(it => it is not null)
                .OfType<ExpandoObject>()
                .Select(it => it.GetExpando("overallPeriod"))
                .OfType<ExpandoObject>();

            var regulationStartTimes = periods.Select(it => it.GetValueOrDefault<DateTime?>("start")).Where(it => it is not null).Select(it => it.Value).ToList();
            var regulationEndTimes = periods.Select(it => it.GetValueOrDefault<DateTime?>("end")).Where(it => it is not null).Select(it => it.Value).ToList();

            results.Add(DtroEvent.FromCreation(dtro, baseUrl, regulationStartTimes, regulationEndTimes));

            if (dtro.Created != dtro.LastUpdated)
            {
                results.Add(DtroEvent.FromUpdate(dtro, baseUrl, regulationStartTimes, regulationEndTimes));
            }

            if (dtro.Deleted)
            {
                results.Add(DtroEvent.FromDeletion(dtro, baseUrl, regulationStartTimes, regulationEndTimes));
            }
        }

        results.Sort((x, y) => y.EventTime.CompareTo(x.EventTime));

        return results;
    }

    /// <inheritdoc/>
    public IEnumerable<DtroSearchResult> MapToSearchResult(IEnumerable<Models.DTRO> dtros)
    {
        var results = new List<DtroSearchResult>();

        var baseUrl = _configuration.GetSection("SearchServiceUrl").Value;

        foreach (var dtro in dtros)
        {
            var periods = dtro.Data
                .GetValueOrDefault<IList<object>>("source.provision")
                .OfType<ExpandoObject>()
                .SelectMany(it => it.GetValueOrDefault<IList<object>>("regulations"))
                .Where(it => it is not null)
                .OfType<ExpandoObject>()
                .Select(it => it.GetExpando("overallPeriod"))
                .OfType<ExpandoObject>();

            var regulationStartTimes = periods.Select(it => it.GetValueOrDefault<DateTime?>("start")).Where(it => it is not null).Select(it => it.Value).ToList();
            var regulationEndTimes = periods.Select(it => it.GetValueOrDefault<DateTime?>("end")).Where(it => it is not null).Select(it => it.Value).ToList();

            results.Add(DtroSearchResult.FromDtro(dtro, baseUrl, regulationStartTimes, regulationEndTimes));
        }

        return results;
    }

    /// <inheritdoc/>
    public void InferIndexFields(Models.DTRO dtro)
    {
        dtro.InferIndexFields(_projectionService);
    }
}
