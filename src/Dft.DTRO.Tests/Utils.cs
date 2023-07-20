using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using DfT.DTRO.Models;

namespace Dft.DTRO.Tests;
public static class Utils
{
    public static DfT.DTRO.Models.DTRO PrepareDtro(string jsonData, SchemaVersion? schemaVersion = null)
        => new()
        {
            SchemaVersion = schemaVersion ?? "3.1.2",
            Data = JsonConvert.DeserializeObject<ExpandoObject>(jsonData, new ExpandoObjectConverter())
        };
}
