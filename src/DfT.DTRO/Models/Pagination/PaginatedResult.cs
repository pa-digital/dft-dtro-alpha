using System.Collections.Generic;

namespace DfT.DTRO.Models.Pagination;

/// <summary>
/// Result of a query with paginated data.
/// </summary>
public class PaginatedResult<T>
{
    /// <summary>
    /// Creates a paginated query result.
    /// </summary>
    /// <param name="results"></param>
    /// <param name="totalCount"></param>
    public PaginatedResult(IReadOnlyCollection<T> results, int totalCount)
    {
        Results = results;
        TotalCount = totalCount;
    }

    /// <summary>
    /// List of records.
    /// </summary>
    public IReadOnlyCollection<T> Results { get; set; }

    /// <summary>
    /// Total number of records.
    /// </summary>
    public int TotalCount { get; set; }
}