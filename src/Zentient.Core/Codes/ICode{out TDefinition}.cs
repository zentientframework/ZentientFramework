// <copyright file="ICode{out TDefinition}.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Codes
{
    /// <summary>
    /// Covariant typed code surface exposing the concrete definition instance.
    /// </summary>
    /// <typeparam name="TDefinition">The definition type implemented by the code.</typeparam>
    public interface ICode<out TDefinition> : ICode
        where TDefinition : ICodeDefinition
    {
        /// <summary>
        /// Gets the canonical definition instance for this code.
        /// </summary>
        TDefinition Definition { get; }
    }
}
