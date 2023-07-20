using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DfT.DTRO.Models.Conditions.ValueRules;

/// <summary>
/// Produces instances of <see cref="ValueRuleJsonConverter{T}"/>
/// where the generic type argument matches the generic type argument of <see cref="IValueRule{T}"/>
/// that is supposed to be converted.
/// </summary>
public class ValueRuleJsonConverterFactory : JsonConverterFactory
{
    private static readonly Type baseConverterType = typeof(ValueRuleJsonConverter<>);
    private static readonly Type baseTargetType = typeof(IValueRule<>);

    private readonly string _operatorPropertyName;
    private readonly string _valuePropertyName;

    /// <summary>
    /// A constructor that allows overriding parameter names.
    /// </summary>
    /// <param name="OperatorPropertyName">The parameter name for the operator (<c>"operator"</c> by default)</param>
    /// <param name="ValuePropertyName">The parameter name for the value (<c>"value"</c> by default)</param>
    public ValueRuleJsonConverterFactory(string OperatorPropertyName = null, string ValuePropertyName = null)
    {
        _operatorPropertyName = OperatorPropertyName ?? "operator";
        _valuePropertyName = ValuePropertyName ?? "value";
    }

    /// <summary>
    /// The default constructor.
    /// </summary>
    public ValueRuleJsonConverterFactory() : this(null, null)
    {

    }

    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.GetGenericTypeDefinition() == baseTargetType;
    }

    /// <inheritdoc/>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converter = baseConverterType.MakeGenericType(typeToConvert.GetGenericArguments().First());

        return Activator.CreateInstance(converter, _operatorPropertyName, _valuePropertyName) as JsonConverter;
    }
}
