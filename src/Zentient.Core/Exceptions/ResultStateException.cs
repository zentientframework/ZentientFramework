// <copyright file="ResultStateException{T}.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

// Zentient.Exceptions/ResultStateException.cs
namespace Zentient.Exceptions
{
    using System;
    using System.Collections.Immutable;
    using Zentient.Errors;
    using Zentient.Validation;

    /// <summary>
    /// Thrown when a Result is accessed in an invalid state.
    /// Carries minimal diagnostic data: the expected value type and the error payload.
    /// </summary>
    public class ResultStateException : InvalidOperationException
    {
        /// <summary>
        /// Gets the type of the value represented by this instance.
        /// </summary>
        public Type ValueType { get; }

        /// <summary>
        /// Gets the collection of errors encountered during the operation.
        /// </summary>
        /// <remarks>The returned array is immutable and may be empty if no errors occurred. Each element
        /// provides details about a specific error.</remarks>
        public ImmutableArray<Error> Errors { get; }

        /// <summary>
        /// Initializes a new instance of the ResultStateException class with the specified value type and a collection
        /// of errors.
        /// </summary>
        /// <param name="valueType">The type of the value associated with the result that caused the exception. Cannot be null.</param>
        /// <param name="errors">An immutable array containing the errors that led to the exception. If the array is default, an empty array
        /// is used.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="valueType"/> is null.</exception>
        public ResultStateException(Type valueType, ImmutableArray<Error> errors)
            : base(CreateMessage(valueType, errors))
        {
            ValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
            Errors = errors.IsDefault ? ImmutableArray<Error>.Empty : errors;
        }

        /// <summary>
        /// Initializes a new instance of the ResultStateException class with the specified value type, error
        /// collection, and optional inner exception.
        /// </summary>
        /// <param name="valueType">The type of the value associated with the result that caused the exception. Cannot be null.</param>
        /// <param name="errors">An immutable array of Error objects representing the errors that led to this exception. If the array is
        /// default, it is treated as empty.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or null if no inner exception is specified.</param>
        /// <exception cref="ArgumentNullException">Thrown if the valueType parameter is null.</exception>
        public ResultStateException(Type valueType, ImmutableArray<Error> errors, Exception? innerException)
            : base(CreateMessage(valueType, errors), innerException)
        {
            ValueType = Guard.AgainstNull(valueType, nameof(valueType));
            Errors = errors.IsDefault ? ImmutableArray<Error>.Empty : errors;
        }

        private static string CreateMessage(Type valueType, ImmutableArray<Error> errors)
        {
            var typeName = valueType?.FullName ?? "Unknown";
            var count = errors.IsDefault ? 0 : errors.Length;
            return $"Cannot access Value of a failed Result<{typeName}>. Errors: {count}.";
        }
    }

    /// <summary>
    /// Generic convenience wrapper that does not capture the Result struct.
    /// </summary>
    /// <typeparam name="TValue">The type of the value associated with the result that caused the exception.</typeparam>
    public sealed class ResultStateException<TValue> : ResultStateException
    {
        /// <summary>
        /// Initializes a new instance of the ResultStateException class with the specified collection of errors.
        /// </summary>
        /// <param name="errors">An immutable array of Error objects that describes the errors associated with the exception. Cannot be
        /// empty.</param>
        public ResultStateException(ImmutableArray<Error> errors)
            : base(typeof(TValue), errors)
        { }

        /// <summary>
        /// Initializes a new instance of the ResultStateException class with the specified collection of errors and an
        /// optional inner exception.
        /// </summary>
        /// <param name="errors">An immutable array of Error objects that describes the errors associated with the exception. Cannot be
        /// empty.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or null if no inner exception is specified.</param>
        public ResultStateException(ImmutableArray<Error> errors, Exception? innerException)
            : base(typeof(TValue), errors, innerException)
        { }
    }
}