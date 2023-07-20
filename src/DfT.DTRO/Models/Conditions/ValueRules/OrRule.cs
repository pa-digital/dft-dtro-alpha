using System;

namespace DfT.DTRO.Models.Conditions.ValueRules;

/// <summary>
/// Represents a rule that is a disjunction of two other rules.
/// </summary>
/// <typeparam name="T">The type of parameter used in this rule.</typeparam>
/// <param name="First">The first rule in the disjunction.</param>
/// <param name="Second">The second rule in the disjunction.</param>
public readonly record struct OrRule<T>(IValueRule<T> First, IValueRule<T> Second) : IValueRule<T> where T : IComparable<T>
{

    /// <inheritdoc/>
    public bool Apply(T value)
    {
        return First.Apply(value) || Second.Apply(value);
    }

    /// <inheritdoc/>
    public bool Contradicts(IValueRule<T> other)
    {
        return other.Contradicts(First) && other.Contradicts(Second);
    }

    /// <inheritdoc/>
    public IValueRule<T> Inverted()
    {
        return new AndRule<T>(First.Inverted(), Second.Inverted());
    }

    /// <summary>
    /// Returns a string representation of this rule.
    /// </summary>
    /// <returns>A string representation of this rule</returns>
    public override string ToString()
    {
        return $"({First} || {Second})";
    }
}
