// <copyright file="Id{T}.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Core
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// A strongly-typed, phantom-typed identifier.
    /// Prevents "Primitive Obsession" bugs (e.g., passing UserId into OrderId).
    /// </summary>
    /// <typeparam name="T">The type of concept this ID identifies.</typeparam>
    [TypeConverter(typeof(Zentient.ComponentModel.IdTypeConverter<>))]
    [DebuggerDisplay("{Value}")]
    public readonly record struct Id<T> : IComparable<Id<T>>, IParsable<Id<T>>
    {
        /// <summary>The underlying string value of the identifier.</summary>
        public string Value { get; }

        /// <summary>
        /// Initializes a new instance of the Id class using the specified string value.
        /// </summary>
        /// <param name="value">The string value to assign to the identifier. Cannot be null, empty, or consist only of white-space
        /// characters.</param>
        internal Id(string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));
            Value = value;

        }

        /// <summary>Generates a new collision-resistant ID (UUIDv4 format).</summary>
        /// <returns>A new <see cref="Id{T}"/> instance with a unique identifier value.</returns>
        public static Id<T> New() => new(Guid.NewGuid().ToString("N"));

        /// <summary>
        /// Parses the specified string and returns an <see cref="Id{T}"/> instance that represents the value contained
        /// in the string.
        /// </summary>
        /// <param name="value">The string containing the ID to parse. Cannot be null or empty.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information, or <see langword="null"/> to use the
        /// current culture.</param>
        /// <returns>An <see cref="Id{T}"/> instance that corresponds to the parsed value.</returns>
        /// <exception cref="FormatException">Thrown when <paramref name="value"/> is not in a valid format for an ID of type <typeparamref name="T"/>.</exception>
        public static Id<T> Parse(string value, IFormatProvider? provider = null)
        {
            if (!TryParse(value, provider, out var result))
            {
                throw new FormatException($"Invalid ID format for type {typeof(T).Name}.");
            }

            return result;
        }

        /// <summary>
        /// Attempts to parse the specified string into an <see cref="Id{T}"/> instance.
        /// </summary>
        /// <remarks>If <paramref name="value"/> is null, empty, or consists only of whitespace, parsing fails
        /// and <paramref name="result"/> is set to its default value. Otherwise, the trimmed string is used to
        /// construct the identifier.</remarks>
        /// <param name="value">The string representation of the identifier to parse. Leading and trailing whitespace are ignored. Cannot be
        /// null, empty, or consist only of whitespace.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information. This parameter is currently ignored.</param>
        /// <param name="result">When this method returns, contains the parsed <see cref="Id{T}"/> value if parsing succeeded; otherwise, the
        /// default value.</param>
        /// <returns>true if the string was successfully parsed; otherwise, false.</returns>
        public static bool TryParse([NotNullWhen(true)] string? value, IFormatProvider? provider, out Id<T> result)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                result = default;
                return false;
            }
            // Normalization: Trim whitespace
            result = new Id<T>(value.Trim());
            return true;
        }

        /// <summary>
        /// Parses the specified character span and returns an <see cref="Id{T}"/> instance that represents the value
        /// contained in the input.
        /// </summary>
        /// <param name="value">A read-only span of characters containing the string representation of the identifier to parse.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information, or <see langword="null"/> to use the
        /// current culture.</param>
        /// <returns>An <see cref="Id{T}"/> instance equivalent to the value contained in <paramref name="value"/>.</returns>
        public static Id<T> Parse(ReadOnlySpan<char> value, IFormatProvider? provider) => Parse(value.ToString(), provider);

        /// <summary>
        /// Attempts to parse the specified character span into an <see cref="Id{T}"/> instance.
        /// </summary>
        /// <param name="value">A read-only span of characters that contains the value to parse.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information, or null to use the current culture.</param>
        /// <param name="result">When this method returns, contains the parsed <see cref="Id{T}"/> value if parsing succeeded; otherwise, the default
        /// value.</param>
        /// <returns>true if the span was parsed successfully; otherwise, false.</returns>
        public static bool TryParse(ReadOnlySpan<char> value, IFormatProvider? provider, out Id<T> result) => TryParse(value.ToString(), provider, out result);

        /// <summary>
        /// Returns the string representation of the current object.
        /// </summary>
        /// <returns>The value of the object as a string.</returns>
        public override string ToString() => Value;

        /// <inheritdoc/>
        public int CompareTo(Id<T> other) => string.Compare(Value, other.Value, StringComparison.Ordinal);

        /// <summary>
        /// Converts the specified <see cref="Id{T}"/> instance to its underlying string value.
        /// </summary>
        /// <param name="id">The <see cref="Id{T}"/> instance to convert to a string.</param>
        public static explicit operator string(Id<T> id) => id.Value;

        /// <summary>
        /// Converts a string value to an instance of the <see cref="Id{T}"/> type.
        /// </summary>
        /// <param name="value">The string value to convert. Cannot be <see langword="null"/>.</param>
        public static implicit operator Id<T>(string value) => new(value ?? throw new ArgumentNullException(nameof(value)));
    }
}
namespace Zentient.ComponentModel
{
    using System;
    using System.ComponentModel;
    using System.Globalization;

    using Zentient.Core;
}
