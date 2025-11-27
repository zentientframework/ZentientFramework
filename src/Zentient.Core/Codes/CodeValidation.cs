// <copyright file="CodeValidation.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Codes
{
    using System;
    using System.Text.RegularExpressions;

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
            if (options is null) throw new ArgumentNullException(nameof(options));
            lock (s_optionsLock) { s_options = options; }
        }

        /// <summary>
        /// Shortcut to configure a custom <see cref="Regex"/> pattern for validating all code keys.
        /// </summary>
        /// <param name="pattern">The regex string pattern.</param>
        /// <exception cref="ArgumentNullException">Thrown if pattern is null.</exception>
        public static void ConfigureKeyPattern(string pattern)
        {
            if (pattern is null) throw new ArgumentNullException(nameof(pattern));
            Configure(new CodeValidationOptions { KeyPattern = new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline) });
        }

        /// <summary>
        /// Enforces all configured rules on a code's canonical key.
        /// </summary>
        /// <param name="key">The key to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown if key is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the key fails any configured rule (e.g., whitespace, regex mismatch).</exception>
        public static void ValidateKey(string key)
        {
            if (key is null) throw new ArgumentNullException(nameof(key));
            if (!s_options.AllowWhitespaceInKey)
            {
                if (key.Length == 0) throw new ArgumentException("Key cannot be empty.", nameof(key));
                if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key cannot be whitespace.", nameof(key));
            }

            if (s_options.KeyPattern is not null)
            {
                if (!s_options.KeyPattern.IsMatch(key)) throw new ArgumentException("Key does not match required format.", nameof(key));
            }
        }

        /// <summary>
        /// Enforces all configured rules on a code's optional display name.
        /// This validation can be disabled via <see cref="CodeValidationOptions.DisableDisplayNameValidation"/>.
        /// </summary>
        /// <param name="displayName">The display name to validate (can be null).</param>
        /// <exception cref="ArgumentException">Thrown if the display name fails any configured rule.</exception>
        public static void ValidateDisplay(string? displayName)
        {
            if (s_options.DisableDisplayNameValidation) return;
            if (displayName is null) return;
            if (displayName.Length == 0) throw new ArgumentException("Display name cannot be empty.", nameof(displayName));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Display name cannot be whitespace.", nameof(displayName));
        }

        /// <summary>
        /// Simple null check validation for a code definition.
        /// </summary>
        /// <param name="definition">The definition object to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown if the definition is null.</exception>
        public static void ValidateDefinition(ICodeDefinition definition)
        {
            if (definition is null) throw new ArgumentNullException(nameof(definition));
        }
    }
}
