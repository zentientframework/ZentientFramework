namespace Zentient.Codes
{
    using System;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Provides static methods for validating code keys, display names, and code definitions according to configurable
    /// validation options.
    /// </summary>
    /// <remarks>This class is intended for internal use to enforce consistent validation rules for
    /// code-related entities. Validation behavior can be customized at runtime using the provided configuration
    /// methods. All members are thread-safe.</remarks>
    internal static class CodeValidation
    {
        private static volatile CodeValidationOptions s_options = new() { DisableDisplayNameValidation = false, AllowWhitespaceInKey = false, KeyPattern = null };
        private static readonly object s_optionsLock = new();

        /// <summary>
        /// Configures the code validation system with the specified options.
        /// </summary>
        /// <remarks>This method updates the global code validation settings. Subsequent validation
        /// operations will use the provided options until reconfigured.</remarks>
        /// <param name="options">The options to use for code validation. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is null.</exception>
        public static void Configure(CodeValidationOptions options)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));
            lock (s_optionsLock) { s_options = options; }
        }

        /// <summary>
        /// Configures the key validation pattern used for code validation operations.
        /// </summary>
        /// <remarks>The specified pattern is compiled and applied with culture-invariant and single-line
        /// options. This affects how keys are validated in subsequent operations.</remarks>
        /// <param name="pattern">A regular expression pattern that defines the format of valid keys. The pattern must be compatible with .NET
        /// regular expressions.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="pattern"/> is <see langword="null"/>.</exception>
        public static void ConfigureKeyPattern(string pattern)
        {
            if (pattern is null) throw new ArgumentNullException(nameof(pattern));
            Configure(new CodeValidationOptions { KeyPattern = new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline) });
        }

        /// <summary>
        /// Validates that the specified key meets the required format and constraints.
        /// </summary>
        /// <remarks>Validation rules are determined by the current configuration, which may restrict
        /// whitespace or require the key to match a specific pattern.</remarks>
        /// <param name="key">The key to validate. Cannot be null. May be subject to restrictions on whitespace and format depending on
        /// configuration.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="key"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="key"/> is empty, consists only of whitespace, or does not match the required
        /// format.</exception>
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
        /// Validates that the specified display name is not empty or composed solely of whitespace characters.
        /// </summary>
        /// <param name="displayName">The display name to validate. If <paramref name="displayName"/> is <see langword="null"/>, no validation is
        /// performed.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="displayName"/> is an empty string or consists only of whitespace characters.</exception>
        public static void ValidateDisplay(string? displayName)
        {
            if (s_options.DisableDisplayNameValidation) return;
            if (displayName is null) return;
            if (displayName.Length == 0) throw new ArgumentException("Display name cannot be empty.", nameof(displayName));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Display name cannot be whitespace.", nameof(displayName));
        }

        /// <summary>
        /// Validates that the specified code definition is not null.
        /// </summary>
        /// <param name="definition">The code definition to validate. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="definition"/> is null.</exception>
        public static void ValidateDefinition(ICodeDefinition definition)
        {
            if (definition is null) throw new ArgumentNullException(nameof(definition));
        }
    }
}
