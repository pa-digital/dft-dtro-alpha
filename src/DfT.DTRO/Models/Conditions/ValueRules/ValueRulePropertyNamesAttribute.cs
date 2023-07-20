using System;
using System.Text.Json.Serialization;

namespace DfT.DTRO.Models.Conditions.ValueRules;

/// <summary>
/// An attribute that enables overriding default parameter names for fields and properties of type <see cref="IValueRule{T}"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class ValueRulePropertyNamesAttribute : JsonConverterAttribute
{
    private readonly string _operatorPropertyName;
    private readonly string _valuePropertyName;

    /// <summary>
    /// A constructor that allows overriding parameter names.
    /// </summary>
    /// <param name="OperatorPropertyName">The parameter name for the operator (<c>"operator"</c> by default)</param>
    /// <param name="ValuePropertyName">The parameter name for the value (<c>"value"</c> by default)</param>
    public ValueRulePropertyNamesAttribute(string OperatorPropertyName = null, string ValuePropertyName = null) : base()
    {
        _operatorPropertyName = OperatorPropertyName;
        _valuePropertyName = ValuePropertyName;
    }

    /// <summary>
    /// Creates the converter that handles parameter names in a way defined in this attribute.
    /// </summary>
    /// <param name="typeToConvert"></param>
    /// <returns>
    /// The converter.
    /// </returns>
    public override JsonConverter CreateConverter(Type typeToConvert)
        => new ValueRuleJsonConverterFactory(_operatorPropertyName, _valuePropertyName);
}
