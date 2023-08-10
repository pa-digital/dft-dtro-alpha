using System;

namespace DfT.DTRO.Models;

/// <summary>
/// A confirmation of storage paired with an accompanying ID.
/// </summary>
public class DTROResponse
{
    /// <summary>
    /// The unique identifier of the DTRO record stored centrally.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Default constructor.
    /// </summary>
    public DTROResponse()
    {
        Id = Guid.NewGuid();
    }
}