using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NpgsqlTypes;

namespace DfT.DTRO.Models;

/// <summary>
/// Bounding box definition.
/// </summary>
/// <param name="SouthLatitude">The south latitude of the bounding box.</param>
/// <param name="NorthLatitude">The north latitude of the bounding box.</param>
/// <param name="WestLongitude">The west longitude of the bounding box.</param>
/// <param name="EastLongitude">The east longitude of the bounding box.</param>
public record struct BoundingBox(double WestLongitude, double SouthLatitude, double EastLongitude, double NorthLatitude)
{
    /// <summary>
    /// A bounding box that limits allowed coordinates for <c>crs == "osgb36Epsg27700"</c>
    /// <br/><br/>
    /// defined as <c>[-103976.3, -16703.87, 652897.98, 1199851.44]</c>.
    /// </summary>
    public static readonly BoundingBox ForOsgb36Epsg27700 = new(-103976.3, -16703.87, 652897.98, 1199851.44);

    /// <summary>
    /// A bounding box that limits allowed coordinates for <c>crs == "osgb36Epsg27700"</c>
    /// <br/><br/>
    /// defined as <c>[-7.5600, 49.9600, 1.7800, 60.8400]</c>.
    /// </summary>
    public static readonly BoundingBox ForWgs84Epsg4326 = new(-7.5600, 49.9600, 1.7800, 60.8400);

    /// <summary>
    /// Checks if the coordinates are within the bounding box.
    /// </summary>
    /// <param name="latitude">The latitude to verify.</param>
    /// <param name="longitude">The longitude to verify.</param>
    /// <returns>
    /// <see langword="true"/> if the coordinates are within the bounding box;
    /// otherwise <see langword="false"/>
    /// </returns>
    public bool Contains(double longitude, double latitude)
        => latitude >= SouthLatitude && latitude <= NorthLatitude &&
            longitude >= WestLongitude && longitude <= EastLongitude;

    /// <summary>
    /// Checks if the coordinates are within the bounding box.
    /// </summary>
    /// <param name="latitude">The latitude to verify.</param>
    /// <param name="longitude">The longitude to verify.</param>
    /// <param name="errors">
    /// A <see cref="BoundingBoxErrors"/> object that explains the errors.
    /// <br/><br/>
    /// <see langword="null"/> if there were no errors (the method returned <see langword="true"/>).
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the coordinates are within the bounding box;
    /// otherwise <see langword="false"/>
    /// </returns>
    public bool Contains(double longitude, double latitude, [NotNullWhen(false)] out BoundingBoxErrors errors)
    {
        string longitudeError = null, latitudeError = null;

        if (longitude <= WestLongitude)
        {
            longitudeError = $"{longitude} is below the minimum longitude of {WestLongitude}.";
        }
        else if (longitude >= EastLongitude)
        {
            longitudeError = $"{longitude} is above the maximum longitude of {EastLongitude}.";
        }

        if (latitude <= SouthLatitude)
        {
            latitudeError = $"{latitude} is below the minimum latitude of {SouthLatitude}.";
        }
        else if (latitude >= NorthLatitude)
        {
            latitudeError = $"{latitude} is above the maximum latitude of {NorthLatitude}.";
        }

        if (latitudeError is not null || longitudeError is not null)
        {
            errors = new()
            {
                LatitudeError = latitudeError,
                LongitudeError = longitudeError,
            };
            return false;
        }

        errors = null;
        return true;
    }

    /// <summary>
    /// Checks if the coordinates are within the bounding box.
    /// </summary>
    /// <param name="coordinates">The coordinates to verify.</param>
    /// <returns>
    /// <see langword="true"/> if the coordinates are within the bounding box;
    /// otherwise <see langword="false"/>
    /// </returns>
    public bool Contains(Coordinates coordinates)
        => Contains(coordinates.Longitude, coordinates.Latitude);

    /// <summary>
    /// Checks if the coordinates are within the bounding box.
    /// </summary>
    /// <param name="coordinates">The coordinates to verify.</param>
    /// <param name="errors">
    /// A <see cref="BoundingBoxErrors"/> object that explains the errors.
    /// <br/><br/>
    /// <see langword="null"/> if there were no errors (the method returned <see langword="true"/>).
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the coordinates are within the bounding box;
    /// otherwise <see langword="false"/>
    /// </returns>
    public bool Contains(Coordinates coordinates, [NotNullWhen(false)] out BoundingBoxErrors errors)
        => Contains(coordinates.Longitude, coordinates.Latitude, out errors);

    /// <summary>
    /// Creates a minimal <see cref="BoundingBox"/> that contains all of the provided coordinates.
    /// </summary>
    /// <param name="coordinates">The coordinates that should be contained within the bounding box.</param>
    /// <returns>A <see cref="BoundingBox"/> that contains all of provided coordinates.</returns>
    public static BoundingBox Wrapping(params Coordinates[] coordinates)
        => Wrapping(coordinates.AsEnumerable());

    /// <summary>
    /// Determines whether this instance has any overlapping area
    /// with the <see cref="BoundingBox"/> provided in the argument.
    /// </summary>
    /// <param name="other">The <see cref="BoundingBox"/> to check for overlap with.</param>
    /// <returns>
    /// <see langword="true"/> if the overlap exists; otherwise <see langword="false"/>.
    /// </returns>
    public bool Overlaps(BoundingBox other)
        => SouthLatitude <= other.NorthLatitude &&
            other.SouthLatitude <= NorthLatitude &&
            WestLongitude <= other.EastLongitude &&
            other.WestLongitude <= EastLongitude;

    /// <summary>
    /// Converts a <see cref="BoundingBox"/> to an <see cref="NpgsqlBox"/>.
    /// </summary>
    /// <param name="bbox"></param>
    public static implicit operator NpgsqlBox(BoundingBox bbox)
        => new(bbox.NorthLatitude, bbox.EastLongitude, bbox.SouthLatitude, bbox.WestLongitude);

    /// <summary>
    /// Creates a minimal <see cref="BoundingBox"/> that contains all of the provided coordinates.
    /// </summary>
    /// <param name="coordinates">The coordinates that should be contained within the bounding box.</param>
    /// <returns>A <see cref="BoundingBox"/> that contains all of provided coordinates.</returns>
    public static BoundingBox Wrapping(IEnumerable<Coordinates> coordinates)
    {
        var (east, south) = coordinates.First();
        var north = south;
        var west = east;

        foreach (var coord in coordinates.Skip(1))
        {
            if (coord.Latitude < south)
            {
                south = coord.Latitude;
            }
            else if (coord.Latitude > north)
            {
                north = coord.Latitude;
            }
            if (coord.Longitude < west)
            {
                west = coord.Longitude;
            }
            else if (coord.Longitude > east)
            {
                east = coord.Longitude;
            }
        }

        return new BoundingBox(west, south, east, north);
    }
}

/// <summary>
/// Data structure providing explanation for bounding box validation error.
/// </summary>
public class BoundingBoxErrors
{
    /// <summary>
    /// Longitude related error explanation
    /// </summary>
    public string LongitudeError;

    /// <summary>
    /// Latitude related error explanation
    /// </summary>
    public string LatitudeError;
}
