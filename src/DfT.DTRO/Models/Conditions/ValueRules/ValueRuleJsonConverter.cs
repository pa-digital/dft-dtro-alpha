using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace DfT.DTRO.Models.Conditions.ValueRules;

/// <summary>
/// Supports converting JSON to <see cref="IValueRule{T}"/> with <typeparamref name="T"/> as the type used by the rule.
/// </summary>
/// <typeparam name="T">The generic type that will be used in the <see cref="IValueRule{T}"/>.</typeparam>
public class ValueRuleJsonConverter<T> : JsonConverter<IValueRule<T>> where T : IComparable<T>
{
    private readonly string _operatorPropertyName;
    private readonly string _valuePropertyName;

    /// <summary>
    /// A constructor that allows overriding parameter names.
    /// </summary>
    /// <param name="OperatorPropertyName">The parameter name for the operator (<c>"operator"</c> by default)</param>
    /// <param name="ValuePropertyName">The parameter name for the value (<c>"value"</c> by default)</param>
    public ValueRuleJsonConverter(string OperatorPropertyName = null, string ValuePropertyName = null)
    {
        _operatorPropertyName = OperatorPropertyName ?? "operator";
        _valuePropertyName = ValuePropertyName ?? "value";
    }

    /// <summary>
    /// The default constructor.
    /// </summary>
    public ValueRuleJsonConverter() : this(null, null) { }

    /// <inheritdoc/>
    public override IValueRule<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var array = JsonSerializer.Deserialize<JsonArray>(ref reader, options);

        var result = array.OfType<JsonObject>().Select(it => ToValueRule(it)).ToList();

        if (result.Count == 1)
        {
            return result.Single();
        }

        if (result.Count == 2)
        {
            return new AndRule<T>(result[0], result[1]);
        }

        throw new JsonException();
    }

    private IValueRule<T> ToValueRule(JsonObject jObject)
    {
        if (!jObject.TryGetPropertyValue(_operatorPropertyName, out var op))
        {
            throw new JsonException($"'{_operatorPropertyName}' is required.");
        }

        if (!jObject.TryGetPropertyValue(_valuePropertyName, out var value))
        {
            throw new JsonException($"'{_valuePropertyName}' is required.");
        }

        var convertedValue = value.Deserialize<T>();

        var opString = op.GetValue<string>();

        var inclusive = opString.ToLower().EndsWith("orequalto");

        if (opString.ToLower().StartsWith("equalto"))
        {
            return new EqualityRule<T>(convertedValue);
        }

        if (opString.ToLower().StartsWith("greaterthan"))
        {
            return new MoreThanRule<T>(convertedValue, inclusive);
        }

        if (opString.ToLower().StartsWith("lessthan"))
        {
            return new LessThanRule<T>(convertedValue, inclusive);
        }

        throw new JsonException($"The '{_operatorPropertyName}' value was not one of known operators.");
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, IValueRule<T> value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
