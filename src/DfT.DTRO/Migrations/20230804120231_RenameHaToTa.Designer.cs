// <auto-generated />
using System;
using System.Collections.Generic;
using DfT.DTRO.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using NpgsqlTypes;

#nullable disable

namespace DfT.DTRO.Migrations
{
    [DbContext(typeof(DtroContext))]
    [Migration("20230804120231_RenameHaToTa")]
    partial class RenameHaToTa
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.12")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("DfT.DTRO.Models.DTRO", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<DateTime?>("Created")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("CreatedCorrelationId")
                        .HasColumnType("text");

                    b.Property<string>("Data")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<bool>("Deleted")
                        .HasColumnType("boolean");

                    b.Property<DateTime?>("DeletionTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime?>("LastUpdated")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("LastUpdatedCorrelationId")
                        .HasColumnType("text");

                    b.Property<NpgsqlBox>("Location")
                        .HasColumnType("box");

                    b.Property<List<string>>("OrderReportingPoints")
                        .HasColumnType("text[]");

                    b.Property<DateTime?>("RegulationEnd")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime?>("RegulationStart")
                        .HasColumnType("timestamp with time zone");

                    b.Property<List<string>>("RegulationTypes")
                        .HasColumnType("text[]");

                    b.Property<string>("SchemaVersion")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("TrafficAuthorityId")
                        .HasColumnType("integer")
                        .HasColumnName("TA");

                    b.Property<string>("TroName")
                        .HasColumnType("text");

                    b.Property<List<string>>("VehicleTypes")
                        .HasColumnType("text[]");

                    b.HasKey("Id");

                    b.ToTable("Dtros");
                });
#pragma warning restore 612, 618
        }
    }
}
