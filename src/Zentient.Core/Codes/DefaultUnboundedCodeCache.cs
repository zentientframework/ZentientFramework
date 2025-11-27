// <copyright file="DefaultUnboundedCodeCache.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Codes
{
    using System;
    using System.Collections.Concurrent;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// The default unbounded in-memory cache implementation.
    /// It uses a thread-safe <see cref="ConcurrentDictionary{TKey, TValue}"/> for each code definition type
    /// and never automatically evicts items.
    /// </summary>
    internal sealed class DefaultUnboundedCodeCache : ICodeCache
    {
        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet<TDefinition>(string key, out ICode<TDefinition>? code) where TDefinition : ICodeDefinition
            => CodeTable<TDefinition>.Table.TryGetValue(key, out code);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ICode<TDefinition> AddOrGet<TDefinition>(string key, Func<string, ICode<TDefinition>> factory) where TDefinition : ICodeDefinition
            => CodeTable<TDefinition>.Table.GetOrAdd(key, factory);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear<TDefinition>() where TDefinition : ICodeDefinition
            => CodeTable<TDefinition>.Table.Clear();

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearAll()
            => CodeRegistry.ClearAllPerTypeTables();
    }
}
