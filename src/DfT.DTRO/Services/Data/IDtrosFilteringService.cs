using System.Collections.Generic;
using DfT.DTRO.Models;
using DfT.DTRO.Models.Filtering;
using DfT.DTRO.Models.Pagination;

namespace DfT.DTRO.Services.Data;

/// <summary>
/// Service layer to handle filtering of DTROs.
/// </summary>
public interface IDtrosFilteringService
{
    /// <summary>
    /// Applies the requested search criteria to the provided list of DTROs.
    /// </summary>
    /// <param name="dtros">List of DTROs to filter</param>
    /// <param name="searchCriteria">Criteria used to filter the data</param>
    /// <returns>
    /// A paginated list of <see cref="DtroSearchResult"/> objects
    /// matching the criteria provided in <paramref name="searchCriteria"/>.
    /// </returns>
    PaginatedResponse<DtroSearchResult> Filter(IEnumerable<Models.DTRO> dtros, SearchCriteria searchCriteria);

    /// <summary>
    /// Applies the requested event search criteria to the provided list of DTROs.
    /// </summary>
    /// <param name="dtros">List of DTROs to filter</param>
    /// <param name="search">Criteria used to filter the data</param>
    /// <returns>
    /// A paginated list of <see cref="DtroEvent"/> objects
    /// matching the criteria provided in <paramref name="search"/>.
    /// </returns>
    DtroEventSearchResult Filter(IEnumerable<Models.DTRO> dtros, DtroEventSearch search);
}