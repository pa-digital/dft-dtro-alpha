using System.Collections.Generic;
using DfT.DTRO.Models;

namespace DfT.DTRO.Services.Data;

/// <summary>
/// Provides methods used for mapping <see cref="Models.DTRO"/> to other types.
/// </summary>
public interface IDtroMappingService
{
    /// <summary>
    /// Infers the fields that are not directly sent in the request
    /// but are used in the database for search optimization.
    /// </summary>
    /// <param name="dtro">The <see cref="Models.DTRO"/> to infer index fields for.</param>
    void InferIndexFields(Models.DTRO dtro);

    IEnumerable<DtroEvent> MapToEvents(IEnumerable<Models.DTRO> dtros);

    IEnumerable<DtroSearchResult> MapToSearchResult(IEnumerable<Models.DTRO> dtros);
}
