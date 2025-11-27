// <copyright file="Unit.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Runtime.CompilerServices;

namespace Zentient.Primitives
{
    /// <summary>
    /// Represents "void" in functional constructs.
    /// A singleton, zero-size struct for uniform monadic returns.
    /// </summary>
    public readonly record struct Unit
    {
        /// <summary>
        /// Gets the default value of the <see cref="Unit"/> type.
        /// </summary>
        public static Unit Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return default;
            }
        }
    }
}
