using System;
using System.Linq;
using System.Threading.Tasks;
using DfT.DTRO.Attributes;
using DfT.DTRO.FeatureManagement;
using DfT.DTRO.Models;
using DfT.DTRO.Services.Data;
using DfT.DTRO.Services.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace DfT.DTRO.Controllers;

/// <summary>
/// Controller implementation that allows users to query data store events (e.g., D-TROs being created, updated or deleted).
/// </summary>
[Tags("Events")]
[ApiController]
public class EventsController : ControllerBase
{
    private readonly IStorageService _storageService;
    private readonly IDtroMappingService _eventMappingService;

    /// <summary>
    /// The default constructor.
    /// </summary>
    /// <param name="storageService">An <see cref="IStorageService"/> instance.</param>
    /// <param name="eventMappingService">An <see cref="IDtroMappingService"/> instance.</param>
    public EventsController(
        IStorageService storageService,
        IDtroMappingService eventMappingService
        )
    {
        _storageService = storageService;
        _eventMappingService = eventMappingService;
    }

    /// <summary>
    /// Endpoint for querying central data store events (e.g., D-TROs being created, updated or deleted).
    /// </summary>
    /// <param name="search">A search query object</param>
    /// <returns></returns>
    [HttpPost("/v1/events")]
    [FeatureGate(FeatureNames.DtroRead)]
    [ValidateModelState]
    [SwaggerResponse(statusCode: 200, description: "Successfully received the event list")]
    [SwaggerResponse(statusCode: 400, description: "The request was malformed.")]
    public async Task<ActionResult<DtroEventSearchResult>> Events([FromBody] DtroEventSearch search)
    {
        if (search.Since is not null && search.Since > DateTime.Now)
        {
            return BadRequest(
                new ApiErrorResponse("Bad request", "The datetime for the since field cannot be in the future.")
            );
        }

        var searchRes = await _storageService.FindDtros(search);

        var events = _eventMappingService.MapToEvents(searchRes).ToList();

        var paginatedEvents = events
            .Skip((search.Page.Value - 1) * search.PageSize.Value)
            .Take(search.PageSize.Value)
            .ToList();

        var res = new DtroEventSearchResult
        {
            TotalCount = events.Count(),
            Events = paginatedEvents,
            Page = search.Page.Value,
            PageSize = Math.Min(search.PageSize.Value, paginatedEvents.Count)
        };

        return res;
    }
}
