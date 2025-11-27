// <copyright file="Abstractions.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Abstractions
{
    /// <summary>
    /// Provider factory pattern for pluggable runtime capabilities.
    /// </summary>
    /// <typeparam name="TItem">Type produced by the provider.</typeparam>
    public interface IProvider<out TItem>
        where TItem : class
    {
        /// <summary>
        /// Unique provider identifier.
        /// </summary>
        string ProviderId { get; }

        /// <summary>
        /// Create an instance of the provided capability.
        /// </summary>
        /// <returns>An instance of <typeparamref name="TItem"/>.</returns>
        TItem Create();
    }
}
