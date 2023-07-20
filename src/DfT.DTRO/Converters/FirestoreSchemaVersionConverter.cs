using System;
using DfT.DTRO.Models;
using Google.Cloud.Firestore;

namespace DfT.DTRO.Converters;

/// <summary>
/// Converts <see cref="SchemaVersion"/> instances to and from Firestore.
/// </summary>
public class FirestoreSchemaVersionConverter : IFirestoreConverter<SchemaVersion>
{
    /// <inheritdoc/>
    public SchemaVersion FromFirestore(object value)
    {
        if (value is not string stringValue)
        {
            throw new InvalidOperationException("The value must be a string.");
        }

        return new SchemaVersion(stringValue);
    }

    /// <inheritdoc/>
    public object ToFirestore(SchemaVersion value)
    {
        return value.ToString();
    }
}
