using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Dynamic;
using System.Runtime.Serialization;
using DfT.DTRO.Attributes;
using DfT.DTRO.Converters;
using Google.Cloud.Firestore;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Swashbuckle.AspNetCore.Annotations;

namespace DfT.DTRO.Models;

/// <summary>
/// Wrapper for a DTRO submission.
/// </summary>
[DataContract]
[FirestoreData]
public class DTRO
{
    /// <summary>
    /// Id of the DTRO.
    /// </summary>
    [Key]
    [FirestoreDocumentId]
    [SwaggerSchema(ReadOnly = true)]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column(TypeName = "uuid")]
    public Guid Id { get; set; }
    
    /// <summary>
    /// The schema identifier of the DTRO data payload being submitted.
    /// </summary>
    /// <example>3.1.0</example>
    [Required(ErrorMessage = "schemaVersion field must be included")]
    [DataMember(Name="schemaVersion")]
    [FirestoreProperty(Name="schemaVersion", ConverterType = typeof(FirestoreSchemaVersionConverter))]
    [JsonConverter(typeof(SchemaVersionJsonConverter))]
    public SchemaVersion SchemaVersion { get; set; }

    /// <summary>
    /// Timestamp that represents the creation time of this document.
    /// </summary>
    [DataMember(Name = "created"), SaveOnce]
    [FirestoreProperty(Name = "created")]
    [SwaggerSchema(ReadOnly = true)]
    public DateTime? Created { get; set; }

    /// <summary>
    /// Timestamp that represents the last time this document was updated.
    /// </summary>
    [DataMember(Name = "lastUpdated")]
    [FirestoreProperty(Name = "lastUpdated")]
    [SwaggerSchema(ReadOnly = true)]
    public DateTime? LastUpdated { get; set; }

    /// <summary>
    /// Correlation ID of the request with which this DTRO was created.
    /// </summary>
    [DataMember(Name = "createdCorrelationId"), SaveOnce]
    [FirestoreProperty(Name = "createdCorrelationId")]
    [SwaggerSchema(ReadOnly = true)]
    public string CreatedCorrelationId { get; set; }

    /// <summary>
    /// Correlation ID of the request with which this DTRO was last updated.
    /// </summary>
    [DataMember(Name = "lastUpdatedCorrelationId")]
    [FirestoreProperty(Name = "lastUpdatedCorrelationId")]
    [SwaggerSchema(ReadOnly = true)]
    public string LastUpdatedCorrelationId { get; set; }

    /// <summary>
    /// A flag representing whether the DTRO has been deleted.
    /// </summary>
    [DataMember(Name = "deleted")]
    [FirestoreProperty(Name = "deleted")]
    [SwaggerSchema(ReadOnly = true)]
    public bool Deleted { get; set; } = false;

    /// <summary>
    /// Timestamp that represents when the DTRO was deleted.
    /// <br/><br/>
    /// <see langword="null"/> if <see cref="Deleted"/> is <see langword="false"/>.
    /// </summary>
    [DataMember(Name = "deletionTime")]
    [FirestoreProperty(Name = "deletionTime")]
    [SwaggerSchema(ReadOnly = true)]
    public DateTime? DeletionTime { get; set; }

    /// <summary>
    /// The DTRO data model being submitted.
    /// </summary>
    [Required(ErrorMessage = "data field must be included")]
    [DataMember(Name = "data")]
    [FirestoreProperty(Name = "data", ConverterType = typeof(FirestoreDtroConverter))]
    [JsonConverter(typeof(ExpandoObjectConverter))]
    public ExpandoObject Data { get; set; }

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
}
