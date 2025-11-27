// <copyright file="CodeRegistry.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Codes
{
    using System;
    using System.Collections.Concurrent;
    using System.Runtime.CompilerServices;
    using System.Threading;

    /// <summary>
    /// Central static class for managing code definitions, configuring the global cache, 
    /// and handling definition resolution across the application.
    /// </summary>
    public static class CodeRegistry
    {
        // Internal members are generally not documented for public consumption.
        internal static ICodeCache Cache { get; private set; } = new DefaultUnboundedCodeCache();
        internal static ICodeDefinitionComparer DefinitionComparer { get; private set; } = new DefaultDefinitionComparer();

        /// <summary>
        /// Gets a value indicating whether the registry enforces key uniqueness across all definition types.
        /// </summary>
        public static bool IsKeyUniquenessEnforced => s_requireKeysUniqueAcrossDefinitionTypes;

        /// <summary>
        /// Gets a value indicating whether the registry enforces key uniqueness across all definition types.
        /// </summary>
        public static bool RequireKeysUniqueAcrossDefinitionTypes => s_requireKeysUniqueAcrossDefinitionTypes;

        private static readonly ConcurrentDictionary<(Type, string), ICodeDefinition> s_definitionByFingerprint = new();
        private static readonly ConcurrentBag<Action> s_cacheClearActions = new();
        private static readonly ConcurrentDictionary<string, Func<object?>> s_definitionResolvers = new(StringComparer.Ordinal);
        private static ConcurrentBag<Func<string, ICodeDefinition?>> s_autoResolvers = new();

        private static bool s_allowUntrustedTypeFallback = false;
        private static bool s_requireKeysUniqueAcrossDefinitionTypes = false;

        /// <summary>
        /// Event triggered immediately after a new <see cref="ICode"/> instance is created and added to the cache.
        /// </summary>
        public static event Action<ICode>? CodeCreated;

        /// <summary>
        /// Event triggered when an existing <see cref="ICode"/> instance is successfully retrieved (reused) from the cache.
        /// </summary>
        public static event Action<ICode>? CodeReused;

        /// <summary>
        /// Event triggered after all internal caches and tables have been cleared.
        /// </summary>
        public static event Action? CodeCacheCleared;

        /// <summary>
        /// Event triggered when a code definition is successfully resolved using a registration hint.
        /// </summary>
        public static event Action<string>? DefinitionResolved;

        /// <summary>
        /// Gets a value indicating whether the registry is permitted to attempt creating a default 
        /// instance of a <see cref="ICodeDefinition"/> type if resolution fails.
        /// </summary>
        public static bool AllowUntrustedTypeFallback
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Volatile.Read(ref s_allowUntrustedTypeFallback);
            }
        }

        private static readonly object s_registryLock = new();

        /// <summary>Invokes the <see cref="CodeCreated"/> event.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OnCodeCreated(ICode code) => CodeCreated?.Invoke(code);

        /// <summary>Invokes the <see cref="CodeReused"/> event.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OnCodeReused(ICode code) => CodeReused?.Invoke(code);

        /// <summary>Invokes the <see cref="CodeCacheCleared"/> event.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OnCodeCacheCleared() => CodeCacheCleared?.Invoke();

        /// <summary>Invokes the <see cref="DefinitionResolved"/> event.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OnDefinitionResolved(string hint) => DefinitionResolved?.Invoke(hint);

        /// <summary>
        /// Configures the global <see cref="ICodeCache"/> implementation used by all <see cref="Code{TDefinition}"/> instances.
        /// </summary>
        /// <param name="cache">The new cache instance.</param>
        /// <exception cref="ArgumentNullException">Thrown if cache is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConfigureCache(ICodeCache cache)
        {
            Cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <summary>
        /// Configures core runtime options for the code registry.
        /// </summary>
        /// <param name="allowUntrustedTypeFallback">If true, the registry attempts to <c>Activator.CreateInstance</c> a default definition if no factory is registered.</param>
        /// <param name="requireKeysToBeUniqueAcrossDefinitionTypes">If true, enforces that a code key (e.g., "OK") cannot be used for two different definition types (e.g., HttpCodeDefinition and GrpcCodeDefinition).</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConfigureOptions(bool allowUntrustedTypeFallback, bool requireKeysToBeUniqueAcrossDefinitionTypes = false)
        {
            Volatile.Write(ref s_allowUntrustedTypeFallback, allowUntrustedTypeFallback);
            Volatile.Write(ref s_requireKeysUniqueAcrossDefinitionTypes, requireKeysToBeUniqueAcrossDefinitionTypes);
        }

        /// <summary>
        /// Configures the comparison strategy for determining if two <see cref="ICodeDefinition"/> objects are equivalent.
        /// This is critical for cache key generation.
        /// </summary>
        /// <param name="comparer">The new comparer implementation.</param>
        /// <exception cref="ArgumentNullException">Thrown if comparer is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConfigureDefinitionComparer(ICodeDefinitionComparer comparer)
        {
            DefinitionComparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        }

        /// <summary>
        /// Registers a factory function for a specific code definition type, mapped to a string hint (e.g., a known key).
        /// This is the primary mechanism for dependency injection of <see cref="ICodeDefinition"/> instances.
        /// </summary>
        /// <typeparam name="TDefinition">The code definition type being registered.</typeparam>
        /// <param name="hint">A unique string hint used to resolve the definition (e.g., "HTTP_STATUS_CODE_DEFINITION").</param>
        /// <param name="resolver">A factory function that returns the definition instance.</param>
        /// <exception cref="ArgumentException">Thrown if the hint is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown if the resolver is null.</exception>
        public static void Register<TDefinition>(string hint, Func<TDefinition> resolver)
            where TDefinition : ICodeDefinition
        {
            if (string.IsNullOrWhiteSpace(hint)) throw new ArgumentException("hint required", nameof(hint));
            if (resolver is null) throw new ArgumentNullException(nameof(resolver));
            s_definitionResolvers[hint] = () => resolver();
        }

        /// <summary>
        /// Registers an automatic resolver function that attempts to locate a definition based on a provided string hint.
        /// This is an advanced mechanism for dynamic or complex resolution scenarios (e.g., based on naming conventions).
        /// </summary>
        /// <param name="resolver">The resolver function.</param>
        /// <exception cref="ArgumentNullException">Thrown if the resolver is null.</exception>
        public static void RegisterAutoResolver(Func<string, ICodeDefinition?> resolver)
        {
            if (resolver is null) throw new ArgumentNullException(nameof(resolver));
            s_autoResolvers.Add(resolver);
        }

        /// <summary>
        /// Attempts to resolve a code definition based on a provided string hint, first using static registrations, 
        /// then using registered auto-resolvers.
        /// </summary>
        /// <param name="hint">The hint string (e.g., a key, a full type name) to look up.</param>
        /// <param name="definition">When the method returns, contains the resolved definition object; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if a definition was successfully resolved; otherwise, <see langword="false"/>.</returns>
        public static bool TryResolve(string hint, out object? definition)
        {
            if (hint is null) { definition = null; return false; }

            if (s_definitionResolvers.TryGetValue(hint, out var factory))
            {
                definition = factory();
                if (definition is not null) DefinitionResolved?.Invoke(hint);
                return true;
            }

            foreach (var ar in s_autoResolvers)
            {
                try
                {
                    var def = ar(hint);
                    if (def is not null)
                    {
                        DefinitionResolved?.Invoke(hint);
                        definition = def;
                        return true;
                    }
                }
                catch { } // Swallowing exceptions from custom resolvers is typical for this pattern
            }

            definition = null;
            return false;
        }

        // Internal implementation to fetch or cache the canonical definition instance based on its type and fingerprint.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ICodeDefinition GetOrAddDefinitionByFingerprint(Type defType, string fingerprint, ICodeDefinition def)
        {
            var key = (defType, fingerprint ?? string.Empty);
            return s_definitionByFingerprint.GetOrAdd(key, def);
        }

        /// <summary>
        /// Clears all internal caches, including registered resolvers, auto-resolvers, and all per-type code tables.
        /// This should generally only be called during application shutdown or testing.
        /// </summary>
        public static void ClearAllCaches()
        {
            s_definitionResolvers.Clear();
            s_definitionByFingerprint.Clear();
            // Replacing the bag for thread safety (Atomic replacement)
            s_autoResolvers = new ConcurrentBag<Func<string, ICodeDefinition?>>();

            // Clears all CodeTable<TDefinition>.Table instances registered via actions
            foreach (var action in s_cacheClearActions)
            {
                action();
            }

            OnCodeCacheCleared();
        }

        /// <summary>
        /// Clears all per-type code tables (e.g., <c>CodeTable&lt;MyDefinition&gt;.Table</c>).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ClearAllPerTypeTables() => ClearAllCaches();

        /// <summary>
        /// Sets the global requirement for code keys to be unique across all definition types.
        /// </summary>
        /// <param name="requireUnique">If <see langword="true"/>, key uniqueness is enforced globally.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConfigureKeyUniqueness(bool requireUnique)
        {
            Volatile.Write(ref s_requireKeysUniqueAcrossDefinitionTypes, requireUnique);
        }

        /// <summary>
        /// Attempts to find the first registered hint (string) that resolves to a specific <see cref="ICodeDefinition"/> type.
        /// This is an expensive operation intended mainly for diagnostics and advanced scenarios.
        /// </summary>
        /// <param name="defType">The code definition type to search for.</param>
        /// <param name="hint">When the method returns, contains the registration hint; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if a hint was found; otherwise, <see langword="false"/>.</returns>
        public static bool TryGetHintForDefinitionType(Type defType, out string? hint)
        {
            foreach (var kv in s_definitionResolvers)
            {
                // Must invoke the factory, which can be an expensive operation
                var d = kv.Value();
                if (d?.GetType() == defType)
                {
                    hint = kv.Key;
                    return true;
                }
            }
            hint = null;
            return false;
        }
    }
}
