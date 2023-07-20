namespace DfT.DTRO.Models;

/// <summary>
/// Definition of a model which combines version name and location of schema definition.
/// </summary>
public class SchemaDefinition
{
    /// <summary>
    /// The version number of the DTRO model.
    /// </summary>
    public string SchemaVersion { get; set; }

    /// <summary>
    /// The URI at which the model schema can be found.
    /// </summary>
    public string SchemaLocation { get; set; }
}