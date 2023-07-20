using System.Dynamic;
using DfT.DTRO.Converters;
using DfT.DTRO.Models;
using Microsoft.EntityFrameworkCore;

namespace DfT.DTRO.DAL;

/// <summary>
/// Represents a session with the DTRO database.
/// </summary>
public partial class DtroContext : DbContext
{
    /// <summary>
    /// Used to query the DTRO table.
    /// </summary>
    public virtual DbSet<Models.DTRO> Dtros { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="DtroContext"/> using the specified options.
    /// </summary>
    /// <param name="options">The options for this context.</param>
    public DtroContext(DbContextOptions<DtroContext> options) : base(options)
    {

    }

    /// <inheritdoc/>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<SchemaVersion>()
            .HaveConversion<SchemaVersionValueConverter>();

        configurationBuilder
            .Properties<ExpandoObject>()
            .HaveColumnType("jsonb")
            .HaveConversion<ExpandoObjectValueConverter>();
    }
}
