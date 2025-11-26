// <copyright file="Class1.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Zentient.Codes;
using Zentient.Metadata;

namespace Zentient.Errors
{
    /// <summary>
    /// Severity describes how an error should be interpreted by policies and by the runtime.
    /// </summary>
    public enum ErrorSeverity : byte
    {
        /// <summary>No problem; informational notice only.</summary>
        Info = 0,

        /// <summary>Non-fatal; the operation may continue or degrade gracefully.</summary>
        Warning = 1,

        /// <summary>Recoverable failure; retry or fallback strategies are appropriate.</summary>
        Recoverable = 2,

        /// <summary>Fatal failure; the operation must be halted and propagated.</summary>
        Fatal = 3
    }

    /// <summary>
    /// Immutable, structured, typed error envelope.
    /// Errors carry both a canonical string key and an optional typed code <see cref="ICode{TDefinition}"/>.
    /// The typed code provides strong protocol/domain identity, while the key ensures stable serialization.
    /// </summary>
    public sealed class Error
    {
        /// <summary>
        /// Canonical key for the error code, guaranteed to be stable for serialization and policies.
        /// This derives from <see cref="Code"/> when available; otherwise from builder input.
        /// </summary>
        public string CodeKey { get; }

        /// <summary>User-facing, human-readable message describing the error.</summary>
        public string Message { get; }

        /// <summary>Behavioral classification describing severity and propagation expectations.</summary>
        public ErrorSeverity Severity { get; }

        /// <summary>Immutable diagnostic metadata describing the failure context.</summary>
        public IMetadata DiagnosticMetadata { get; }

        /// <summary>Attached exception (optional). Preserved for diagnostics but not required.</summary>
        public Exception? Exception { get; }

        /// <summary>
        /// Typed code conforming to <see cref="ICode{TDefinition}"/> with <see cref="ICodeDefinition"/>.
        /// Allows the error to express domain-safe codes (e.g. HTTP codes, gRPC status codes).
        /// </summary>
        public ICode<ICodeDefinition>? Code { get; }

        internal Error(
            string message,
            ErrorSeverity severity,
            IMetadata diagnosticMetadata,
            Exception? exception,
            ICode<ICodeDefinition> typedCode)
        {
            if (typedCode is null)
                throw new ArgumentNullException("Typed code must be provided for Error instances.", nameof(typedCode));
            if (string.IsNullOrWhiteSpace(typedCode.Key))
                throw new ArgumentException("Typed code must have a non-empty Key.", nameof(typedCode));
            if (!string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Error message must be non-empty.", nameof(message));
            Code = typedCode;
            CodeKey = typedCode.Key!;
            Message = message;
            Severity = severity;
            DiagnosticMetadata = diagnosticMetadata ?? Zentient.Metadata.Metadata.Empty;
            Exception = exception;
        }

        // ------------------------------------------------------
        // Static factory helpers
        // ------------------------------------------------------

        /// <summary>
        /// Create a new error builder with the given code and message.
        /// This is the primary DX-first constructor for Error instances.
        /// </summary>
        public static Builder NewBuilder(string codeKey, string message) => new(codeKey, message);

        /// <summary>Common factory for validation errors.</summary>
        public static Error Validation(string message) =>
            NewBuilder("VALIDATION", message).WithSeverity(ErrorSeverity.Recoverable).Build();

        /// <summary>Common factory for not-found errors.</summary>
        public static Error NotFound(string key) =>
            NewBuilder("NOT_FOUND", $"The requested item '{key}' was not found.")
            .WithSeverity(ErrorSeverity.Recoverable)
            .Build();

        /// <summary>Common factory for conflict errors.</summary>
        public static Error Conflict(string message) =>
            NewBuilder("CONFLICT", message)
            .WithSeverity(ErrorSeverity.Recoverable)
            .Build();

        /// <summary>Common factory for unexpected errors, wrapping an exception.</summary>
        public static Error Unexpected(Exception ex) =>
            NewBuilder("UNEXPECTED", ex?.Message ?? "Unexpected error")
            .WithException(ex)
            .WithSeverity(ErrorSeverity.Fatal)
            .Build();

        /// <summary>Canonical cancellation error.</summary>
        public static Error Canceled =>
            NewBuilder("CANCELED", "The operation was canceled.")
            .WithSeverity(ErrorSeverity.Recoverable)
            .Build();

        // ======================================================================
        // Builder
        // ======================================================================

        /// <summary>
        /// Mutable builder for constructing immutable <see cref="Error"/> instances.
        /// Supports both string-key and typed-code paths. Ensures zero-allocation semantics
        /// for diagnostic metadata until Build() is called.
        /// </summary>
        public sealed class Builder
        {
            private ICodeBuilder<ICodeDefinition>? _codeBuilder;
            private string _message;
            private ErrorSeverity _severity;
            private Exception? _exception;
            private readonly Zentient.Metadata.Metadata.Builder _diagnosticBuilder = Zentient.Metadata.Metadata.NewBuilder();
            private ICode<ICodeDefinition>? _typedCode;
            private string _codeKey;

            internal Builder(string codeKey, string message)
            {
                if (string.IsNullOrWhiteSpace(codeKey))
                    throw new ArgumentException("Code key must be non-empty.", nameof(codeKey));

                _message = message ?? throw new ArgumentNullException(nameof(message));
                _severity = ErrorSeverity.Fatal;
                _codeKey = codeKey;
            }

            public Builder WithCode<TDefinition>(Func<ICodeBuilder<TDefinition>, ICodeBuilder<TDefinition>> codeBuilderFactory)
                where TDefinition : ICodeDefinition
            {

                _codeBuilder = (ICodeBuilder<ICodeDefinition>?)(codeBuilderFactory(new CodeBuilder<TDefinition>()) ?? throw new ArgumentNullException(nameof(codeBuilderFactory)));
                return this;
            }

            /// <summary>
            /// Set the error message.
            /// </summary>
            public Builder WithMessage(string message)
            {
                _message = message ?? throw new ArgumentNullException(nameof(message));
                return this;
            }

            /// <summary>
            /// Set the severity of the error.
            /// </summary>
            public Builder WithSeverity(ErrorSeverity severity)
            {
                _severity = severity;
                return this;
            }

            /// <summary>
            /// Attach an exception for diagnostic purposes.
            /// </summary>
            public Builder WithException(Exception? exception)
            {
                _exception = exception;
                return this;
            }

            /// <summary>
            /// Attach diagnostic metadata using a mutable builder.
            /// </summary>
            public Builder WithMetadata(Action<Zentient.Metadata.Metadata.Builder> configure)
            {
                configure?.Invoke(_diagnosticBuilder);
                return this;
            }

            /// <summary>
            /// Attach a typed code instance. This automatically sets the builder's code key from the code's Key.
            /// </summary>
            public Builder WithTypedCode(ICode<ICodeDefinition> code)
            {
                _typedCode = code ?? throw new ArgumentNullException(nameof(code));
                _codeKey = code.Key; // The typed code is authoritative.
                return this;
            }

            /// <summary>
            /// Typed convenience method. Creates and attaches a typed code instance in one step.
            /// </summary>
            public Builder WithTypedCode<TDefinition>(
                string key,
                TDefinition definition,
                IMetadata? metadata = null,
                string? displayName = null)
                where TDefinition : ICodeDefinition
            {
                // Construct via Code<TDefinition>.GetOrCreate, then cast through covariance.
                var typed = Code<TDefinition>.GetOrCreate(key, definition, metadata, displayName);
                _typedCode = (ICode<ICodeDefinition>)typed;
                _codeKey = typed.Key;
                return this;
            }

            /// <summary>
            /// Build the immutable <see cref="Error"/> instance.
            /// </summary>
            public Error Build()
            {
                var diagnostic = _diagnosticBuilder.Build();
                return new Error(
                    _message,
                    _severity,
                    diagnostic,
                    _exception,
                    _typedCode
                );
            }
        }
    }
}
