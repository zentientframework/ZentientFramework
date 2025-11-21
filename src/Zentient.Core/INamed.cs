// <copyright file="INamed.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Core
{
    /// <summary>
    /// A small contract representing a named entity.
    /// </summary>
    public interface INamed
    {
        /// <summary>
        /// Name label for the implementing type or instance.
        /// </summary>
        string Name { get; }
    }
}
