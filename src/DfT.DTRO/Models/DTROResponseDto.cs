using System.Dynamic;
using System.Runtime.Serialization;
using DfT.DTRO.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DfT.DTRO.Models;

/// <summary>
/// Response DTO of the DTRO data.
/// </summary>
[DataContract]
public class DTROResponseDto
{
    /// <summary>
    /// The schema identifier of the DTRO data payload being submitted.
    /// </summary>
    /// <example>3.1.1</example>
    [DataMember(Name = "schemaVersion")]
    [JsonConverter(typeof(SchemaVersionJsonConverter))]
    public SchemaVersion SchemaVersion { get; set; }

    /// <summary>
    /// The DTRO data model being submitted.
    /// </summary>
    [DataMember(Name = "data")]
    [JsonConverter(typeof(ExpandoObjectConverter))]
    public ExpandoObject Data { get; set; }

    /// <summary>
    /// Creates a new <see cref="DTROResponseDto"/>
    /// based on the <see cref="DTRO"/> provided in <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The <see cref="DTRO"/> to base the new <see cref="DTROResponseDto"/> on.</param>
    /// <returns>The created <see cref="DTROResponseDto"/>.</returns>
    public static DTROResponseDto FromDTRO(DTRO source) => new()
    {
        SchemaVersion = source.SchemaVersion,
        Data = source.Data,
    };
}
