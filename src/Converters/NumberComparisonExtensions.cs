using System;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Applies <see cref="NumberComparison"/> to a numeric value and a threshold.
/// </summary>
internal static class NumberComparisonExtensions
{
    /// <summary>
    /// Evaluates the comparison. <see cref="NumberComparison.NonZero"/> ignores the threshold.
    /// </summary>
    /// <param name="comparison">The comparison to apply.</param>
    /// <param name="value">The value to compare.</param>
    /// <param name="threshold">
    /// The threshold, required by every comparison except <see cref="NumberComparison.NonZero"/>.
    /// </param>
    /// <returns><see langword="true"/> when the comparison succeeds.</returns>
    /// <exception cref="InvalidOperationException">The comparison requires a threshold and it is missing.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="comparison"/> is not a defined value.</exception>
    public static bool Evaluate(this NumberComparison comparison, decimal value, decimal? threshold)
    {
        if (comparison == NumberComparison.NonZero)
        {
            return value != 0m;
        }

        if (threshold is not { } limit)
        {
            throw new InvalidOperationException($"The '{comparison}' comparison requires a numeric converter parameter.");
        }

        return comparison switch
        {
            NumberComparison.Equal => value == limit,
            NumberComparison.NotEqual => value != limit,
            NumberComparison.GreaterThan => value > limit,
            NumberComparison.GreaterThanOrEqual => value >= limit,
            NumberComparison.LessThan => value < limit,
            NumberComparison.LessThanOrEqual => value <= limit,
            _ => throw new ArgumentOutOfRangeException(nameof(comparison), comparison, "Unknown number comparison.")
        };
    }
}
