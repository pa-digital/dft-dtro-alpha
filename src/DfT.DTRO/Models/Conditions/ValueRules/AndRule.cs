using System;

namespace DfT.DTRO.Models.Conditions.ValueRules;

/// <summary>
/// Represents a rule that is a conjunction of two other rules.
/// </summary>
public interface IAndRule : IValueRule
{
    /// <summary>
    /// The first rule in the conjunction.
    /// </summary>
    IValueRule First { get; }
    /// <summary>
    /// The second rule in the conjunction.
    /// </summary>
    IValueRule Second { get; }
}

/// <summary>
/// Represents a rule that is a conjunction of two other rules.
/// </summary>
/// <typeparam name="T">The type of parameter used in this rule.</typeparam>
/// <param name="First">The first rule in the conjunction.</param>
/// <param name="Second">The second rule in the conjunction.</param>
public readonly record struct AndRule<T>(IValueRule<T> First, IValueRule<T> Second) : IAndRule, IValueRule<T> where T : IComparable<T>
{
    IValueRule IAndRule.First => First;

    IValueRule IAndRule.Second => Second;

    /// <inheritdoc/>
    public bool Apply(T value)
    {
        return First.Apply(value) && Second.Apply(value);
    }

    /// <inheritdoc/>
    public bool Contradicts(IValueRule<T> other)
    {
        if (other is null)
        {
            return false;
        }

        return other.Contradicts(First) || other.Contradicts(Second);
    }

    /// <inheritdoc/>
    public IValueRule<T> Inverted()
    {
        return new OrRule<T>(First.Inverted(), Second.Inverted());
    }

    /// <summary>
    /// Returns a string representation of this rule.
    /// </summary>
    /// <returns>A string representation of this rule</returns>
    public override string ToString()
    {
        return $"({First} && {Second})";
    }
}
