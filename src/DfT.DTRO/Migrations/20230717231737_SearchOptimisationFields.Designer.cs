﻿// <auto-generated />
using System;
using DfT.DTRO.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DfT.DTRO.Migrations
{
    [DbContext(typeof(DtroContext))]
    [Migration("20230717231737_SearchOptimisationFields")]
    partial class SearchOptimisationFields
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.19")
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

                    b.Property<int>("HighwayAuthorityId")
                        .HasColumnType("integer")
                        .HasColumnName("HA");

                    b.Property<DateTime?>("LastUpdated")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("LastUpdatedCorrelationId")
                        .HasColumnType("text");

                    b.Property<string>("SchemaVersion")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("TroName")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Dtros");
                });
#pragma warning restore 612, 618
        }
    }
}
