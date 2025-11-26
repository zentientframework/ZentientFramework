// <copyright file="ErrorSeverity.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Errors
{
    /// <summary>
    /// Behavioral severity levels used by error and result semantics.
    /// </summary>
    public enum ErrorSeverity : byte
    {
        /// <summary>Diagnostic/debug level messages.</summary>
        Debug = 0,

        /// <summary>Informational message; no action required.</summary>
        Info = 1,

        /// <summary>Indicates a potential issue or suboptimal condition.</summary>
        Warning = 2,

        /// <summary>An error condition that requires attention.</summary>
        Error = 3,

        /// <summary>Critical/fatal condition that should abort the operation.</summary>
        Fatal = 4,
        Recoverable = 5
    }
}
