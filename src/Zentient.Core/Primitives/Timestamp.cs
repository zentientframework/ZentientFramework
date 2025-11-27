// <copyright file="Timestamp.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Primitives
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Represents a high-precision point in time.
    /// Wraps UTC ticks; add arithmetic helpers for convenience.
    /// </summary>
    [DebuggerDisplay("{ToDateTimeOffset()}")]
    public readonly record struct Timestamp(long Ticks) : IComparable<Timestamp>
    {
        /// <summary>
        /// Gets a <see cref="Timestamp"/> instance representing the current UTC time.
        /// </summary>
        /// <remarks>The value is based on the current system clock in Coordinated Universal Time
        /// (UTC). The precision and accuracy depend on the underlying system timer.</remarks>
        public static Timestamp Now
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(DateTime.UtcNow.Ticks);
            }
        }

        /// <summary>
        /// Creates a new Timestamp instance that represents the same point in time as the specified DateTimeOffset,
        /// using its UTC value.
        /// </summary>
        /// <param name="dto">The DateTimeOffset value to convert. The UTC time of this value will be used to create the Timestamp.</param>
        /// <returns>A Timestamp instance corresponding to the UTC time of the specified DateTimeOffset.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Timestamp FromDateTimeOffset(DateTimeOffset dto) => new(dto.UtcDateTime.Ticks);

        /// <summary>
        /// Converts the current instance to a <see cref="DateTimeOffset"/> value using the stored ticks and a zero
        /// offset.
        /// </summary>
        /// <returns>A <see cref="DateTimeOffset"/> representing the same point in time as the current instance, with an
        /// offset of zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DateTimeOffset ToDateTimeOffset() => new(Ticks, TimeSpan.Zero);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(Timestamp other) => Ticks.CompareTo(other.Ticks);

        /// <summary>
        /// Returns a new Timestamp that adds the specified number of ticks to the current instance.
        /// </summary>
        /// <param name="ticks">The number of ticks to add to the current Timestamp. A tick typically represents 100 nanoseconds.</param>
        /// <returns>A new Timestamp that is the result of adding the specified number of ticks to this instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Timestamp AddTicks(long ticks) => new(Ticks + ticks);

        /// <summary>
        /// Calculates the time interval between this timestamp and another specified timestamp.
        /// </summary>
        /// <param name="other">The timestamp to subtract from this instance. Represents the point in time to compare against.</param>
        /// <returns>A TimeSpan representing the difference between this timestamp and <paramref name="other"/>. The value is
        /// positive if this timestamp is later than <paramref name="other"/>; otherwise, it is negative or zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TimeSpan Subtract(Timestamp other) => TimeSpan.FromTicks(Ticks - other.Ticks);

        /// <summary>
        /// Converts a <see cref="Timestamp"/> instance to a <see cref="DateTimeOffset"/> using an implicit
        /// conversion.
        /// </summary>
        /// <remarks>This operator enables seamless conversion from <see cref="Timestamp"/> to
        /// <see cref="DateTimeOffset"/> without requiring an explicit cast. The conversion preserves the date and
        /// time information represented by the <see cref="Timestamp"/>.</remarks>
        /// <param name="t">The <see cref="Timestamp"/> value to convert to a <see cref="DateTimeOffset"/>.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator DateTimeOffset(Timestamp t) => t.ToDateTimeOffset();

        /// <summary>
        /// Converts a <see cref="DateTimeOffset"/> value to a <see cref="Timestamp"/> instance.
        /// </summary>
        /// <param name="d">The <see cref="DateTimeOffset"/> value to convert to a <see cref="Timestamp"/>.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Timestamp(DateTimeOffset d) => FromDateTimeOffset(d);
    }
}
