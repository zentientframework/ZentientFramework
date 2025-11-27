// <copyright file="ICodeDefinition.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Codes
{
    using Zentient.Definitions;
    using Zentient.Errors;

    /// <summary>
    /// Marker interface for types that describe a code definition payload.
    /// </summary>
    /// <remarks>
    /// Implement this interface for types that act as canonical definition objects used to
    /// parameterize a <see cref="Code{TDefinition}"/> instance.
    /// </remarks>
    public interface ICodeDefinition : IDefinition
    {
        /// <summary>
        /// Gets the severity level associated with the current instance.
        /// </summary>
        Severity Severity { get; }
    }
}
