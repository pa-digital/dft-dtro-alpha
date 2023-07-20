using System.Collections.Generic;
using DfT.DTRO.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using SchemaVersion = DfT.DTRO.Models.SchemaVersion;

namespace DfT.DTRO.Services.Validation;

/// <summary>
/// Service layer implementation for working with JSON schemas.
/// </summary>
public interface IJsonSchemaValidationService
{
    /// <summary>
    /// Gets a JSON schema by a quoted version identifier.
    /// </summary>
    /// <param name="request">A DTRO submission request.</param>
    /// <returns>A JSON schema for use in creation by the quoted version identifier.</returns>
    string GetJsonSchemaForRequestAsString(Models.DTRO request);

    /// <summary>
    /// Validates a request against a JSON Schema version.
    /// </summary>
    /// <param name="jsonSchemaAsString">The JSON schema to validate against in a string format.</param>
    /// <param name="inputJson">The DTRO submission request JSON string value.</param>
    /// <returns>A list of validation errors (if any are found).</returns>
    IList<ValidationError> ValidateRequestAgainstJsonSchema(string jsonSchemaAsString, string inputJson);

    /// <summary>
    /// Sources model versions that are available to submit against.
    /// </summary>
    /// <param name="httpContext">The inbound HTTP context for protocol assessment in specifying relative location.</param>
    /// <returns></returns>
    IList<SchemaDefinition> GetSchemas(HttpContext httpContext);

    /// <summary>
    /// Gets a JSON schema for new record creation by a quoted identifier.
    /// </summary>
    /// <param name="schemaVersion">The schema to load.</param>
    /// <returns>A parsed JObject schema.</returns>
    JObject GetJsonSchemaByVersion(SchemaVersion schemaVersion);
}