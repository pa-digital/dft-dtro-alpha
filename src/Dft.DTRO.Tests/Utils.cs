using System.Dynamic;
using System.Text;
using DfT.DTRO.Models;
using DfT.DTRO.Services.Conversion;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Dft.DTRO.Tests;
public static class Utils
{
    public static DfT.DTRO.Models.DTRO PrepareDtro(string jsonData, SchemaVersion? schemaVersion = null)
        => new()
        {
            SchemaVersion = schemaVersion ?? "3.1.2",
            Data = JsonConvert.DeserializeObject<ExpandoObject>(jsonData, new ExpandoObjectConverter())
        };

    public static async Task<StringContent> CreateDtroJsonPayload(string dtroJsonPath, string schemaVersion,
        bool inferIndexFields = true)
    {
        string sampleDtroDataJson = await File.ReadAllTextAsync(dtroJsonPath);

        ExpandoObject? dtroData =
            JsonConvert.DeserializeObject<ExpandoObject>(sampleDtroDataJson, new ExpandoObjectConverter());
        DfT.DTRO.Models.DTRO dtro = new() { SchemaVersion = schemaVersion, Data = dtroData };
        if (inferIndexFields)
        {
            dtro.InferIndexFields(new Proj4SpatialProjectionService());
        }

        string payload = JsonConvert.SerializeObject(dtro);

        return new StringContent(payload, Encoding.UTF8, "application/json");
    }

    public static async Task<DfT.DTRO.Models.DTRO> CreateDtroObject(string dtroJsonPath)
    {
        string sampleDtroDataJson = await File.ReadAllTextAsync(dtroJsonPath);
        DateTime createdAt = DateTime.Now;

        ExpandoObject? dtroData =
            JsonConvert.DeserializeObject<ExpandoObject>(sampleDtroDataJson, new ExpandoObjectConverter());
        DfT.DTRO.Models.DTRO sampleDtro = new()
        {
            Id = Guid.NewGuid(), Created = createdAt, LastUpdated = createdAt, Data = dtroData
        };
        sampleDtro.InferIndexFields(new Proj4SpatialProjectionService());

        return sampleDtro;
    }
}
