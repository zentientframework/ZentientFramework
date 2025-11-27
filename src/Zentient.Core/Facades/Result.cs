// <copyright file="Result.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Facades
{
    using System;
    using System.Collections.Immutable;
    using System.Runtime.CompilerServices;
    using Zentient.Errors;
    using Zentient.Primitives;

    /// <summary>
    /// Provides static factory methods for creating successful or failed result instances with associated values or
    /// errors.
    /// </summary>
    /// <remarks>Use the methods in this class to construct instances of <see cref="Result{T}"/> representing
    /// either successful operations with a value, or failed operations with one or more errors. These methods enforce
    /// that failed results must include at least one error, ensuring that error information is always available for
    /// failure cases.</remarks>
    public static class Results
    {
        /// <summary>
        /// Creates a successful result containing the specified value.
        /// </summary>
        /// <typeparam name="T">The type of the successful payload.</typeparam>
        /// <param name="value">The value to be encapsulated in the successful result.</param>
        /// <returns>A <see cref="Result{T}"/> representing a successful operation with the provided value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> Succeed<T>(T value) => Result<T>.Succeed(value);

        /// <summary>
        /// Creates a failed result containing the specified collection of errors.
        /// </summary>
        /// <typeparam name="T">The type of the successful payload.</typeparam>
        /// <param name="errors">An immutable array of <see cref="Error"/> instances representing the errors associated with the failed
        /// result. Must contain at least one element.</param>
        /// <returns>A <see cref="Result{T}"/> representing a failed operation with the provided errors.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="errors"/> is default or empty.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> Fail<T>(ImmutableArray<Error> errors) => Result<T>.Fail(errors);

        /// <summary>
        /// Creates a failed result containing the specified errors.
        /// </summary>
        /// <typeparam name="T">The type of the successful payload.</typeparam>
        /// <param name="errors">An array of <see cref="Error"/> objects that describe the reasons for failure. Must contain at least one
        /// element.</param>
        /// <returns>A <see cref="Result{T}"/> instance representing a failed operation with the provided errors.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="errors"/> is <see langword="null"/> or does not contain at least one element.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<T> Fail<T>(params Error[] errors) => Result<T>.Fail(errors);
    }
}
