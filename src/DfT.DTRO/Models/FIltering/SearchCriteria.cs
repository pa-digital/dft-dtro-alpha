using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DfT.DTRO.Models.Pagination;
using Newtonsoft.Json;

namespace DfT.DTRO.Models.Filtering;

/// <summary>
/// DTRO search criteria definition.
/// </summary>
public class SearchCriteria : PaginatedRequest
{
    /// <summary>
    /// List of search queries.
    /// </summary>
    [MinLength(1)]
    public IEnumerable<SearchQuery> Queries { get; set; }


    /// <summary>
    /// Returns a search criteria as a JSON string.
    /// </summary>
    /// <returns>Search criteria as a string.</returns>
    public string ToJsonString()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}