using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace DfT.DTRO.Models.Filtering;

/// <summary>
/// DTRO search criteria for a single query.
/// </summary>
public class SearchQuery
{
    /// <summary>
    /// Spatial search parameters.
    /// </summary>
    public Location Location { get; set; }

    /// <summary>
    /// Date D-TRO was published.
    /// </summary>
    [DataType(DataType.DateTime)]
    public DateTime? PublicationTime { get; set; }

    /// <summary>
    /// Highway Authority identifier.
    /// </summary>
    [DataMember(Name = "ha")]
    public int? Ha { get; set; }

    /// <summary>
    /// Published title of the Traffic Regulation Order.
    /// </summary>
    [MinLength(0)]
    [MaxLength(500)]
    public string TroName { get; set; }

    /// <summary>
    /// Regulation type, a value from `allRegulationTypes` list.
    /// </summary>
    [MinLength(0)]
    [MaxLength(60)]
    public string RegulationType { get; set; }

    /// <summary>
    /// Type of vehicle the restriction applies to.
    /// </summary>
    [MinLength(0)]
    [MaxLength(40)]
    public string VehicleType { get; set; }

    /// <summary>
    /// Reporting point of the D-TRO.
    /// </summary>
    [MinLength(0)]
    [MaxLength(50)]
    public string OrderReportingPoint { get; set; }

    /// <summary>
    /// Date and time when the regulation starts.
    /// </summary>
    public ValueCondition<DateTime> RegulationStart { get; set; }

    /// <summary>
    /// Date and time when the regulation ends.
    /// </summary>
    public ValueCondition<DateTime> RegulationEnd { get; set; }
}