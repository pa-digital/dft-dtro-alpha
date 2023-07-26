using System.Collections.Generic;
using DfT.DTRO.Models;

namespace DfT.DTRO.Services.Data;

public interface IDtroMappingService
{
    IEnumerable<DtroEvent> MapToEvents(IEnumerable<Models.DTRO> dtros);
    IEnumerable<DtroSearchResult> MapToSearchResult(IEnumerable<Models.DTRO> dtros);
}
