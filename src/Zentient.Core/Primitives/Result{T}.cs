// <copyright file="Result{T}.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Primitives
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Zentient.Errors;
    using Zentient.Exceptions;
    using Zentient.Validation;

    /// <summary>
    /// High-performance discriminated result type carrying either a success value of type <typeparamref name="TValue"/>
    /// or an <see cref="Error"/>. This struct is immutable, ensures minimal allocations on the success path,
    /// and simplifies functional programming patterns like Map and Bind.
    /// </summary>
    /// <typeparam name="TValue">Type of the successful payload.</typeparam>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public readonly struct Result<TValue>
    {
        private readonly Tag _tag;
        private readonly TValue _value;
        private readonly ImmutableArray<Error> _errors;

        private enum Tag : byte { None = 0, Success = 1, Failure = 2 }

        /// <summary>
        /// Initializes a new instance of the Result class that represents a successful result with the specified value.
        /// </summary>
        /// <param name="value">The value to associate with the successful result. This parameter cannot be null.</param>
        internal Result(TValue value)
        {
            _value = value;
            _errors = ImmutableArray<Error>.Empty;
            _tag = Tag.Success;
        }

        /// <summary>
        /// Initializes a new instance of the Result class that represents a failure with the specified collection of
        /// errors.
        /// </summary>
        /// <param name="errors">An immutable array containing the errors that describe the failure. Cannot be null.</param>
        internal Result(ImmutableArray<Error> errors)
        {
            _value = default!;
            _errors =

                Guard.AgainstNull(errors, nameof(errors));
            _tag = Tag.Failure;
        }

        // ----------------------
        // Fast-path factories
        // ----------------------

        /// <summary>
        /// Creates a successful result containing the specified value.
        /// </summary>
        /// <param name="value">The value to be encapsulated in the successful result.</param>
        /// <returns>A <see cref="Result{T}"/> representing a successful operation with the provided value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<TValue> Succeed(TValue value) => new(value);

        /// <summary>
        /// Creates a failed result containing the specified collection of errors.
        /// </summary>
        /// <param name="errors">An immutable array of <see cref="Error"/> instances representing the errors associated with the failed
        /// result. Must contain at least one element.</param>
        /// <returns>A <see cref="Result{T}"/> representing a failed operation with the provided errors.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="errors"/> is default or empty.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<TValue> Fail(ImmutableArray<Error> errors)
        {
            Guard.AgainstNullOrEmpty(errors, nameof(errors));
            return new Result<TValue>(errors);
        }

        /// <summary>
        /// Creates a failed result containing the specified errors.
        /// </summary>
        /// <param name="errors">An array of <see cref="Error"/> objects that describe the reasons for failure. Must contain at least one
        /// element.</param>
        /// <returns>A <see cref="Result{T}"/> instance representing a failed operation with the provided errors.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="errors"/> is <see langword="null"/> or does not contain at least one element.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<TValue> Fail(params Error[] errors)
        {
            if (errors is null || errors.Length == 0) throw new ArgumentException("errors must contain at least one Error", nameof(errors));
            return new Result<TValue>(ImmutableArray.CreateRange(errors));
        }

        // ----------------------
        // Properties
        // ----------------------

        /// <summary>True when the result represents success.</summary>
        [MemberNotNullWhen(true, nameof(Value))]
        [MemberNotNullWhen(false, nameof(Errors))]
        public bool IsSuccess
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _tag == Tag.Success;
            }
        }

        /// <summary>True when the result represents failure.</summary>
        [MemberNotNullWhen(true, nameof(Errors))]
        [MemberNotNullWhen(false, nameof(Value))]
        public bool IsFailure
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _tag == Tag.Failure;
            }
        }

        /// <summary>Returns the success value or throws <see cref="InvalidOperationException"/> when result is failure.</summary>
        public TValue Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return IsSuccess ? _value : throw new ResultStateException<TValue>(_errors);
            }
        }

        /// <summary>Returns the error for failure results or throws <see cref="InvalidOperationException"/> when result is success.</summary>
        public ImmutableArray<Error> Errors
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return IsFailure ? _errors : ImmutableArray<Error>.Empty;
            }
        }

        // -----------------------
        // Basic combinators — allocation-conscious
        // -----------------------

        /// <summary>
        /// Transforms the successful result value using the specified mapping function, returning a new result of the mapped
        /// type.
        /// </summary>
        /// <remarks>If the original result is a failure, the mapping function is not invoked and the errors are
        /// propagated to the returned result.</remarks>
        /// <typeparam name="U">The type of the value returned by the mapping function.</typeparam>
        /// <param name="mapper">A function that takes the successful result value and returns a value of type <typeparamref name="U"/>. Cannot be
        /// null.</param>
        /// <returns>A <see cref="Result{U}"/> containing the mapped value if the original result was successful; otherwise, a failed
        /// result containing the same errors.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="mapper"/> is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Result<U> Map<U>(Func<TValue, U> mapper)
        {
            Guard.AgainstNull(mapper, nameof(mapper));
            return IsSuccess ? Result<U>.Succeed(mapper(_value)) : Result<U>.Fail(_errors);
        }

        /// <summary>
        /// Invokes the specified binder function if the current result is successful, returning its result; otherwise,
        /// propagates the failure.
        /// </summary>
        /// <remarks>This method enables chaining of operations that may fail, following the monadic bind
        /// pattern. If the current result is not successful, the binder function is not invoked and the failure is
        /// propagated.</remarks>
        /// <typeparam name="U">The type of the value returned by the binder function and contained in the resulting result.</typeparam>
        /// <param name="binder">A function to apply to the successful value, which returns a new result of type <typeparamref name="U"/>.
        /// Cannot be null.</param>
        /// <returns>A result containing the value produced by the binder function if the current result is successful;
        /// otherwise, a failed result containing the original errors.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="binder"/> is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Result<U> Bind<U>(Func<TValue, Result<U>> binder)
        {
            Guard.AgainstNull(binder, nameof(binder));
            return IsSuccess ? binder(_value) : Result<U>.Fail(_errors);
        }

        /// <summary>
        /// Invokes the specified asynchronous binder function if the current result is successful, returning its
        /// result; otherwise, propagates the failure.
        /// </summary>
        /// <remarks>This method enables chaining asynchronous operations that return results, propagating
        /// errors without invoking the binder if the current result is a failure.</remarks>
        /// <typeparam name="U">The type of the value returned by the binder function and the resulting result.</typeparam>
        /// <param name="binder">A function to apply to the successful value, which returns a task that produces a new result. Cannot be
        /// null.</param>
        /// <returns>A task that represents the asynchronous bind operation. If the current result is successful, the returned
        /// task yields the result of the binder function; otherwise, it yields a failed result containing the original
        /// errors.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="binder"/> is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<Result<U>> BindAsync<U>(Func<TValue, ValueTask<Result<U>>> binder)
        {
            Guard.AgainstNull(binder, nameof(binder));
            return IsSuccess ? binder(_value) : new ValueTask<Result<U>>(Result<U>.Fail(_errors));
        }

        /// <summary>
        /// Invokes the specified delegate based on whether the result represents a success or a failure, and returns the
        /// corresponding value.
        /// </summary>
        /// <remarks>This method provides a functional way to handle both success and failure cases by requiring explicit
        /// handling for each outcome.</remarks>
        /// <typeparam name="R">The type of the value returned by the delegates.</typeparam>
        /// <param name="onSuccess">A delegate to invoke if the result is successful. Receives the success value as its argument.</param>
        /// <param name="onFailure">A delegate to invoke if the result is a failure. Receives an immutable array of errors as its argument.</param>
        /// <returns>The value returned by either <paramref name="onSuccess"/> or <paramref name="onFailure"/>, depending on the result
        /// state.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="onSuccess"/> or <paramref name="onFailure"/> is <see langword="null"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public R Match<R>(Func<TValue, R> onSuccess, Func<ImmutableArray<Error>, R> onFailure)
        {
            Guard.AgainstNull(onSuccess, nameof(onSuccess));
            Guard.AgainstNull(onFailure, nameof(onFailure));
            return IsSuccess ? onSuccess(_value) : onFailure(_errors);
        }

        /// <summary>
        /// Returns the contained value if the operation was successful; otherwise, returns the specified default value.
        /// </summary>
        /// <param name="defaultValue">The value to return if the operation was not successful.</param>
        /// <returns>The contained value if successful; otherwise, the specified default value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue ValueOrDefault(TValue defaultValue) => IsSuccess ? _value : defaultValue;

        /// <summary>
        /// Attempts to retrieve the stored value if the operation was successful.
        /// </summary>
        /// <param name="value">When this method returns, contains the value associated with a successful result if available; otherwise,
        /// the default value for type <typeparamref name="TValue"/>.</param>
        /// <returns>true if the value was successfully retrieved; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(out TValue value)
        {
            if (IsSuccess)
            {
                value = _value;
                return true;
            }

            value = default!;
            return false;
        }

        /// <summary>
        /// Ensures that the current result satisfies the specified condition, returning a failure result with the provided
        /// error if the condition is not met.
        /// </summary>
        /// <remarks>If the current result is already a failure, the condition is not evaluated and the original result is
        /// returned.</remarks>
        /// <param name="predicate">A function that defines the condition to evaluate against the result's value. The function should return <see
        /// langword="true"/> if the value meets the condition; otherwise, <see langword="false"/>.</param>
        /// <param name="errorIfFalse">The error to associate with the result if the condition specified by <paramref name="predicate"/> is not satisfied.</param>
        /// <returns>A <see cref="Result{T}"/> that is unchanged if the current result is a failure or if the condition is satisfied;
        /// otherwise, a failure result containing <paramref name="errorIfFalse"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="predicate"/> or <paramref name="errorIfFalse"/> is <see langword="null"/>.</exception>
        public Result<TValue> Ensure(Func<TValue, bool> predicate, Error errorIfFalse)
        {
            Guard.AgainstNull(predicate, nameof(predicate));
            Guard.AgainstNull(errorIfFalse, nameof(errorIfFalse));

            return IsFailure || predicate(_value) ? this : Fail(ImmutableArray.Create(errorIfFalse));
        }

        /// <summary>
        /// Combines two <see cref="Result{T}"/> instances into a single result, propagating success or aggregating
        /// errors as appropriate.
        /// </summary>
        /// <remarks>If both results are failures, the returned result aggregates the errors from both
        /// <paramref name="first"/> and <paramref name="second"/>. This method is useful for scenarios where multiple
        /// operations may fail and you want to collect all error information.</remarks>
        /// <param name="first">The first result to combine. If <paramref name="first"/> is successful, the method returns <paramref
        /// name="second"/>.</param>
        /// <param name="second">The second result to combine. If <paramref name="second"/> is successful, the method returns <paramref
        /// name="first"/>.</param>
        /// <returns>A <see cref="Result{T}"/> that is successful if either input is successful; otherwise, a failed result
        /// containing the combined errors from both inputs.</returns>
        public static Result<TValue> Combine(Result<TValue> first, Result<TValue> second)
        {
            if (first.IsSuccess) return second;
            if (second.IsSuccess) return first;

            // Use ImmutableArray.Builder for efficient assembly
            var builder = ImmutableArray.CreateBuilder<Error>(first._errors.Length + second._errors.Length);
            builder.AddRange(first._errors);
            builder.AddRange(second._errors);
            return Fail(builder.ToImmutable());
        }

        /// <summary>
        /// Applies the specified mapping function to each item in the collection and returns a result containing a list of
        /// successful mapped values, or a failure result containing any errors encountered.
        /// </summary>
        /// <remarks>If any mapping fails, the returned result will contain all errors if <paramref
        /// name="aggregateAllErrors"/> is <see langword="true"/>; otherwise, only the errors from the first failure are
        /// included. The order of mapped values in the result matches the order of the input items.</remarks>
        /// <typeparam name="TInput">The type of elements in the input collection to be mapped.</typeparam>
        /// <param name="items">The collection of input items to be mapped. Cannot be null.</param>
        /// <param name="mapper">A function that maps each input item to a result. Cannot be null.</param>
        /// <param name="aggregateAllErrors">If <see langword="true"/>, collects all errors from failed mappings; if <see langword="false"/>, returns on the
        /// first error encountered. The default is <see langword="true"/>.</param>
        /// <returns>A result containing a read-only list of successfully mapped values if all mappings succeed; otherwise, a failure
        /// result containing the errors encountered.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="items"/> or <paramref name="mapper"/> is null.</exception>
        public static Result<IReadOnlyList<TValue>> Traverse<TInput>(IEnumerable<TInput> items, Func<TInput, Result<TValue>> mapper, bool aggregateAllErrors = true)
        {
            Guard.AgainstNull(items, nameof(items));
            Guard.AgainstNull(mapper, nameof(mapper));

            var valuesBuilder = ImmutableArray.CreateBuilder<TValue>();
            var errorsBuilder = ImmutableArray.CreateBuilder<Error>();

            foreach (var it in items)
            {
                var r = mapper(it);
                if (r.IsSuccess)
                {
                    valuesBuilder.Add(r.Value);
                }
                else
                {
                    errorsBuilder.AddRange(r.Errors);
                    if (!aggregateAllErrors)
                    {
                        // short-circuit on first error
                        return Result<IReadOnlyList<TValue>>.Fail(errorsBuilder.ToImmutable());
                    }
                }
            }

            if (errorsBuilder.Count > 0) return Result<IReadOnlyList<TValue>>.Fail(errorsBuilder.ToImmutable());
            return Result<IReadOnlyList<TValue>>.Succeed((IReadOnlyList<TValue>)valuesBuilder.ToImmutable());
        }

        /// <summary>
        /// Aggregates a sequence of result values into a single result containing a list of successful values, or an
        /// error if any result in the sequence is a failure.
        /// </summary>
        /// <remarks>If any result in the sequence is a failure, the returned result will contain error
        /// information. When <paramref name="aggregateAllErrors"/> is <see langword="true"/>, all errors from failed
        /// results are included; when <see langword="false"/>, only the first error is reported. The order of values in
        /// the returned list matches the order of the input sequence.</remarks>
        /// <param name="results">The collection of result values to aggregate. Each item represents an individual operation that may succeed
        /// or fail.</param>
        /// <param name="aggregateAllErrors">Indicates whether to aggregate all errors from failed results into a single error result. If <see
        /// langword="true"/>, all errors are combined; otherwise, only the first error is returned.</param>
        /// <returns>A result containing a read-only list of all successful values if every result in the sequence is successful;
        /// otherwise, a result containing the aggregated error(s).</returns>
        public static Result<IReadOnlyList<TValue>> Sequence(IEnumerable<Result<TValue>> results, bool aggregateAllErrors = true)
            => Traverse(results, r => r, aggregateAllErrors);

        /// <summary>Try find first error with matching code; O(n) worst-case, optional lazy index can be added in future.</summary>
        public bool TryGetError(string code, out Error error)
        {
            code = Guard.AgainstNullOrWhitespace(code, nameof(code));

            if (IsFailure)
            {
                foreach (var e in _errors)
                {
                    if (string.Equals(e.Code?.Key, code, StringComparison.Ordinal)) { error = e; return true; }
                }
            }
            error = default!;
            return false;
        }

        /// <summary>
        /// Creates a task that represents the current result, allowing asynchronous consumption of the result value.
        /// </summary>
        /// <remarks>This method is useful for integrating synchronous result values into asynchronous workflows, such as
        /// when returning a result from an async method signature.</remarks>
        /// <returns>A <see cref="Task{Result}"/> that contains this result instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<Result<TValue>> ToTask() => IsSuccess ? System.Threading.Tasks.Task.FromResult(this) : System.Threading.Tasks.Task.FromResult(this);

        /// <summary>
        /// Creates a <see cref="ValueTask{Result}"/> that represents the asynchronous result of this operation.
        /// </summary>
        /// <returns>A <see cref="ValueTask{Result}"/> containing the result of the current operation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<Result<TValue>> ToValueTask() => new(this);

        private string DebuggerDisplay
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return IsSuccess ? $"Succeed<{typeof(TValue).Name}>({_value})" : $"Fail<{typeof(TValue).Name}>({_errors.Length} errors)";
            }
        }
    }
}
