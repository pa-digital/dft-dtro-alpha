using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DfT.DTRO.Attributes;
using DfT.DTRO.FeatureManagement;
using DfT.DTRO.Models;
using DfT.DTRO.Models.Filtering;
using DfT.DTRO.Models.Pagination;
using DfT.DTRO.Services.Data;
using DfT.DTRO.Services.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace DfT.DTRO.Controllers;

/// <summary>
/// Prototype controller for searching DTROs.
/// </summary>
[Tags("Search")]
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
public class SearchController : ControllerBase
{
    private readonly IStorageService _storageService;
    private readonly IDtrosFilteringService _filteringService;
    private readonly ILogger<SearchController> _logger;

    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <param name="storageService">An <see cref="IStorageService" /> instance.</param>
    /// <param name="filteringService">An <see cref="IDtrosFilteringService" /> instance.</param>
    /// <param name="logger">An <see cref="ILogger{DTROsSearchController}" /> instance.</param>
    public SearchController(
        IStorageService storageService,
        IDtrosFilteringService filteringService,
        ILogger<SearchController> logger)
    {
        _storageService = storageService;
        _filteringService = filteringService;
        _logger = logger;
    }

    /// <summary>
    /// Finds existing DTROs that match the requested criteria.
    /// </summary>
    /// <param name="body">A DTRO search criteria object.</param>
    /// <response code="200">Ok</response>
    /// <response code="400">Bad request</response>
    [HttpPost]
    [Route("/v1/search")]
    [ValidateModelState]
    [FeatureGate(FeatureNames.DtroRead)]
    [SwaggerResponse(200, type: typeof(PaginatedResponse<DtroSearchResult>), description: "Ok")]
    public async Task<IActionResult> SearchDtros([FromBody] SearchCriteria body)
    {
        const string methodName = "dtro.search";

        _logger.LogInformation("[{method}] Searching DTROs with criteria {searchCriteria}",
            methodName,
            body.ToJsonString()
        );

        try
        {
            DateTime? minPublicationTime = null;

            if (body.Queries.Any(query => query.PublicationTime is not null && query.PublicationTime > DateTime.Now))
            {
                return BadRequest(
                    new ApiErrorResponse("Bad request", "The datetime for the publicationTime field cannot be in the future.")
                );
            }

            if (body.Queries.All(query => query.PublicationTime is not null))
            {
                minPublicationTime = body.Queries.Min(query => query.PublicationTime)!.Value.ToUniversalTime();
            }

            IEnumerable<Models.DTRO> dtros = await _storageService.FindDtros(minPublicationTime);

            // Warning:
            // This implementation is intended only for prototype use.
            // Data is filtered in-memory instead of on the database side due to the limitations of current database.
            PaginatedResponse<DtroSearchResult> filteredDtros = _filteringService.Filter(dtros, body);

            _logger.LogInformation("[{method}] Found {dtrosCount} DTROs matching the criteria",
                methodName,
                filteredDtros.TotalCount
            );

            return Ok(filteredDtros);
        }
        catch (InvalidOperationException err)
        {
            return BadRequest(new ApiErrorResponse("Bad Request", err.Message));
        }
    }
}