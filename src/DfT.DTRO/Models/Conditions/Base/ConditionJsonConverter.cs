using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace DfT.DTRO.Models.Conditions.Base;

/// <summary>
/// Converts <see cref="Condition"/> from JSON.
/// </summary>
public class ConditionJsonConverter : JsonConverter<Condition>
{
    /// <inheritdoc/>
    public override Condition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var json = JsonSerializer.Deserialize<JsonNode>(ref reader, options);

        if (json is null)
        {
            return null;
        }

        if (json is not JsonObject jObject)
        {
            throw new InvalidOperationException("The Condition must be an object");
        }

        if (jObject.ContainsKey("conditions"))
        {
            return json.Deserialize<ConditionSet>(options);
        }

        if (jObject.ContainsKey("roadType"))
        {
            return json.Deserialize<RoadCondition>(options);
        }

        if (jObject.ContainsKey("numbersOfOccupants") || jObject.ContainsKey("disabledWithPermit"))
        {
            return json.Deserialize<OccupantCondition>(options);
        }

        if (jObject.ContainsKey("driverCharacteristicsType")
            || jObject.ContainsKey("licenseCharacteristics")
            || jObject.ContainsKey("ageOfDriver")
            || jObject.ContainsKey("timeDriversLicenseHeld"))
        {
            return json.Deserialize<DriverCondition>(options);
        }

        if (jObject.ContainsKey("accessConditionType") || jObject.ContainsKey("otherAccessRestriction"))
        {
            return json.Deserialize<AccessCondition>(options);
        }

        if (jObject.ContainsKey("type"))
        {
            return json.Deserialize<PermitCondition>(options);
        }

        if (jObject.ContainsKey("vehicleCharacteristics"))
        {
            return json.Deserialize<VehicleCondition>(options);
        }

        throw new JsonException("Unknown condition type.");
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, Condition value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}