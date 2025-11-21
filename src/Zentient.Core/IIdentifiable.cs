// <copyright file="IIdentifiable.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Core
{
    using System;

    /// <summary>
    /// Defines a contract for objects that expose a globally unique identifier (<see cref="Guid"/>).
    /// </summary>
    public interface IIdentifiable
    {
        /// <summary>
        /// Gets the unique identifier associated with this instance.
        /// </summary>
        Guid GuidId { get; }
    }
}
