// <copyright file="Guard.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Provides compact, DX-first guard helpers used across the core library.
    /// Methods are optimized for common call-sites: they avoid allocations on the fast path,
    /// return normalized values where appropriate (trimmed strings), and supply clear exceptions
    /// with the caller parameter name via <see cref="CallerArgumentExpressionAttribute"/>.
    /// </summary>
    public static class Guard
    {
        // ---------------------------------------------------------------------
        // String guards (DX-first: return normalized/trimmed values)
        // ---------------------------------------------------------------------

        /// <summary>
        /// Ensures the provided string is not <see langword="null"/> and returns a trimmed value.
        /// Trimming is performed only when necessary to avoid allocations on the hot path.
        /// </summary>
        /// <param name="value">Input string to validate. May be <see langword="null"/>.</param>
        /// <param name="parameterName">Automatically captured caller expression representing the <paramref name="value"/> parameter.</param>
        /// <returns>The trimmed input string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string AgainstNull(string? value, [CallerArgumentExpression("value")] string? parameterName = null)
        {
            var name = parameterName ?? nameof(value);
            ArgumentNullException.ThrowIfNull(value, name);

            var span = value.AsSpan();
            var len = span.Length;
            if (len == 0) return value; // preserve reference for empty string
            if (!char.IsWhiteSpace(span[0]) && !char.IsWhiteSpace(span[len - 1]))
            {
                // no trimming required
                return value;
            }

            // allocate only when necessary
            var trimmed = span.Trim();
            return trimmed.Length == len ? value : trimmed.ToString();
        }

        /// <summary>
        /// Ensures the provided string is not <see langword="null"/>, empty, or whitespace.
        /// Returns a trimmed string. Allocations are avoided when the input is already trimmed.
        /// </summary>
        /// <param name="value">Input string to validate. May be <see langword="null"/>.</param>
        /// <param name="parameterName">Automatically captured caller expression representing the <paramref name="value"/> parameter.</param>
        /// <returns>The trimmed input string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty or whitespace.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string AgainstNullOrWhitespace(string? value, [CallerArgumentExpression("value")] string? parameterName = null)
        {
            var name = parameterName ?? nameof(value);
            ArgumentNullException.ThrowIfNull(value, name);

            var span = value.AsSpan();
            var trimmed = span.Trim();

            if (trimmed.Length == 0)
            {
                throw new ArgumentException($"{name} cannot be empty or whitespace.", name);
            }

            return trimmed.Length == span.Length ? value : trimmed.ToString();
        }

        /// <summary>
        /// Try-style version that returns the trimmed value via an out parameter and a boolean result instead of throwing.
        /// Useful for hot paths where exceptions are undesirable.
        /// </summary>
        /// <param name="value">Input string to test.</param>
        /// <param name="trimmed">When true is returned, contains the trimmed value; otherwise <see langword="null"/>.</param>
        /// <returns>True when <paramref name="value"/> is non-null and contains non-whitespace characters; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAgainstNullOrWhitespace(string? value, out string? trimmed)
        {
            if (value is null) { trimmed = null; return false; }

            var span = value.AsSpan();
            var t = span.Trim();
            if (t.Length == 0) { trimmed = null; return false; }
            trimmed = t.Length == span.Length ? value : t.ToString();
            return true;
        }

        /// <summary>
        /// Validates that a string parameter is not empty or whitespace.
        /// If the incoming value is <see langword="null"/>, this method returns <see langword="null"/>.
        /// Returns the trimmed string when successful.
        /// </summary>
        /// <param name="value">The string value to validate. If <see langword="null"/>, no validation is performed and <see langword="null"/> is returned.</param>
        /// <param name="parameterName">Automatically captured caller expression representing the <paramref name="value"/> parameter.</param>
        /// <returns>The trimmed string or <see langword="null"/> when the input was <see langword="null"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> consists only of whitespace characters or is empty.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string? AgainstWhitespace(string? value, [CallerArgumentExpression("value")] string? parameterName = null)
        {
            var name = parameterName ?? nameof(value);
            if (value is null) return null;

            var span = value.AsSpan();
            var trimmed = span.Trim();
            if (trimmed.Length == 0) throw new ArgumentException($"{name} cannot be empty or whitespace.", name);
            return trimmed.Length == span.Length ? value : trimmed.ToString();
        }

        // ---------------------------------------------------------------------
        // Reference and value-type guards
        // ---------------------------------------------------------------------

        /// <summary>
        /// Throws <see cref="ArgumentNullException"/> when the supplied reference is <see langword="null"/>.
        /// Returns the original reference when successful.
        /// </summary>
        /// <typeparam name="T">Reference type.</typeparam>
        /// <param name="value">Value to validate.</param>
        /// <param name="parameterName">Automatically captured caller expression representing the <paramref name="value"/> parameter.</param>
        /// <returns>The supplied reference (not null).</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T AgainstNull<T>(T? value, [CallerArgumentExpression("value")] string? parameterName = null) where T : class
        {
            var name = parameterName ?? nameof(value);
            ArgumentNullException.ThrowIfNull(value, name);
            return value;
        }

        /// <summary>
        /// Ensures a value-type is not equal to its default value.
        /// Returns the value when successful.
        /// </summary>
        /// <typeparam name="T">Value type.</typeparam>
        /// <param name="value">Value to validate.</param>
        /// <param name="parameterName">Automatically captured caller expression representing the <paramref name="value"/> parameter.</param>
        /// <returns>The supplied value.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is equal to its default value.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T AgainstDefault<T>(T value, [CallerArgumentExpression("value")] string? parameterName = null)
            where T : struct
        {
            var name = parameterName ?? nameof(value);
            if (EqualityComparer<T>.Default.Equals(value, default)) throw new ArgumentException($"{name} must not be the default value.", name);
            return value;
        }

        // ---------------------------------------------------------------------
        // Collection guards
        // ---------------------------------------------------------------------

        /// <summary>
        /// Ensures the provided collection is not <see langword="null"/> and contains at least one element.
        /// Returns the collection when successful.
        /// </summary>
        /// <typeparam name="T">Element type.</typeparam>
        /// <param name="enumerable">Collection to validate.</param>
        /// <param name="parameterName">Automatically captured caller expression representing the <paramref name="enumerable"/> parameter.</param>
        /// <returns>The original collection instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="enumerable"/> is <see langword="null"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> AgainstNullOrEmpty<T>(IEnumerable<T>? enumerable, [CallerArgumentExpression("enumerable")] string? parameterName = null)
        {
            var name = parameterName ?? nameof(enumerable);
            if (enumerable is null) throw new ArgumentNullException(name, $"{name} must not be null.");

            // Prefer non-enumerating checks when available
            if (enumerable is ICollection<T> coll)
            {
                if (coll.Count == 0) throw new ArgumentException($"{name} must contain at least one element.", name);
                return enumerable;
            }

            if (enumerable.TryGetNonEnumeratedCount(out var nonEnumeratedCount))
            {
                if (nonEnumeratedCount == 0) throw new ArgumentException($"{name} must contain at least one element.", name);
                return enumerable;
            }

            using var e = enumerable.GetEnumerator();
            if (!e.MoveNext()) throw new ArgumentException($"{name} must contain at least one element.", name);

            return enumerable;
        }

        // ---------------------------------------------------------------------
        // Numeric and range guards
        // ---------------------------------------------------------------------

        /// <summary>
        /// Ensures integer value is not negative.
        /// Returns the value when successful.
        /// </summary>
        /// <param name="value">Value to validate.</param>
        /// <param name="parameterName">Automatically captured caller expression representing the <paramref name="value"/> parameter.</param>
        /// <returns>The supplied value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is negative.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int AgainstNegative(int value, [CallerArgumentExpression("value")] string? parameterName = null)
        {
            var name = parameterName ?? nameof(value);
            if (value < 0) throw new ArgumentOutOfRangeException(name, value, $"{name} must not be negative.");
            return value;
        }

        /// <summary>
        /// Ensures integer value is positive (greater than zero).
        /// Returns the value when successful.
        /// </summary>
        /// <param name="value">Value to validate.</param>
        /// <param name="parameterName">Automatically captured caller expression representing the <paramref name="value"/> parameter.</param>
        /// <returns>The supplied value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is zero or negative.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int AgainstNonPositive(int value, [CallerArgumentExpression("value")] string? parameterName = null)
        {
            var name = parameterName ?? nameof(value);
            if (value <= 0) throw new ArgumentOutOfRangeException(name, value, $"{name} must be greater than zero.");
            return value;
        }

        /// <summary>
        /// Ensures the provided <typeparamref name="T"/> value is within the inclusive range [min, max].
        /// Returns the value when successful.
        /// </summary>
        /// <typeparam name="T">Comparable type.</typeparam>
        /// <param name="value">Value to validate.</param>
        /// <param name="min">Minimum allowed value (inclusive).</param>
        /// <param name="max">Maximum allowed value (inclusive).</param>
        /// <param name="parameterName">Automatically captured caller expression representing the <paramref name="value"/> parameter.</param>
        /// <returns>The supplied value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is outside the range [min, max].</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T AgainstOutOfRange<T>(T value, T min, T max, [CallerArgumentExpression("value")] string? parameterName = null)
            where T : IComparable<T>
        {
            var name = parameterName ?? nameof(value);
            if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
            {
                throw new ArgumentOutOfRangeException(name, value, $"{name} must be in range [{min}, {max}].");
            }

            return value;
        }

        // ---------------------------------------------------------------------
        // Enum validation
        // ---------------------------------------------------------------------

        /// <summary>
        /// Validates that an enum value is defined for the given enum type.
        /// Returns the value when successful.
        /// </summary>
        /// <typeparam name="TEnum">Enum type.</typeparam>
        /// <param name="value">Enum value to validate.</param>
        /// <param name="parameterName">Automatically captured caller expression representing the <paramref name="value"/> parameter.</param>
        /// <returns>The supplied enum value.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is not defined in the enum type.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TEnum AgainstInvalidEnum<TEnum>(TEnum value, [CallerArgumentExpression("value")] string? parameterName = null)
            where TEnum : struct, Enum
        {
            var name = parameterName ?? nameof(value);
            if (!Enum.IsDefined(typeof(TEnum), value))
            {
                throw new ArgumentException($"{name} has an undefined value for enum '{typeof(TEnum).FullName}'.", name);
            }

            return value;
        }

        // ---------------------------------------------------------------------
        // Internal helpers (ThrowHelpers / small fast helpers)
        // ---------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgNull(string param) => throw new ArgumentNullException(param);
    }
}
