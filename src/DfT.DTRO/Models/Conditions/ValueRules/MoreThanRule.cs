using System;

namespace DfT.DTRO.Models.Conditions.ValueRules;

/// <summary>
/// Represents a rule that checks subsequence in sort order against a value.
/// </summary>
public interface IMoreThanRule : IValueRule { }

/// <summary>
/// Represents a rule that checks subsequence in sort order against a value of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of parameter used in this rule.</typeparam>
/// <param name="Value">The value to check subsequence against.</param>
/// <param name="Inclusive">Whether the check should include <see cref="Value"/></param>
public readonly record struct MoreThanRule<T>(T Value, bool Inclusive) : IMoreThanRule, IValueRule<T> where T : IComparable<T>
{

    /// <inheritdoc/>
    public bool Apply(T value)
    {
        var comparison = value.CompareTo(Value);

        if (comparison > 0)
        {
            return true;
        }

        if (comparison == 0 && Inclusive)
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public bool Contradicts(IValueRule<T> other)
    {
        if (other is null)
        {
            return false;
        }

        if (other is EqualityRule<T> eq)
        {
            return !Apply(eq.Value);
        }

        if (other is LessThanRule<T> lessThan)
        {
            return !Apply(lessThan.Value);
        }

        if (other is AndRule<T> || other is OrRule<T>)
        {
            return other.Contradicts(this);
        }

        return false;
    }

    /// <inheritdoc/>
    public IValueRule<T> Inverted()
    {
        return new LessThanRule<T>(Value, !Inclusive);
    }


    /// <summary>
    /// Returns a string representation of this rule.
    /// </summary>
    /// <returns>A string representation of this rule</returns>
    public override string ToString()
    {
        return $">{(Inclusive ? "=" : "")}{Value}";
    }
}
