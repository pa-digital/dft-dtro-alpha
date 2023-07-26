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
    private readonly ILogger<SearchController> _logger;
    private readonly IDtroMappingService _mappingService;

    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <param name="storageService">An <see cref="IStorageService" /> instance.</param>
    /// <param name="logger">An <see cref="ILogger{DTROsSearchController}" /> instance.</param>
    /// <param name="mappingService">An <see cref="IDtroMappingService" /> instance.</param>
    public SearchController(
        IStorageService storageService,
        ILogger<SearchController> logger,
        IDtroMappingService mappingService)
    {
        _storageService = storageService;
        _logger = logger;
        _mappingService = mappingService;
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
    public async Task<ActionResult<PaginatedResponse<DtroSearchResult>>> SearchDtros([FromBody] DtroSearch body)
    {
        const string methodName = "dtro.search";

        _logger.LogInformation("[{method}] Searching DTROs with criteria {searchCriteria}",
            methodName,
            body.ToJsonString()
        );

        try
        {
            var result = await _storageService.FindDtros(body);

            IEnumerable<DtroSearchResult> mappedResult = _mappingService.MapToSearchResult(result.Results);

            PaginatedResponse<DtroSearchResult> paginatedResult =
                new(mappedResult.ToList().AsReadOnly(),
                    body.Page,
                    result.TotalCount);

            return paginatedResult;
        }
        catch (InvalidOperationException err)
        {
            return BadRequest(new ApiErrorResponse("Bad Request", err.Message));
        }
    }
}