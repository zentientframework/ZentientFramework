// <copyright file="IInternalCode.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Codes
{
    /// <summary>
    /// Internal helper used to extract the stored fingerprint without reflection.
    /// Implemented by the canonical <see cref="Code{TDefinition}"/> to provide a fast-path fingerprint.
    /// </summary>
    internal interface IInternalCode
    {
        /// <summary>The cached identity fingerprint of the associated definition object.</summary>
        string? Fingerprint { get; }
    }
}
