using DfT.DTRO.Attributes;
using DfT.DTRO.FeatureManagement;
using DfT.DTRO.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using DfT.DTRO.Services.Storage;
using DfT.DTRO.Services.Data;
using Microsoft.AspNetCore.Http;
using System;

namespace DfT.DTRO.Controllers;

/// <summary>
/// Controller implementation that allows users to query data store events (e.g., D-TROs being created, updated or deleted).
/// </summary>
[Tags("Events")]
[ApiController]
public class EventsController : ControllerBase
{
    private readonly IStorageService _storageService;
    private readonly IDtrosFilteringService _filteringService;

    /// <summary>
    /// The default constructor.
    /// </summary>
    /// <param name="storageService">An <see cref="IStorageService"/> instance.</param>
    /// <param name="filteringService">An <see cref="IDtrosFilteringService"/> instance.</param>
    public EventsController(
        IStorageService storageService,
        IDtrosFilteringService filteringService
        )
    {
        _storageService = storageService;
        _filteringService = filteringService;
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

        var dtros = await _storageService.FindDtros(search.Since?.ToUniversalTime());

        // Warning:
        // This implementation is intended only for prototype use.
        // Data is filtered in-memory instead of on the database side due to the limitations of current database.
        var filteredDtros = _filteringService.Filter(dtros, search);

        return filteredDtros;
    }
}
