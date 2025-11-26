// <copyright file="FileName.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Errors
{
    using System;
    using Zentient.Codes;
    using Zentient.Metadata;
    using Zentient.Core;
    using Zentient.Definitions; // Added for ICodeDefinition

    /// <summary>
    /// Immutable, structured error envelope that carries a stable code, human-readable message,
    /// severity and optional diagnostic metadata and exception. Construct instances via the
    /// <see cref="Builder"/> or the provided static factories.
    /// </summary>
    public sealed class Error
    {
        /// <summary>
        /// The strongly-typed error code instance. This is the primary identifier for the error
        /// and may carry small, stable metadata and a display name.
        /// </summary>
        public ICode Code { get; }

        /// <summary>
        /// Convenience access to the string key for the error code (equivalent to <see cref="Code"/>.<see cref="ICode.Key"/>).
        /// </summary>
        public string CodeKey => Code.Key;

        /// <summary>
        /// Human-readable message describing the error. Must be non-null.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Behavioral severity that describes how calling code should treat the error (recoverable, fatal, etc.).
        /// </summary>
        public ErrorSeverity Severity { get; }

        /// <summary>
        /// Diagnostic metadata snapshot associated with this error. Intended for logging/tracing and diagnostics.
        /// The metadata is immutable and may be empty.
        /// </summary>
        public IMetadata DiagnosticMetadata { get; }

        /// <summary>
        /// Optional exception associated with the error. May be <c>null</c> when not applicable.
        /// The exception is preserved for diagnostic purposes only and is not required for equality or serialization.
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// Internal constructor used by the <see cref="Builder"/> and the static factories to create an <see cref="Error"/>.
        /// </summary>
        /// <param name="code">The strongly-typed <see cref="ICode"/> that identifies the error. Must not be <c>null</c>.</param>
        /// <param name="message">Human readable message. Must not be <c>null</c>.</param>
        /// <param name="severity">Error severity.</param>
        /// <param name="diagnosticMetadata">Diagnostic metadata snapshot; when <c>null</c> an empty snapshot is used.</param>
        /// <param name="exception">Optional exception to attach for diagnostics.</param>
        internal Error(ICode code, string message, ErrorSeverity severity, IMetadata diagnosticMetadata, Exception? exception = null)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Severity = severity;
            DiagnosticMetadata = diagnosticMetadata ?? Zentient.Metadata.Metadata.Empty;
            Exception = exception;
        }

        /// <summary>
        /// Create a new <see cref="Builder"/> seeded with the provided <see cref="ICode"/> and message.
        /// Use the returned builder to further customize metadata, severity and exception before calling <see cref="Builder.Build"/>.
        /// </summary>
        /// <param name="code">Initial code instance. Must not be <c>null</c>.</param>
        /// <param name="message">Initial message. Must not be <c>null</c>.</param>
        /// <returns>A new <see cref="Builder"/> instance.</returns>
        public static Builder NewBuilder(ICode code, string message) => new(code, message);

        /// <summary>
        /// Internal static helper that contains pre-defined <see cref="ICode"/> instances used by the built-in
        /// error factories (Validation, NotFound, Conflict, Unexpected, Canceled).
        /// </summary>
        private static class ErrorDefinitions
        {
            /// <summary>
            /// Private concrete, minimal definition implementing <see cref="ICodeDefinition"/> used to seed built-in codes.
            /// </summary>
            /// <remarks>
            /// This record is an internal, compact representation used solely to satisfy the <see cref="ICodeDefinition"/>
            /// contract for built-in error codes.
            /// </remarks>
            private sealed record GeneralErrorDefinition(
                Guid GuidId,
                string Key,
                string DisplayName,
                string? Description,
                IMetadata Tags) : ICodeDefinition;

            /// <summary>
            /// A shared, minimal code definition used for the bundled error codes.
            /// </summary>
            public static readonly ICodeDefinition GeneralError = new GeneralErrorDefinition(
                Guid.NewGuid(),
                "ZENTIENT_GENERAL_ERROR_DEF",
                "General Error Definition",
                "A general-purpose definition for built-in error codes.",
                Zentient.Metadata.Metadata.Empty);

            /// <summary>Prebuilt validation error code (key = "VALIDATION").</summary>
            public static readonly ICode ValidationCode = new Zentient.Codes.Code.CodeBuilder<ICodeDefinition>()
                .WithDefinition(GeneralError)
                .WithKey("VALIDATION")
                .WithDisplayName("Validation Error")
                .Build();

            /// <summary>Prebuilt not-found error code (key = "NOT_FOUND").</summary>
            public static readonly ICode NotFoundCode = new Zentient.Codes.Code.CodeBuilder<ICodeDefinition>()
                .WithDefinition(GeneralError)
                .WithKey("NOT_FOUND")
                .WithDisplayName("Not Found")
                .Build();

            /// <summary>Prebuilt conflict error code (key = "CONFLICT").</summary>
            public static readonly ICode ConflictCode = new Zentient.Codes.Code.CodeBuilder<ICodeDefinition>()
                .WithDefinition(GeneralError)
                .WithKey("CONFLICT")
                .WithDisplayName("Conflict")
                .Build();

            /// <summary>Prebuilt unexpected error code (key = "UNEXPECTED").</summary>
            public static readonly ICode UnexpectedCode = new Zentient.Codes.Code.CodeBuilder<ICodeDefinition>()
                .WithDefinition(GeneralError)
                .WithKey("UNEXPECTED")
                .WithDisplayName("Unexpected Error")
                .Build();

            /// <summary>Prebuilt canceled error code (key = "CANCELED").</summary>
            public static readonly ICode CanceledCode = new Zentient.Codes.Code.CodeBuilder<ICodeDefinition>()
                .WithDefinition(GeneralError)
                .WithKey("CANCELED")
                .WithDisplayName("Operation Canceled")
                .Build();
        }

        /// <summary>
        /// Factory creating a validation <see cref="Error"/> with the provided message.
        /// Severity is set to <see cref="ErrorSeverity.Recoverable"/>.
        /// </summary>
        /// <param name="message">Human readable message describing the validation failure.</param>
        /// <returns>An <see cref="Error"/> instance representing the validation failure.</returns>
        public static Error Validation(string message) => NewBuilder(ErrorDefinitions.ValidationCode, message).WithSeverity(ErrorSeverity.Recoverable).Build();

        /// <summary>
        /// Factory creating a not-found <see cref="Error"/> for the specified key.
        /// Adds a metadata entry "NotFoundKey" containing the requested key and sets severity to <see cref="ErrorSeverity.Recoverable"/>.
        /// </summary>
        /// <param name="key">The requested key that could not be found.</param>
        /// <returns>An <see cref="Error"/> instance representing the not-found condition.</returns>
        public static Error NotFound(string key) => NewBuilder(ErrorDefinitions.NotFoundCode, $"The requested item '{key}' was not found.")
            .WithSeverity(ErrorSeverity.Recoverable)
            .WithMetadata(m => m.Set("NotFoundKey", key))
            .Build();

        /// <summary>
        /// Factory creating a conflict <see cref="Error"/> with the provided message.
        /// Severity is set to <see cref="ErrorSeverity.Recoverable"/>.
        /// </summary>
        /// <param name="message">Message describing the conflict.</param>
        /// <returns>An <see cref="Error"/> representing the conflict outcome.</returns>
        public static Error Conflict(string message) => NewBuilder(ErrorDefinitions.ConflictCode, message).WithSeverity(ErrorSeverity.Recoverable).Build();

        /// <summary>
        /// Factory wrapping an unexpected exception into an <see cref="Error"/>.
        /// The provided exception is attached to the returned <see cref="Error"/>. Severity is set to <see cref="ErrorSeverity.Fatal"/>.
        /// </summary>
        /// <param name="ex">Exception that caused the failure.</param>
        /// <returns>An <see cref="Error"/> representing the unexpected failure.</returns>
        public static Error Unexpected(Exception ex) => NewBuilder(ErrorDefinitions.UnexpectedCode, ex?.Message ?? "Unexpected error")
            .WithException(ex)
            .WithSeverity(ErrorSeverity.Fatal)
            .Build();

        /// <summary>
        /// A cancellation sentinel <see cref="Error"/> instance. Severity is set to <see cref="ErrorSeverity.Recoverable"/>.
        /// </summary>
        public static Error Canceled => NewBuilder(ErrorDefinitions.CanceledCode, "The operation was canceled.").WithSeverity(ErrorSeverity.Recoverable).Build();

        /// <summary>
        /// Mutable builder that is used to compose and construct an immutable <see cref="Error"/>.
        /// Use fluent methods to configure code, message, severity, exception and diagnostic metadata, then call <see cref="Build"/>.
        /// </summary>
        public sealed class Builder
        {
            private ICode _code;
            private string _message;
            private ErrorSeverity _severity;
            private Exception? _exception;
            private readonly Zentient.Metadata.Metadata.Builder _diagnostic = Zentient.Metadata.Metadata.NewBuilder();

            /// <summary>
            /// Creates a new builder seeded with the provided <paramref name="code"/> and <paramref name="message"/>.
            /// </summary>
            /// <param name="code">Initial <see cref="ICode"/> instance. Must not be <c>null</c>.</param>
            /// <param name="message">Initial message. Must not be <c>null</c>.</param>
            internal Builder(ICode code, string message)
            {
                _code = code ?? throw new ArgumentNullException(nameof(code));
                _message = message ?? throw new ArgumentNullException(nameof(message));
                _severity = ErrorSeverity.Fatal;
            }

            /// <summary>
            /// Replace the code associated with the builder.
            /// </summary>
            /// <param name="code">The new <see cref="ICode"/> to use.</param>
            /// <returns>The same builder instance (fluent API).</returns>
            public Builder WithCode(ICode code)
            {
                _code = code ?? throw new ArgumentNullException(nameof(code));
                return this;
            }

            /// <summary>
            /// Create or attach a strongly-typed code using the <see cref="Code{TDefinition}.GetOrCreate"/> factory.
            /// </summary>
            /// <typeparam name="TDef">Type of the code definition conforming to <see cref="ICodeDefinition"/>.</typeparam>
            /// <param name="key">Canonical string key for the code.</param>
            /// <param name="definition">Typed definition instance used to back the code.</param>
            /// <param name="metadata">Optional small metadata for the code definition.</param>
            /// <param name="displayName">Optional display name.</param>
            /// <returns>The same builder instance (fluent API).</returns>
            public Builder WithCode<TDef>(string key, TDef definition, IMetadata? metadata = null, string? displayName = null)
                where TDef : ICodeDefinition
            {
                _code = Code<TDef>.GetOrCreate(key, definition, metadata, displayName);
                return this;
            }

            /// <summary>
            /// Set or replace the human-readable message for the error.
            /// </summary>
            /// <param name="message">Message text. Must not be <c>null</c>.</param>
            /// <returns>The same builder instance (fluent API).</returns>
            public Builder WithMessage(string message)
            {
                _message = message ?? throw new ArgumentNullException(nameof(message));
                return this;
            }

            /// <summary>
            /// Set the error severity.
            /// </summary>
            /// <param name="severity">Severity value.</param>
            /// <returns>The same builder instance (fluent API).</returns>
            public Builder WithSeverity(ErrorSeverity severity)
            {
                _severity = severity;
                return this;
            }

            /// <summary>
            /// Attach an exception instance for diagnostics.
            /// </summary>
            /// <param name="exception">Exception to attach; may be <c>null</c> to clear.</param>
            /// <returns>The same builder instance (fluent API).</returns>
            public Builder WithException(Exception? exception)
            {
                _exception = exception;
                return this;
            }

            /// <summary>
            /// Configure diagnostic metadata using the mutable metadata builder. The provided <paramref name="configure"/>
            /// action receives a <see cref="Zentient.Metadata.Metadata.Builder"/> to set key/value pairs.
            /// </summary>
            /// <param name="configure">Action that configures diagnostic metadata; may be <c>null</c>.</param>
            /// <returns>The same builder instance (fluent API).</returns>
            public Builder WithMetadata(Action<Zentient.Metadata.Metadata.Builder> configure)
            {
                configure?.Invoke(_diagnostic);
                return this;
            }

            /// <summary>
            /// Build the immutable <see cref="Error"/> instance from the current builder state.
            /// </summary>
            /// <returns>An immutable <see cref="Error"/> instance.</returns>
            public Error Build() => new(_code, _message, _severity, _diagnostic.Build(), _exception);
        }
    }
}

#if false
namespace Zentient.Errors
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    using Zentient.Metadata;

    /// <summary>
    /// Minimal, value-like error envelope used by the <see cref="Result{T}"/> type.
    /// Lightweight and intentionally simple to keep allocations small on hot paths.
    /// </summary>
    public sealed class Error : IEquatable<Error>
    {
        /// <summary>Machine stable code identifying the error category.</summary>
        public string Code { get; }

        /// <summary>Human visible message describing this error instance.</summary>
        public string Message { get; }

        /// <summary>Behavioral severity for this error instance.</summary>
        public ErrorSeverity Severity { get; }

        private Error(string code, string message, ErrorSeverity severity)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Severity = severity;
        }

        /// <summary>Create a new <see cref="Error"/> instance.</summary>
        /// <param name="code">Stable error code.</param>
        /// <param name="message">Human readable message.</param>
        /// <param name="severity">Severity level.</param>
        /// <returns>Constructed <see cref="Error"/>.</returns>
        public static Error Create(string code, string message, ErrorSeverity severity = ErrorSeverity.Error) => new Error(code, message, severity);

        /// <summary>Small builder used for ergonomic test/writer DX.</summary>
        public sealed class Builder
        {
            private string _code;
            private string _message;
            private ErrorSeverity _severity;

            /// <summary>Create a new builder pre-seeded with code and message.</summary>
            /// <param name="code">Initial code.</param>
            /// <param name="message">Initial message.</param>
            public Builder(string code, string message)
            {
                _code = code ?? "UNKNOWN";
                _message = message ?? string.Empty;
                _severity = ErrorSeverity.Error;
            }

            /// <summary>Set the error code.</summary>
            public Builder SetCode(string code) { _code = code ?? _code; return this; }

            /// <summary>Set the error message.</summary>
            public Builder SetMessage(string message) { _message = message ?? _message; return this; }

            /// <summary>Set the error severity.</summary>
            public Builder SetSeverity(ErrorSeverity severity) { _severity = severity; return this; }

            /// <summary>Build the <see cref="Error"/> instance.</summary>
            public Error Build() => new Error(_code, _message, _severity);
        }

        /// <inheritdoc/>
        public override string ToString() => $"{Code}: {Message} ({Severity})";

        /// <inheritdoc/>
        public bool Equals(Error? other) => other is not null && (ReferenceEquals(this, other) || (other.Code == Code && other.Message == Message && other.Severity == Severity));

        /// <inheritdoc/>
        public override bool Equals(object? obj) => Equals(obj as Error);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Code, Message, (int)Severity);
    }
}
#endif
