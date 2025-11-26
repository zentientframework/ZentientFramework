namespace Zentient.Codes
{
    using System.Text.RegularExpressions;

    /// <summary>
    /// Validation options to tune key/display validation behavior.
    /// </summary>
    public sealed class CodeValidationOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether validation of display names is disabled.
        /// </summary>
        /// <remarks>When set to <see langword="true"/>, display names will not be checked for validity.
        /// This may allow invalid or non-standard display names to be used. Use with caution if display name integrity
        /// is required.</remarks>
        public bool DisableDisplayNameValidation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether whitespace characters are permitted in configuration keys.
        /// </summary>
        /// <remarks>When enabled, keys containing spaces, tabs, or other whitespace characters will be
        /// accepted and processed. Disabling this option enforces stricter key validation, which may help prevent
        /// accidental formatting errors or inconsistencies in configuration files.</remarks>
        public bool AllowWhitespaceInKey { get; set; }

        /// <summary>
        /// Gets or sets the regular expression used to validate or match keys.
        /// </summary>
        public Regex? KeyPattern { get; set; }
    }
}
