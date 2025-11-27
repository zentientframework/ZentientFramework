// <copyright file="Severity.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Errors
{
    /// <summary>
    /// Represents the severity classification for an <see cref="Error"/>.
    /// The severity guides runtime behavior, policy decisions and reporting (for example whether to retry,
    /// escalate, or abort).
    /// </summary>
    public enum Severity : byte
    {
        /// <summary>
        /// Informational condition that does not indicate a problem; useful for telemetry or user feedback.
        /// </summary>
        Info = 0,

        /// <summary>
        /// A non-fatal condition that indicates a degraded or unexpected situation; the operation may continue.
        /// </summary>
        Warning = 1,

        /// <summary>
        /// A recoverable failure where retry or fallback semantics are appropriate.
        /// </summary>
        Recoverable = 2,

        /// <summary>
        /// A fatal error indicating that the operation must be halted and the condition propagated.
        /// </summary>
        Fatal = 3
    }
}
