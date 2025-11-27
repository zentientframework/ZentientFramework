// <copyright file="DefaultDefinitionComparer.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Codes
{
    using System;

    /// <summary>
    /// Default implementation of <see cref="ICodeDefinitionComparer"/>.
    /// It considers two definitions equivalent if they either share the same <see cref="ICodeDefinitionFingerprint.IdentityFingerprint"/>
    /// or if they are of the exact same runtime type.
    /// </summary>
    internal sealed class DefaultDefinitionComparer : ICodeDefinitionComparer
    {
        /// <summary>
        /// Checks if two <see cref="ICodeDefinition"/> instances are equivalent for the purpose of code caching.
        /// </summary>
        /// <param name="a">The first definition.</param>
        /// <param name="b">The second definition.</param>
        /// <returns><see cref="true"/> if the definitions are considered equivalent; otherwise, <see cref="false"/>.</returns>
        public bool AreEquivalent(ICodeDefinition a, ICodeDefinition b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is ICodeDefinitionFingerprint fa && b is ICodeDefinitionFingerprint fb)
            {
                // Prefer fingerprint matching for identity (e.g., matching version/config hash)
                return string.Equals(fa.IdentityFingerprint, fb.IdentityFingerprint, StringComparison.Ordinal);
            }
            // Fallback: Two definitions are equivalent if they are the exact same runtime type.
            return a.GetType() == b.GetType();
        }
    }
}
