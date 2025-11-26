// <copyright file="Result.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Results
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    using Zentient.Codes;
    using Zentient.Errors;
    using Zentient.Metadata;

    /// <summary>
    /// High-performance discriminated result type carrying either a success value of type <typeparamref name="T"/>
    /// or an <see cref="Error"/>. The implementation is optimized to minimize allocations on hot paths.
    /// </summary>
    /// <typeparam name="T">Type of the successful payload.</typeparam>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public struct Result<T>
    {
        private readonly byte _kind; // 0 = success, 1 = failure
        private readonly T? _value;
        private readonly Error? _error;

        // Operational metadata lazy representation:
        // - null => Zentient.Metadata.Metadata.Empty
        // - IMetadata => materialized snapshot
        // - KeyValuePair<string, object?>[] => small-path staging array (deterministic order)
        private object? _opMetaOrSmall;

        // Warnings stored as normalized ImmutableArray
        private readonly ImmutableArray<Error> _warnings;

        // ----------------------
        // Constructors (internal so helpers can create instances)
        // ----------------------
        internal Result(T value, object? opMetaOrSmall, ImmutableArray<Error> warnings)
        {
            _kind = 0;
            _value = value;
            _error = null;
            _opMetaOrSmall = opMetaOrSmall;
            _warnings = warnings.IsDefault ? ImmutableArray<Error>.Empty : NormalizeWarnings(warnings);
        }

        internal Result(Error error, object? opMetaOrSmall, ImmutableArray<Error> warnings)
        {
            _kind = 1;
            _value = default;
            _error = error ?? throw new ArgumentNullException(nameof(error));
            _opMetaOrSmall = opMetaOrSmall;
            _warnings = warnings.IsDefault ? ImmutableArray<Error>.Empty : NormalizeWarnings(warnings);
        }

        // ----------------------
        // Fast-path factories
        // ----------------------

        /// <summary>Success factory with no metadata or warnings.</summary>
        public static Result<T> Ok(T value) => new Result<T>(value: value, opMetaOrSmall: null, warnings: ImmutableArray<Error>.Empty);

        /// <summary>Success factory with materialized operational metadata snapshot.</summary>
        public static Result<T> Ok(T value, IMetadata? operationalMetadata) =>
            new Result<T>(value: value, opMetaOrSmall: operationalMetadata ?? Zentient.Metadata.Metadata.Empty, warnings: ImmutableArray<Error>.Empty);

        /// <summary>Success factory that configures operational metadata using a builder action.</summary>
        public static Result<T> Ok(T value, Action<Zentient.Metadata.Metadata.Builder> configureOperationalMetadata)
        {
            if (configureOperationalMetadata is null) return Ok(value);
            var mb = Zentient.Metadata.Metadata.NewBuilder();
            configureOperationalMetadata(mb);
            var built = mb.Build();
            return new Result<T>(value: value, opMetaOrSmall: built, warnings: ImmutableArray<Error>.Empty);
        }

        /// <summary>Failure factory with no metadata or warnings.</summary>
        public static Result<T> Fail(Error error) => new Result<T>(error: error, opMetaOrSmall: null, warnings: ImmutableArray<Error>.Empty);

        /// <summary>Failure factory with a materialized operational metadata snapshot.</summary>
        public static Result<T> Fail(Error error, Zentient.Metadata.IMetadata operationalMetadata) =>
            new Result<T>(error: error, opMetaOrSmall: operationalMetadata ?? Zentient.Metadata.Metadata.Empty, warnings: ImmutableArray<Error>.Empty);

        /// <summary>Failure factory that uses an error builder and optional metadata builder.</summary>
        public static Result<T> Fail(Action<Error.Builder> configureError, Action<Zentient.Metadata.Metadata.Builder>? configureOperationalMetadata = null)
        {
            var eb = new Error.Builder(Code.From<ICodeDefinition>("UNKNOWN_ERROR"), "An operation failed.");
            configureError?.Invoke(eb);
            var err = eb.Build();

            object? meta = null;
            if (configureOperationalMetadata is not null)
            {
                var mb = Zentient.Metadata.Metadata.NewBuilder();
                configureOperationalMetadata(mb);
                meta = mb.Build();
            }

            return new Result<T>(error: err, opMetaOrSmall: meta, warnings: ImmutableArray<Error>.Empty);
        }

        /// <summary>Success factory which also attaches warnings and optional operational metadata.</summary>
        public static Result<T> OkWithWarnings(T value, IEnumerable<Error> warnings, Action<Zentient.Metadata.Metadata.Builder>? configureOperationalMetadata = null)
        {
            var arr = warnings is null ? ImmutableArray<Error>.Empty : ImmutableArray.CreateRange(warnings);
            object? meta = null;
            if (configureOperationalMetadata is not null)
            {
                var mb = Zentient.Metadata.Metadata.NewBuilder();
                configureOperationalMetadata(mb);
                meta = mb.Build();
            }

            return new Result<T>(value: value, opMetaOrSmall: meta, warnings: arr);
        }

        // ----------------------
        // Properties
        // ----------------------

        /// <summary>True when the result represents success.</summary>
        public bool IsSuccess => _kind == 0;

        /// <summary>True when the result represents failure.</summary>
        public bool IsFailure => _kind != 0;

        /// <summary>Return the success value or throw when result is failure.</summary>
        public T Value => IsSuccess ? _value! : throw new InvalidOperationException($"Result failed: {_error?.Message}");

        /// <summary>Return the error for failure results or throw when result is success.</summary>
        public Error Error => IsFailure ? _error! : throw new InvalidOperationException("Result succeeded.");

        /// <summary>
        /// Operational metadata snapshot. Guarantee: non-null, deterministic for small-paths.
        /// Property materializes small-path arrays on demand and caches the result.
        /// </summary>
        public Zentient.Metadata.IMetadata OperationalMetadata
        {
            get
            {
                if (_opMetaOrSmall is null) return Zentient.Metadata.Metadata.Empty;
                if (_opMetaOrSmall is Zentient.Metadata.IMetadata im) return im;
                if (_opMetaOrSmall is KeyValuePair<string, object?>[] arr)
                {
                    var b = Zentient.Metadata.Metadata.NewBuilder();
                    b.SetRange(arr);
                    var built = b.Build();
                    _opMetaOrSmall = built;
                    return built;
                }

                return _opMetaOrSmall as Zentient.Metadata.IMetadata ?? Zentient.Metadata.Metadata.Empty;
            }
        }

        /// <summary>Warnings as a normalized immutable array.</summary>
        public ImmutableArray<Error> Warnings => _warnings.IsDefault ? ImmutableArray<Error>.Empty : _warnings;

        // ----------------------
        // Mutators & helpers
        // ----------------------

        /// <summary>Return a copy of this result with new operational metadata. No-op when equal by reference.</summary>
        public Result<T> WithOperationalMetadata(Zentient.Metadata.IMetadata metadata)
        {
            var meta = metadata ?? Zentient.Metadata.Metadata.Empty;

            if (_opMetaOrSmall is Zentient.Metadata.IMetadata existing && ReferenceEquals(existing, meta)) return this;
            if (meta.Count == 0 && (_opMetaOrSmall is null)) return this;

            return IsSuccess ? new Result<T>(_value!, opMetaOrSmall: meta, warnings: _warnings) : new Result<T>(_error!, opMetaOrSmall: meta, warnings: _warnings);
        }

        /// <summary>Configure operational metadata using a builder and return a new Result instance. No-op if builder makes no change.</summary>
        public Result<T> WithOperationalMetadata(Action<Zentient.Metadata.Metadata.Builder> configure)
        {
            if (configure is null) return this;
            var mb = Zentient.Metadata.Metadata.NewBuilder();
            configure(mb);
            var built = mb.Build();
            return WithOperationalMetadata(built);
        }

        /// <summary>Return a copy with the given warnings merged and normalized. Avoids allocation when identical.</summary>
        public Result<T> WithWarnings(IEnumerable<Error> warnings)
        {
            var add = warnings is null ? ImmutableArray<Error>.Empty : ImmutableArray.CreateRange(warnings);
            if (add.IsDefaultOrEmpty && _warnings.IsDefaultOrEmpty) return this;

            var merged = _warnings.IsDefaultOrEmpty ? add : _warnings.AddRange(add);
            var norm = NormalizeWarnings(merged);
            if (Enumerable.SequenceEqual(norm, _warnings)) return this;
            return IsSuccess ? new Result<T>(_value!, _opMetaOrSmall, norm) : new Result<T>(_error!, _opMetaOrSmall, norm);
        }

        /// <summary>Map successful value to another type while preserving metadata & warnings. Failure preserves error & metadata.</summary>
        public Result<U> Map<U>(Func<T, U> map)
        {
            if (map is null) throw new ArgumentNullException(nameof(map));
            if (!IsSuccess) return ResultInternal.CreateFailure<U>(_error!, GetOperationalMetadataCopy(), Warnings);
            var mapped = map(_value!);
            var pairs = GetOperationalMetadataPairs().ToArray();
            return ResultInternal.CreateSuccess<U>(mapped, opMetaOrSmall: pairs.Length == 0 ? null : (object)pairs, Warnings);
        }

        /// <summary>Bind into another result-returning function.</summary>
        public Result<U> Bind<U>(Func<T, Result<U>> binder)
        {
            if (binder is null) throw new ArgumentNullException(nameof(binder));
            if (!IsSuccess) return ResultInternal.CreateFailure<U>(_error!, GetOperationalMetadataCopy(), Warnings);
            var res = binder(_value!);
            var merged = Zentient.Metadata.Metadata.DeepMerge(res.OperationalMetadata, GetOperationalMetadataCopy());
            // materialize merged pairs and set on returned result
            var pairs = merged.Count == 0
                ? Array.Empty<KeyValuePair<string, object?>>()
                : (IEnumerable<KeyValuePair<string, object?>>)merged;
            return res.WithOperationalMetadata(mb => mb.SetRange(pairs));
        }

        /// <summary>Async bind optimized to avoid state machine allocation on fast-path.</summary>
        public ValueTask<Result<U>> BindAsync<U>(Func<T, ValueTask<Result<U>>> binder)
        {
            if (binder is null) throw new ArgumentNullException(nameof(binder));
            if (!IsSuccess) return new ValueTask<Result<U>>(ResultInternal.CreateFailure<U>(_error!, GetOperationalMetadataCopy(), Warnings));
            return binder(_value!);
        }

        /// <summary>Match expression for success/failure branches.</summary>
        public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure)
        {
            if (onSuccess is null) throw new ArgumentNullException(nameof(onSuccess));
            if (onFailure is null) throw new ArgumentNullException(nameof(onFailure));
            return IsSuccess ? onSuccess(_value!) : onFailure(_error!);
        }

        // ----------------------
        // Convenience & utilities
        // ----------------------

        /// <summary>Return true when result is success and all warnings are non-fatal.</summary>
        public bool HasOnlyNonFatalWarnings()
        {
            if (IsFailure) return false;
            if (_warnings.IsDefaultOrEmpty) return false;
            foreach (var w in _warnings)
            {
                if (w.Severity > ErrorSeverity.Warning) return false;
            }
            return true;
        }

        /// <summary>Deconstruct the result into a simple tuple form.</summary>
        public void Deconstruct(out bool isSuccess, out T? value, out Error? error)
        {
            isSuccess = IsSuccess;
            value = _value;
            error = _error;
        }

        /// <summary>Implicit conversion from value to successful <see cref="Result{T}"/>.</summary>
        public static implicit operator Result<T>(T value) => Ok(value);

        /// <summary>Implicit conversion from <see cref="Error"/> to failed <see cref="Result{T}"/>.</summary>
        public static implicit operator Result<T>(Error error) => Fail(error);

        /// <inheritdoc/>
        public override string ToString()
        {
            if (IsSuccess) return $"Ok<{typeof(T).Name}> (Warnings: {_warnings.Length})";
            return $"Fail<{typeof(T).Name}> {Error.Code} ({Error.Severity})";
        }

        // ----------------------
        // Internal helpers
        // ----------------------

        private static ImmutableArray<Error> NormalizeWarnings(ImmutableArray<Error> src)
        {
            if (src.IsDefaultOrEmpty) return ImmutableArray<Error>.Empty;
            var arr = src.ToArray();
            Array.Sort(arr, (a, b) =>
            {
                var c = string.CompareOrdinal(a.Code.Key, b.Code.Key);
                if (c != 0) return c;
                return string.CompareOrdinal(a.Message, b.Message);
            });
            return ImmutableArray.CreateRange(arr);
        }

        private IEnumerable<KeyValuePair<string, object?>> GetOperationalMetadataPairs()
        {
            var meta = GetOperationalMetadataCopy();
            return meta.Count == 0 ? Array.Empty<KeyValuePair<string, object?>>() : meta;
        }

        private Zentient.Metadata.IMetadata GetOperationalMetadataCopy()
        {
            var meta = OperationalMetadata;
            return meta;
        }

        private string DebuggerDisplay => IsSuccess ? $"Ok<{typeof(T).Name}> Value={(Value is null ? "null" : Value.ToString())}" : $"Fail<{typeof(T).Name}> {_error?.Code}";
    }
    /// <summary>
    /// Lightweight result builder to create rich results without intermediate allocations.
    /// </summary>
    public sealed class ResultBuilder<T>
    {
        private T? _value;
        private Error? _error;
        private KeyValuePair<string, object?>[]? _opSmall;
        private Zentient.Metadata.Metadata.Builder? _metaBuilder;
        private ImmutableArray<Error>.Builder? _warningsBuilder;
        private bool _isFailure;

        /// <summary>Create a new builder instance.</summary>
        public ResultBuilder() { }

        /// <summary>Set the success value on the builder.</summary>
        public ResultBuilder<T> WithValue(T value) { _value = value; _isFailure = false; return this; }

        /// <summary>Set the error on the builder.</summary>
        public ResultBuilder<T> WithError(Error error) { _error = error; _isFailure = true; return this; }

        /// <summary>Add a small-path operational metadata key/value pair.</summary>
        public ResultBuilder<T> AddOperational(string key, object? value)
        {
            if (_metaBuilder is not null) { _metaBuilder.Set(key, value); return this; }

            if (_opSmall is null) _opSmall = Array.Empty<KeyValuePair<string, object?>>();

            var list = new List<KeyValuePair<string, object?>>(_opSmall) { new KeyValuePair<string, object?>(key, value) };
            _opSmall = list.ToArray();
            return this;
        }

        /// <summary>Add a warning to the builder.</summary>
        public ResultBuilder<T> AddWarning(Error w)
        {
            if (_warningsBuilder is null) _warningsBuilder = ImmutableArray.CreateBuilder<Error>();
            _warningsBuilder.Add(w);
            return this;
        }

        /// <summary>Build the final <see cref="Result{T}"/> instance.</summary>
        public Result<T> Build()
        {
            var warnings = _warningsBuilder is null ? ImmutableArray<Error>.Empty : _warningsBuilder.ToImmutable();

            object? meta = null;
            if (_metaBuilder is not null) meta = _metaBuilder.Build();
            else if (_opSmall is not null)
            {
                Array.Sort(_opSmall, (a, b) => StringComparer.Ordinal.Compare(a.Key, b.Key));
                meta = _opSmall;
            }

            if (_isFailure)
            {
                if (_error is null) throw new InvalidOperationException("ResultBuilder: error not set for failure.");
                return ResultInternal.CreateFailure<T>(_error, meta, warnings);
            }

            return ResultInternal.CreateSuccess<T>(_value!, meta, warnings);
        }
    }

    // Internal static factories used by ResultBuilder.Build and internal operations.
    internal static class ResultInternal
    {
        public static Result<T> CreateSuccess<T>(T value, object? opMetaOrSmall, ImmutableArray<Error> warnings) => new Result<T>(value, opMetaOrSmall, warnings);
        public static Result<T> CreateFailure<T>(Error error, object? opMetaOrSmall, ImmutableArray<Error> warnings) => new Result<T>(error, opMetaOrSmall, warnings);
    }

    /// <summary>
    /// Public-friendly static helpers to create results when external callers need to create specialized instances.
    /// </summary>
    public static class Result
    {
        /// <summary>Create a success result with explicit internal payload.</summary>
        public static Result<T> CreateSuccess<T>(T value, object? opMetaOrSmall, ImmutableArray<Error> warnings) => ResultInternal.CreateSuccess<T>(value, opMetaOrSmall, warnings);

        /// <summary>Create a failure result with explicit internal payload.</summary>
        public static Result<T> CreateFailure<T>(Error error, object? opMetaOrSmall, ImmutableArray<Error> warnings) => ResultInternal.CreateFailure<T>(error, opMetaOrSmall, warnings);
    }
}
