namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Defines how <see cref="NumericToBooleanConverter"/> compares a numeric value with the threshold
/// passed in the converter parameter.
/// </summary>
public enum NumberComparison
{
    /// <summary>
    /// The value is not zero. This is the default comparison, and the threshold is ignored.
    /// </summary>
    NonZero,

    /// <summary>
    /// The value is equal to the threshold.
    /// </summary>
    Equal,

    /// <summary>
    /// The value is not equal to the threshold.
    /// </summary>
    NotEqual,

    /// <summary>
    /// The value is greater than the threshold.
    /// </summary>
    GreaterThan,

    /// <summary>
    /// The value is greater than or equal to the threshold.
    /// </summary>
    GreaterThanOrEqual,

    /// <summary>
    /// The value is less than the threshold.
    /// </summary>
    LessThan,

    /// <summary>
    /// The value is less than or equal to the threshold.
    /// </summary>
    LessThanOrEqual
}
