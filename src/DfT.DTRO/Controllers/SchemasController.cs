using System;
using System.Collections.Generic;
using System.IO;
using DfT.DTRO.FeatureManagement;
using DfT.DTRO.Models;
using DfT.DTRO.Services.Storage;
using DfT.DTRO.Services.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;

namespace DfT.DTRO.Controllers;

/// <summary>
/// Prototype controller for sourcing data model information.
/// </summary>
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
[FeatureGate(FeatureNames.SchemasRead)]
public class SchemasController : ControllerBase
{
    private IStorageService _storageService;

    private IJsonSchemaValidationService _jsonSchemaValidationService;

    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <param name="storageService">A <see cref="IStorageService"/> instance.</param>
    /// <param name="jsonSchemaValidationService">A <see cref="IJsonSchemaValidationService"/> instance.</param>
    public SchemasController(IStorageService storageService, IJsonSchemaValidationService jsonSchemaValidationService)
    {
        _storageService = storageService;
        _jsonSchemaValidationService = jsonSchemaValidationService;
    }

    /// <summary>
    /// Gets available data model JSON schemas.
    /// </summary>
    /// <response code="200">Okay.</response>
    [HttpGet]
    [Route("/v1/schemas")]
    public virtual IActionResult GetModels()
    {
        return Ok(new
        {
            schemas = _jsonSchemaValidationService.GetSchemas(HttpContext)
        });
    }

    /// <summary>
    /// Gets a schema by a named identifier.
    /// </summary>
    /// <response code="200">Okay.</response>
    /// <response code="404">Not found.</response>
    [HttpGet]
    [Route("/v1/schemas/{version}")]
    public virtual IActionResult GetSchema(string version)
    {
        var errors = new List<string>();

        try
        {
            var jsonSchema = _jsonSchemaValidationService.GetJsonSchemaByVersion(version);
            return Ok(new { schemaVersion = version.ToLower(), schema = jsonSchema });
        }
        catch (InvalidOperationException err)
        {
            return BadRequest(new ApiErrorResponse("Bad Request", err.Message));
        }
        catch (FileNotFoundException)
        {
            // If file of quoted name could not be found this indicates invalid data model version ID.
            errors.Add("Schema version not found");

            return NotFound(new
            {
                message = "Not found",
                errors = errors,
            });
        }
    }
}