using System;
using DfT.DTRO.Services.Storage;

namespace DfT.DTRO.Attributes;

/// <summary>
/// Attribute that makes sure property only gets saved to
/// by <see cref="IStorageService"/> implementations
/// when the document is created.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class SaveOnceAttribute : Attribute
{
}
