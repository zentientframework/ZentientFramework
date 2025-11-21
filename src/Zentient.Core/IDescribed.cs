// <copyright file="IDescribed.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Core
{
    /// <summary>
    /// A small contract representing an optional description.
    /// </summary>
    public interface IDescribed
    {
        /// <summary>
        /// Descriptive text; may be <c>null</c>.
        /// </summary>
        string? Description { get; }
    }
}
