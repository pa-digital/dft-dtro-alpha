using System;
using System.Collections.Generic;

namespace DfT.DTRO.Models.Filtering;

/// <summary>
/// A structure for data extracted from a DTRO that will be included in response.
/// </summary>
public class DtroExtractedData
{
    /// <summary>
    /// Default constructor
    /// </summary>
    /// <param name="vehicleTypes"></param>
    /// <param name="regulationTypes"></param>
    /// <param name="orderReportingPoints"></param>
    /// <param name="periodStartDates"></param>
    /// <param name="periodEndDates"></param>
    public DtroExtractedData(
        IEnumerable<string> vehicleTypes,
        IEnumerable<string> regulationTypes,
        IEnumerable<string> orderReportingPoints,
        IEnumerable<DateTime> periodStartDates,
        IEnumerable<DateTime> periodEndDates
    )
    {
        VehicleTypes = vehicleTypes;
        RegulationTypes = regulationTypes;
        OrderReportingPoints = orderReportingPoints;
        PeriodStartDates = periodStartDates;
        PeriodEndDates = periodEndDates;
    }

    /// <summary>
    /// List of all vehicle types specified in the DTRO.
    /// </summary>
    public IEnumerable<string> VehicleTypes { get; }

    /// <summary>
    /// List of all regulation types specified in the DTRO.
    /// </summary>
    public IEnumerable<string> RegulationTypes { get; }

    /// <summary>
    /// List of all order reporting points specified in the DTRO.
    /// </summary>
    public IEnumerable<string> OrderReportingPoints { get; }

    /// <summary>
    /// List of all provision start dates specified in the DTRO.
    /// </summary>
    public IEnumerable<DateTime> PeriodStartDates { get; }

    /// <summary>
    /// List of all provision end dates specified in the DTRO.
    /// </summary>
    public IEnumerable<DateTime> PeriodEndDates { get; }
}