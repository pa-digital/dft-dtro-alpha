using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DfT.DTRO.Models;

/// <summary>
/// Represents a change event recorded in the DTRO service.
/// </summary>
[DataContract]
public class DtroEvent
{
    /// <summary>
    /// Date D-TRO was published.
    /// </summary>
    [DataMember(Name = "publicationTime")]
    public DateTime PublicationTime { get; set; }

    /// <summary>
    /// The unique identifier of the highway authority that created the TRO.
    /// </summary>
    [DataMember(Name = "ha")]
    public int HighwayAuthorityId { get; set; }

    /// <summary>
    /// Published title of the Traffic Regulation Order.
    /// </summary>
    [DataMember(Name = "troName")]
    public string TroName { get; set; }

    /// <summary>
    /// Regulation type.
    /// </summary>
    [DataMember(Name = "regulationType")]
    public List<string> RegulationType { get; set; }

    /// <summary>
    /// Type of vehicle the restriction applies to.
    /// </summary>
    [DataMember(Name = "vehicleType")]
    public List<string> VehicleType { get; set; }

    /// <summary>
    /// Reporting point of the D-TRO.
    /// </summary>
    [DataMember(Name = "orderReportingPoint")]
    public List<string> OrderReportingPoint { get; set; }

    /// <summary>
    /// List of all regulation start dates specified in the DTRO.
    /// </summary>
    [DataMember(Name = "regulationStart")]
    public List<DateTime> RegulationStart { get; set; }

    /// <summary>
    /// List of all regulation end dates specified in the DTRO.
    /// </summary>
    [DataMember(Name = "regulationEnd")]
    public List<DateTime> RegulationEnd { get; set; }

    /// <summary>
    /// Type of the event.
    /// </summary>
    [DataMember(Name = "eventType")]
    public DtroEventType EventType { get; set; }

    /// <summary>
    /// Timestamp representing the time when the event occured.
    /// </summary>
    [DataMember(Name = "eventTime")]
    public DateTime EventTime { get; set; }

    /// <summary>
    /// Named URIs related to this event.
    /// </summary>
    [DataMember(Name = "_links")]
    public Links Links { get; set; }

}
