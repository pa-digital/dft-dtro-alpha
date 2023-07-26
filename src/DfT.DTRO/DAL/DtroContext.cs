using System;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using DfT.DTRO.Converters;
using DfT.DTRO.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;
using NpgsqlTypes;

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
    [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.",
        Justification = "Usage of this API is the easiest workaround for the time being.")]
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDbFunction(typeof(DatabaseMethods).GetMethod(nameof(DatabaseMethods.Overlaps), new[] { typeof(NpgsqlBox), typeof(NpgsqlBox) }))
            .HasTranslation(args =>
            {
                return new PostgresBinaryExpression(
                    PostgresExpressionType.Overlaps, args[0], args[1], args[0].Type, args[0].TypeMapping);
            });
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

        configurationBuilder
            .Properties<BoundingBox>()
            .HaveConversion<BoundingBoxValueConverter>();
    }
}
