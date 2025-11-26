// <copyright file="ICode.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using Zentient.Metadata;

namespace Zentient.Codes
{
    /// <summary>
    /// Lightweight non-generic code surface used for unqualified code storage on Error.
    /// </summary>
    public interface ICode
    {
        /// <summary>The unique key for the code (e.g. "404", "UNAUTHENTICATED").</summary>
        string Key { get; }

        /// <summary>Optional human-friendly name or description.</summary>
        string? DisplayName { get; }

        /// <summary>Small, stable metadata attached to the code definition (NOT diagnostic metadata).</summary>
        IMetadata Metadata { get; }
    }

    /// <summary>
    /// Covariant typed code that carries a strongly-typed <see cref="ICodeDefinition"/>.
    /// </summary>
    /// <typeparam name="TDefinition">Type of the code definition.</typeparam>
    public interface ICode<out TDefinition> : ICode
        where TDefinition : ICodeDefinition
    {
        /// <summary>The strongly-typed definition instance.</summary>
        TDefinition Definition { get; }
    }
}
