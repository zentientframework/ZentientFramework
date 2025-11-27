// <copyright file="Severity.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Errors
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using Zentient;
    using Zentient.Codes;
    using Zentient.Facades;
    using Zentient.Metadata;

    /// <summary>
    /// Immutable, structured, typed error envelope used across the Zentient core.
    /// This type is intentionally serializer-agnostic: adapters and transport layers decide on
    /// how errors are encoded for persistence or transport.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public sealed record Error
    {
        /// <summary>
        /// Gets the canonical key that identifies the error code.
        /// When a typed <see cref="ICode{TDefinition}"/> is attached the code key is aligned with the
        /// typed code's <see cref="ICode{TDefinition}"/> key; otherwise it is taken from the builder input.
        /// </summary>
        public string CodeKey { get; }

        /// <summary>
        /// Gets the human-facing error message.
        /// This value is guaranteed to be non-empty.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the severity classification for this error.
        /// Use this to decide propagation, remediation, or instrumentation actions.
        /// </summary>
        public Severity Severity { get; }

        /// <summary>
        /// Gets immutable diagnostic metadata attached to the error.
        /// This collection is never null; it may be empty if no diagnostic metadata was provided.
        /// </summary>
        public IMetadata DiagnosticMetadata { get; }

        /// <summary>
        /// Gets an optional exception associated with the error for diagnostic purposes.
        /// This exception is preserved for observability but is not used for control-flow by core.
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// Gets an optional strongly-typed canonical code associated with the error.
        /// The typed code enables domain-level identity and cross-protocol mapping.
        /// </summary>
        public ICode<ICodeDefinition>? Code { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Error"/> class.
        /// This constructor is internal; creation is expected via <see cref="Builder"/> or factory helpers.
        /// </summary>
        /// <param name="codeKey">The canonical code key. Must be non-empty.</param>
        /// <param name="message">The human-readable message. Must be non-empty.</param>
        /// <param name="severity">Severity classification.</param>
        /// <param name="diagnosticMetadata">Diagnostic metadata; if null an empty metadata object will be used.</param>
        /// <param name="exception">Optional associated exception for diagnostics.</param>
        /// <param name="code">Optional typed code instance; when provided the builder aligns <paramref name="codeKey"/> to the typed code's key.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="codeKey"/> or <paramref name="message"/> is null/empty/whitespace.</exception>
        internal Error(string codeKey, string message, Severity severity, IMetadata diagnosticMetadata, Exception? exception, ICode<ICodeDefinition>? code)
        {
            // Use centralized guard helpers for consistent DX and minimal duplication.
            CodeKey = Guard.AgainstNullOrWhitespace(codeKey, nameof(codeKey));
            Message = Guard.AgainstNullOrWhitespace(message, nameof(message));
            Severity = Guard.AgainstInvalidEnum(severity, nameof(severity));

            DiagnosticMetadata = diagnosticMetadata ?? Metadata.Empty;
            Exception = exception;
            DiagnosticMetadata = diagnosticMetadata ?? Metadata.Empty;
            Exception = exception;
            Code = code;
        }

        // ------------------------------------------------------
        // Static factory helpers
        // ------------------------------------------------------

        /// <summary>
        /// Creates a new <see cref="Builder"/> initialized with the provided code key and message.
        /// Use the fluent builder to configure severity, metadata, exception and typed codes.
        /// </summary>
        /// <param name="codeKey">The canonical code key to seed the builder.</param>
        /// <param name="message">The human-facing message to seed the builder.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Builder NewBuilder(string codeKey, string message) => new(codeKey, message);

        /// <summary>
        /// Creates a common validation error with the canonical key <c>VALIDATION</c>.
        /// </summary>
        /// <param name="message">Validation failure message.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Error Validation(string message) =>
            NewBuilder("VALIDATION", message).WithSeverity(Severity.Recoverable).Build();

        /// <summary>
        /// Creates a common not-found error with the canonical key <c>NOT_FOUND</c>.
        /// </summary>
        /// <param name="key">Identifier of the missing entity used to compose the message.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Error NotFound(string key) =>
            NewBuilder("NOT_FOUND", $"The requested item '{key}' was not found.")
            .WithSeverity(Severity.Recoverable)
            .Build();

        /// <summary>
        /// Creates a conflict error with the canonical key <c>CONFLICT</c>.
        /// </summary>
        /// <param name="message">Conflict description.</param>
        /// <returns>An <see cref="Error"/> instance representing the conflict.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Error Conflict(string message) =>
            NewBuilder("CONFLICT", message)
            .WithSeverity(Severity.Recoverable)
            .Build();

        /// <summary>
        /// Creates an unexpected error that wraps an exception using the canonical key <c>UNEXPECTED</c>.
        /// </summary>
        /// <param name="ex">The exception that triggered the unexpected error.</param>
        /// <returns>An <see cref="Error"/> instance representing the unexpected error.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Error Unexpected(Exception ex) =>
            NewBuilder("UNEXPECTED", ex?.Message ?? "Unexpected error")
            .WithException(ex)
            .WithSeverity(Severity.Fatal)
            .Build();

        /// <summary>
        /// Gets a canonical cancellation error (<c>CANCELED</c>).
        /// </summary>
        public static Error Canceled
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return NewBuilder("CANCELED", "The operation was canceled.")
                    .WithSeverity(Severity.Recoverable)
                    .Build();
            }
        }

        /// <summary>
        /// Returns a brief string representation of the error for logging.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => $"[{CodeKey}] {Message}";

        private string DebuggerDisplay => ToString();

        // ======================================================================
        // Builder
        // ======================================================================

        /// <summary>
        /// Fluent builder for constructing immutable <see cref="Error"/> instances.
        /// Designed to be ergonomic for call-sites and to minimize allocations for common simple errors.
        /// </summary>
        public sealed class Builder
        {
            private string _codeKey;
            private string _message;
            private Severity _severity;
            private Exception? _exception;

            // Lazy-initialized metadata builder to reduce allocation overhead on simple errors
            private Metadata.Builder? _diagnosticBuilder;

            private ICode<ICodeDefinition>? _typedCode;

            /// <summary>
            /// Initializes a new instance of the <see cref="Builder"/> class using the provided code key and message.
            /// Input validation is performed immediately to give fast feedback to callers.
            /// </summary>
            /// <param name="codeKey">Initial code key. Must be non-empty.</param>
            /// <param name="message">Initial human-readable message. Must be non-null.</param>
            internal Builder(string codeKey, string message)
            {
                _codeKey = Guard.AgainstNullOrWhitespace(codeKey, nameof(codeKey));
                _message = Guard.AgainstNullOrWhitespace(message, nameof(message));
                _severity = Severity.Fatal; // Default to safe/conservative severity
            }

            /// <summary>
            /// Sets the error message.
            /// </summary>
            /// <param name="message">A non-null human-readable message.</param>
            /// <returns>The builder instance.</returns>
            public Builder WithMessage(string message)
            {
                message = Guard.AgainstNullOrWhitespace(message, nameof(message));
                _message = Guard.AgainstNullOrWhitespace(message, nameof(message));
                return this;
            }

            /// <summary>
            /// Sets the severity classification for the error.
            /// </summary>
            /// <param name="severity">Severity to assign.</param>
            /// <returns>The builder instance.</returns>
            public Builder WithSeverity(Severity severity)
            {
                _severity = Guard.AgainstInvalidEnum(severity, nameof(severity));
                return this;
            }

            /// <summary>
            /// Attaches an exception to the error for diagnostic purposes.
            /// </summary>
            /// <param name="exception">Optional exception.</param>
            /// <returns>The builder instance.</returns>
            public Builder WithException(Exception? exception)
            {
                _exception = exception;
                return this;
            }

            /// <summary>
            /// Configures diagnostic metadata using a mutable metadata builder.
            /// The provided delegate is invoked immediately and the result is captured when <see cref="Build"/> is called.
            /// </summary>

            /// <param name="configure">Action that configures a <see cref="Metadata.Builder"/>.</param>
            /// <returns>The builder instance.</returns>
            public Builder WithMetadata(Action<Metadata.Builder> configure)
            {
                if (configure is null) return this;


                _diagnosticBuilder ??= Metadata.NewBuilder();
                configure(_diagnosticBuilder);
                return this;
            }

            /// <summary>
            /// Attaches an existing typed code instance to the builder.
            /// When provided the typed code's key becomes authoritative and is applied to the resulting <see cref="Error"/>.
            /// </summary>
            /// <param name="code">Typed code instance to attach. Cannot be null.</param>
            /// <returns>The builder instance.</returns>
            public Builder WithTypedCode(ICode<ICodeDefinition> code)
            {
                _typedCode = Guard.AgainstNull(code, nameof(code));
                _codeKey = code.Key; // The typed code is authoritative.
                return this;
            }

            /// <summary>
            /// Constructs or retrieves a typed code for <typeparamref name="TDefinition"/> and attaches it to the builder.
            /// If <paramref name="metadata"/> and <paramref name="displayName"/> are omitted the fast-path creation is used.
            /// </summary>
            /// <typeparam name="TDefinition">Definition type for the code.</typeparam>
            /// <param name="key">Code key.</param>
            /// <param name="definition">Definition instance.</param>
            /// <param name="metadata">Optional metadata for the code.</param>
            /// <param name="displayName">Optional display name for the code.</param>
            /// <returns>The builder instance.</returns>
            public Builder WithTypedCode<TDefinition>(
                string key,
                TDefinition definition,
                IMetadata? metadata = null,
                string? displayName = null)
                where TDefinition : ICodeDefinition
            {
                key = Guard.AgainstNullOrWhitespace(key, nameof(key));
                definition = Guard.AgainstNull(definition, nameof(definition));

                // Use fast-path when no metadata/displayName provided
                if (metadata is null && displayName is null)
                {
                    var t = Code<TDefinition>.GetOrCreateFast(key, definition);
                    return WithTypedCode((ICode<ICodeDefinition>)t);
                }

                var typed = Code<TDefinition>.GetOrCreate(
                    key,
                    definition,
                    Guard.AgainstNull(metadata, nameof(metadata)),
                    Guard.AgainstNull(displayName, nameof(displayName)));
                return WithTypedCode((ICode<ICodeDefinition>)typed);
            }

            /// <summary>
            /// Builds the immutable <see cref="Error"/> instance containing the configured values.
            /// </summary>
            /// <returns>A constructed <see cref="Error"/>.</returns>
            public Error Build()
            {

                var diagnostic = _diagnosticBuilder?.Build() ?? Metadata.Empty;

                return new Error(
                    _codeKey,
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
