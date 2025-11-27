// <copyright file="CodeTable{TDefinition}.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Codes
{
    using System;
    using System.Collections.Concurrent;

    /// <summary>
    /// Internal static class serving as the concrete, memory-backed table for storing cached <see cref="ICode{TDefinition}"/> instances
    /// for a specific <typeparamref name="TDefinition"/> type.
    /// </summary>
    /// <typeparam name="TDefinition">The code definition type.</typeparam>
    internal static class CodeTable<TDefinition>
        where TDefinition : ICodeDefinition
    {
        /// <summary>
        /// The thread-safe dictionary used to cache code instances. Keyed by the code's canonical string key.
        /// </summary>
        internal static readonly ConcurrentDictionary<string, ICode<TDefinition>> Table = new(StringComparer.Ordinal);
    }
}
