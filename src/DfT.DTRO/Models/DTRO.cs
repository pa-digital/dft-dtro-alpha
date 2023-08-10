using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Dynamic;
using System.Linq;
using System.Runtime.Serialization;
using DfT.DTRO.Attributes;
using DfT.DTRO.Converters;
using DfT.DTRO.Extensions;
using DfT.DTRO.Services.Conversion;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Swashbuckle.AspNetCore.Annotations;

namespace DfT.DTRO.Models;

/// <summary>
/// Wrapper for a DTRO submission.
/// </summary>
[DataContract]
public class DTRO
{
    /// <summary>
    /// Id of the DTRO.
    /// </summary>
    [Key]
    [SwaggerSchema(ReadOnly = true)]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column(TypeName = "uuid")]
    public Guid Id { get; set; }

    /// <summary>
    /// The schema identifier of the DTRO data payload being submitted.
    /// </summary>
    /// <example>3.1.1.</example>
    [Required(ErrorMessage = "schemaVersion field must be included")]
    [DataMember(Name = "schemaVersion")]
    [JsonConverter(typeof(SchemaVersionJsonConverter))]
    public SchemaVersion SchemaVersion { get; set; }

    /// <summary>
    /// Timestamp that represents the creation time of this document.
    /// </summary>
    [DataMember(Name = "created")]
    [SaveOnce]
    [SwaggerSchema(ReadOnly = true)]
    public DateTime? Created { get; set; }

    /// <summary>
    /// Timestamp that represents the last time this document was updated.
    /// </summary>
    [DataMember(Name = "lastUpdated")]
    [SwaggerSchema(ReadOnly = true)]
    public DateTime? LastUpdated { get; set; }

    /// <summary>
    /// The earliest of regulation start dates.
    /// </summary>
    public DateTime? RegulationStart { get; set; }

    /// <summary>
    /// The latest of regulation end dates.
    /// </summary>
    public DateTime? RegulationEnd { get; set; }

    /// <summary>
    /// The unique identifier of the traffic authority creating the DTRO.
    /// </summary>
    [DataMember(Name = "ta")]
    [Column("TA")]
    public int TrafficAuthorityId { get; set; }

    /// <summary>
    /// The descriptive name of the DTRO.
    /// </summary>
    public string TroName { get; set; }

    /// <summary>
    /// Correlation ID of the request with which this DTRO was created.
    /// </summary>
    [DataMember(Name = "createdCorrelationId")]
    [SaveOnce]
    [SwaggerSchema(ReadOnly = true)]
    public string CreatedCorrelationId { get; set; }

    /// <summary>
    /// Correlation ID of the request with which this DTRO was last updated.
    /// </summary>
    [DataMember(Name = "lastUpdatedCorrelationId")]
    [SwaggerSchema(ReadOnly = true)]
    public string LastUpdatedCorrelationId { get; set; }

    /// <summary>
    /// A flag representing whether the DTRO has been deleted.
    /// </summary>
    [DataMember(Name = "deleted")]
    [SwaggerSchema(ReadOnly = true)]
    public bool Deleted { get; set; } = false;

    /// <summary>
    /// Timestamp that represents when the DTRO was deleted.
    /// <br/><br/>
    /// <see langword="null"/> if <see cref="Deleted"/> is <see langword="false"/>.
    /// </summary>
    [DataMember(Name = "deletionTime")]
    [SwaggerSchema(ReadOnly = true)]
    public DateTime? DeletionTime { get; set; }

    /// <summary>
    /// The DTRO data model being submitted.
    /// </summary>
    [Required(ErrorMessage = "data field must be included")]
    [DataMember(Name = "data")]
    [JsonConverter(typeof(ExpandoObjectConverter))]
    public ExpandoObject Data { get; set; }

    /// <summary>
    /// Unique regulation types that this DTRO consists of.
    /// </summary>
    public List<string> RegulationTypes { get; set; }

    /// <summary>
    /// Unique vehicle types that this DTRO applies to.
    /// </summary>
    public List<string> VehicleTypes { get; set; }

    /// <summary>
    /// Unique order reporting points that this DTRO applies to.
    /// </summary>
    public List<string> OrderReportingPoints { get; set; }

    /// <summary>
    /// The bounding box containing all points from this DTRO's regulated places.
    /// </summary>
    public BoundingBox Location { get; set; }

    /// <summary>
    /// Returns a DTRO as a JSON string.
    /// </summary>
    /// <returns>DTRO as a string.</returns>
    public string ToJsonString()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented).ToString();
    }

    /// <summary>
    /// Returns the inner body DTRO as a JSON string.
    /// </summary>
    /// <returns>DTRO payload as a string.</returns>
    public string DtroDataToJsonString()
    {
        return JsonConvert.SerializeObject(this.Data, Formatting.Indented).ToString();
    }

    /// <summary>
    /// Infers the fields that are not directly sent in the request
    /// but are used in the database for search optimization.
    /// </summary>
    public void InferIndexFields(ISpatialProjectionService projectionService)
    {
        var regulations = Data.GetValueOrDefault<IList<object>>("source.provision")
            .OfType<ExpandoObject>()
            .SelectMany(it => it.GetValue<IList<object>>("regulations").OfType<ExpandoObject>())
            .ToList();

        TrafficAuthorityId = Data.GetExpando("source").HasField("ta")
            ? Data.GetValueOrDefault<int>("source.ta")
            : Data.GetValueOrDefault<int>("source.ha");
        TroName = Data.GetValueOrDefault<string>("source.troName");
        RegulationTypes = regulations.Select(it => it.GetValueOrDefault<string>("regulationType"))
            .Where(it => it is not null)
            .Distinct()
            .ToList();

        VehicleTypes = regulations.SelectMany(it => it.GetListOrDefault("conditions") ?? Enumerable.Empty<object>())
            .Where(it => it is not null)
            .OfType<ExpandoObject>()
            .Select(it => it.GetExpandoOrDefault("vehicleCharacteristics"))
            .Where(it => it is not null)
            .SelectMany(it => it.GetListOrDefault("vehicleType") ?? Enumerable.Empty<object>())
            .OfType<string>()
            .Distinct()
            .ToList();

        OrderReportingPoints = Data.GetValueOrDefault<IList<object>>("source.provision")
            .OfType<ExpandoObject>()
            .Select(it => it.GetValue<string>("orderReportingPoint"))
            .Distinct()
            .ToList();

        RegulationStart = regulations.Select(it => it.GetExpando("overallPeriod").GetDateTimeOrNull("start"))
            .Where(it => it is not null)
            .Min();
        RegulationEnd = regulations.Select(it => it.GetExpando("overallPeriod").GetDateTimeOrNull("end"))
            .Where(it => it is not null)
            .Max();

        IEnumerable<Coordinates> FlattenAndConvertCoordinates(ExpandoObject coordinates, string crs)
        {
            var type = coordinates.GetValue<string>("type");
            var coords = coordinates.GetValue<IList<object>>("coordinates");

            var result = type switch
            {
                "Polygon" => coords.OfType<IList<object>>().SelectMany(it => it).OfType<IList<object>>()
                    .Select(it => new Coordinates((double)it[0], (double)it[1])),
                "LineString" => coords.OfType<IList<object>>()
                    .Select(it => new Coordinates((double)it[0], (double)it[1])),
                "Point" => new List<Coordinates> { new ((double)coords[0], (double)coords[1]) },
                _ => throw new InvalidOperationException($"Coordinate type '{type}' unsupported.")
            };

            return crs != "osgb36Epsg27700"
                ? result.Select(coords => projectionService.Wgs84ToOsgb36(coords))
                : result;
        }

        var coordinates = Data.GetValueOrDefault<IList<object>>("source.provision").OfType<ExpandoObject>()
            .SelectMany(it => it.GetList("regulatedPlaces").OfType<ExpandoObject>())
            .SelectMany(it => FlattenAndConvertCoordinates(
                it.GetExpando("geometry").GetExpando("coordinates"),
                it.GetExpando("geometry").GetValue<string>("crs")));

        Location = BoundingBox.Wrapping(coordinates);
    }
}