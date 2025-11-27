// <copyright file="CodeValidation.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Codes
{
    using System;
    using System.Text.RegularExpressions;
    using Zentient;

    /// <summary>
    /// Static utility class responsible for configuring and enforcing validation rules for 
    /// <see cref="ICode"/> keys and display names.
    /// </summary>
    internal static class CodeValidation
    {
        private static volatile CodeValidationOptions s_options = new() { DisableDisplayNameValidation = false, AllowWhitespaceInKey = false, KeyPattern = null };
        private static readonly object s_optionsLock = new();

        /// <summary>
        /// Configures the global validation rules applied to all new codes.
        /// </summary>
        /// <param name="options">The <see cref="CodeValidationOptions"/> to use.</param>
        /// <exception cref="ArgumentNullException">Thrown if options is null.</exception>
        public static void Configure(CodeValidationOptions options)
        {
            Guard.AgainstNull(options);
            lock (s_optionsLock) { s_options = options; }
        }

        /// <summary>
        /// Shortcut to configure a custom <see cref="Regex"/> pattern for validating all code keys.
        /// </summary>
        /// <param name="pattern">The regex string pattern.</param>
        /// <exception cref="ArgumentNullException">Thrown if pattern is null.</exception>
        public static void ConfigureKeyPattern(string pattern)
        {
            Guard.AgainstNull(pattern);
            Configure(new CodeValidationOptions { KeyPattern = new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline) });
        }

        /// <summary>
        /// Enforces all configured rules on a code's canonical key.
        /// </summary>
        /// <param name="key">The key to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown if key is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the key fails any configured rule (e.g., whitespace, regex mismatch).</exception>
        public static string ValidateKey(string? key)
        {
            // Ensure non-null quickly using Guard to provide consistent DX and messages.
            key = Guard.AgainstNullOrWhitespace(key, nameof(key));

            // If whitespace in key is disallowed, enforce non-empty and non-whitespace.
            if (!s_options.AllowWhitespaceInKey)
            {
                key = Guard.AgainstWhitespace(key, nameof(key))!;
            }

            if (s_options.KeyPattern is not null)
            {
                if (!s_options.KeyPattern.IsMatch(key)) throw new ArgumentException("Key does not match required format.", nameof(key));
            }

            return key;
        }

        /// <summary>
        /// Enforces all configured rules on a code's optional display name.
        /// This validation can be disabled via <see cref="CodeValidationOptions.DisableDisplayNameValidation"/>.
        /// </summary>
        /// <param name="displayName">The display name to validate (can be null).</param>
        /// <exception cref="ArgumentException">Thrown if the display name fails any configured rule.</exception>
        public static string? ValidateDisplay(string? displayName)
        {
            if (s_options.DisableDisplayNameValidation) return displayName;
            if (displayName is null) return null;

            // Guard.AgainstWhitespace will throw for empty/whitespace values.
            displayName = Guard.AgainstWhitespace(displayName, nameof(displayName));
            return displayName;
        }

        /// <summary>
        /// Simple null check validation for a code definition.
        /// </summary>
        /// <param name="definition">The definition object to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown if the definition is null.</exception>
        public static void ValidateDefinition(ICodeDefinition? definition)
        {
            Guard.AgainstNull(definition, nameof(definition));
        }
    }
}
