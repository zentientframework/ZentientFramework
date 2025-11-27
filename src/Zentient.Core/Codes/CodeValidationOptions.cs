// <copyright file="CodeValidationOptions.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Codes
{
    using System.Text.RegularExpressions;

    /// <summary>
    /// Configuration object for defining global validation rules applied to <see cref="ICode"/> instances.
    /// Configured via <see cref="CodeValidation.Configure(CodeValidationOptions)"/>.
    /// </summary>
    public sealed class CodeValidationOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether checks for empty or whitespace display names should be skipped.
        /// Defaults to <see langword="false"/> (validation is enabled).
        /// </summary>
        public bool DisableDisplayNameValidation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether whitespace characters are allowed within the canonical <see cref="ICode.Key"/>.
        /// Defaults to <see langword="false"/> (whitespace is not allowed).
        /// </summary>
        public bool AllowWhitespaceInKey { get; set; }

        /// <summary>
        /// Gets or sets a specific <see cref="Regex"/> pattern that the canonical <see cref="ICode.Key"/> must match.
        /// If <see langword="null"/>, no pattern matching is performed.
        /// </summary>
        public Regex? KeyPattern { get; set; }
    }
}
