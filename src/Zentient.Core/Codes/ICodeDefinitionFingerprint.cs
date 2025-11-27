// <copyright file="ICodeDefinitionFingerprint.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Codes
{
    /// <summary>
    /// Optional interface that can be implemented by an <see cref="ICodeDefinition"/> to provide a stable, 
    /// unique identity fingerprint (e.g., a hash, version number).
    /// This is used by <see cref="CodeRegistry"/> to ensure multiple code instances use the same canonical definition object.
    /// </summary>
    public interface ICodeDefinitionFingerprint : ICodeDefinition
    {
        /// <summary>
        /// A string that uniquely identifies the version or configuration of the definition object.
        /// </summary>
        string IdentityFingerprint { get; }
    }
}
