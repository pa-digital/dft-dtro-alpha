﻿using DfT.DTRO.Models;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NpgsqlTypes;

namespace DfT.DTRO.Converters;

/// <summary>
/// Defines conversion between a <see cref="BoundingBox"/> structure and its <see cref="NpgsqlBox"/> representation in the database.
/// </summary>
public class BoundingBoxValueConverter : ValueConverter<BoundingBox, NpgsqlBox>
{
    /// <summary>
    /// The single constructor.
    /// </summary>
    public BoundingBoxValueConverter() : base(
        bbox => new NpgsqlBox(bbox.NorthLatitude, bbox.EastLongitude, bbox.SouthLatitude, bbox.WestLongitude),
        npgBox => new BoundingBox(npgBox.Left, npgBox.Bottom, npgBox.Right, npgBox.Top)
        )
    { }
}
