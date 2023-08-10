using System.Collections.Generic;
using System.IO;
using DfT.DTRO.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using SchemaVersion = DfT.DTRO.Models.SchemaVersion;

namespace DfT.DTRO.Services.Validation;

/// <inheritdoc />
public class JsonSchemaValidationService : IJsonSchemaValidationService
{
    private const string SchemaFolder = "Schemas";

    /// <inheritdoc />
    public string GetJsonSchemaForRequestAsString(Models.DTRO request)
    {
        return File.ReadAllText($"{SchemaFolder}/{request.SchemaVersion}.json");
    }

    /// <inheritdoc />
    public IList<ValidationError> ValidateRequestAgainstJsonSchema(string jsonSchemaAsString, string inputJson)
    {
        var parsedSchema = JSchema.Parse(jsonSchemaAsString);
        var parsedBody = JObject.Parse(inputJson);

        IList<ValidationError> validationErrors = new List<ValidationError>();
        parsedBody.IsValid(parsedSchema, out validationErrors);

        return validationErrors;
    }

    /// <inheritdoc />
    public IList<SchemaDefinition> GetSchemas(HttpContext httpContext)
    {
        var availableModels = new List<SchemaDefinition>();

        string[] files = Directory.GetFiles(SchemaFolder, "*.json");

        foreach (string file in files)
        {
            var schemaVersion = Path.GetFileName(file).Replace(".json", string.Empty);
            var modelDefinition = new SchemaDefinition
            {
                SchemaVersion = schemaVersion,
                SchemaLocation = string.Format("{0}://{1}{2}", httpContext.Request.Scheme, httpContext.Request.Host, $"/v1/schemas/{schemaVersion}"),
            };

            availableModels.Add(modelDefinition);
        }

        return availableModels;
    }

    /// <inheritdoc />
    public JObject GetJsonSchemaByVersion(SchemaVersion schemaVersion)
    {
        var jsonSchemaAsString = File.ReadAllText($"{SchemaFolder}/{schemaVersion}.json");
        return JObject.Parse(jsonSchemaAsString);
    }
}