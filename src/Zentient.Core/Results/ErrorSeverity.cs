// <copyright file="ErrorSeverity.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

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