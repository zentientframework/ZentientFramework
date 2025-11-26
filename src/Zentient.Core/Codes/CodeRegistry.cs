namespace Zentient.Codes
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;

    /// <summary>
    /// Central registry and configuration for code canonicalization and lookup.
    /// </summary>
    public static class CodeRegistry
    {
        // Host-configurable cache used for canonicalization.
        internal static ICodeCache Cache { get; private set; } = new DefaultUnboundedCodeCache();

        // Definition comparer used to determine equivalence.
        internal static ICodeDefinitionComparer DefinitionComparer { get; private set; } = new DefaultDefinitionComparer();

        internal static bool IsKeyUniquenessEnforced => Volatile.Read(ref s_requireKeysUniqueAcrossDefinitionTypes!);
        internal static bool RequireKeysUniqueAcrossDefinitionTypes => Volatile.Read(ref s_requireKeysUniqueAcrossDefinitionTypes!);
        internal static ICodeCache CurrentCache => Cache!;

        /// <summary>
        /// Gets a value indicating whether fallback to untrusted types is permitted during type resolution.
        /// </summary>
        /// <remarks>When this property is <see langword="true"/>, the system may allow type resolution to
        /// proceed using types that have not been verified as trusted. This can affect security and reliability in
        /// scenarios where type trust is required. Use caution when enabling this behavior.</remarks>
        public static bool AllowUntrustedTypeFallback => Volatile.Read(ref s_allowUntrustedTypeFallback);

        // Global key index used when strict key uniqueness is enabled.
        private static readonly ConcurrentDictionary<string, Type> s_globalKeyIndex = new(StringComparer.Ordinal);

        // Map from stable hint to a resolver factory
        private static readonly ConcurrentDictionary<string, Func<object?>> s_definitionResolvers = new(StringComparer.Ordinal);

        // Deduplication by fingerprint (typeQualifiedFingerprint -> canonical definition)
        private static readonly ConcurrentDictionary<string, ICodeDefinition> s_definitionByFingerprint = new(StringComparer.Ordinal);

        // Auto resolvers bag (replaceable via atomic exchange)
        private static ConcurrentBag<Func<string, ICodeDefinition?>> s_autoResolvers = new();

        // Host options / flags (volatile to ensure updates are visible across threads)
        private static volatile bool s_allowUntrustedTypeFallback = false;
        private static volatile bool s_requireKeysUniqueAcrossDefinitionTypes = false;

        // Lock object for atomic state transitions (clears)
        private static readonly object s_registryLock = new();

        // Events for diagnostics/auditing
        public static event Action<ICode>? CodeCreated;
        public static event Action<ICode>? CodeReused;
        public static event Action? CodeCacheCleared;
        public static event Action<string>? DefinitionResolved;

        /// <summary>
        /// Raises the CodeCreated event to notify subscribers that a new code instance has been created.
        /// </summary>
        /// <remarks>This method should be called after a new ICode instance is created to allow event
        /// handlers to perform additional processing. If no subscribers are registered to the CodeCreated event, this
        /// method has no effect.</remarks>
        /// <param name="code">The code instance that was created and is being passed to event subscribers. Cannot be null.</param>
        public static void OnCodeCreated(ICode code) => CodeCreated?.Invoke(code);

        /// <summary>
        /// Raises the CodeReused event to notify subscribers that code has been reused.
        /// </summary>
        /// <remarks>This method should be called whenever code reuse occurs to ensure that all event
        /// handlers are properly notified. If no subscribers are attached to the CodeReused event, this method has no
        /// effect.</remarks>
        /// <param name="code">An object that implements the ICode interface, representing the code instance that was reused. Cannot be
        /// null.</param>
        public static void OnCodeReused(ICode code) => CodeReused?.Invoke(code);

        /// <summary>
        /// Raises the event indicating that the code cache has been cleared.
        /// </summary>
        /// <remarks>This method invokes the <c>CodeCacheCleared</c> event if any handlers are attached.
        /// Call this method to notify subscribers that the code cache has been reset or invalidated.</remarks>
        public static void OnCodeCacheCleared() => CodeCacheCleared?.Invoke();

        /// <summary>
        /// Raises the DefinitionResolved event to notify subscribers that a definition has been resolved.
        /// </summary>
        /// <param name="hint">A string containing information or a hint about the resolved definition. This value is passed to event
        /// subscribers and may be null or empty if no additional information is available.</param>
        public static void OnDefinitionResolved(string hint) => DefinitionResolved?.Invoke(hint);

        /// <summary>
        /// Replace the default cache with a host-provided implementation.
        /// </summary>
        public static void ConfigureCache(ICodeCache cache)
        {
            Cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <summary>
        /// Configure registry runtime options.
        /// </summary>
        public static void ConfigureOptions(bool allowUntrustedTypeFallback, bool requireKeysToBeUniqueAcrossDefinitionTypes = false)
        {
            Volatile.Write(ref s_allowUntrustedTypeFallback, allowUntrustedTypeFallback);
            Volatile.Write(ref s_requireKeysUniqueAcrossDefinitionTypes, requireKeysToBeUniqueAcrossDefinitionTypes);
        }

        /// <summary>
        /// Configure the comparer used for code definition comparisons.
        /// </summary>
        public static void ConfigureDefinitionComparer(ICodeDefinitionComparer comparer)
        {
            DefinitionComparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        }

        /// <summary>
        /// Register a resolver factory for a stable hint string.
        /// </summary>
        public static void Register<TDefinition>(string hint, Func<TDefinition> resolver)
            where TDefinition : ICodeDefinition
        {
            if (string.IsNullOrWhiteSpace(hint)) throw new ArgumentException("hint required", nameof(hint));
            if (resolver is null) throw new ArgumentNullException(nameof(resolver));
            s_definitionResolvers[hint] = () => resolver();
        }

        /// <summary>
        /// Register an auto-resolver function. Auto-resolvers are last-chance and best-effort; exceptions are swallowed.
        /// </summary>
        public static void RegisterAutoResolver(Func<string, ICodeDefinition?> resolver)
        {
            if (resolver is null) throw new ArgumentNullException(nameof(resolver));
            s_autoResolvers.Add(resolver);
        }

        /// <summary>
        /// Try to resolve a definition instance by hint.
        /// Fires <see cref="DefinitionResolved"/> when a resolver returns a definition.
        /// </summary>
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
                catch
                {
                    // swallow; auto resolvers are last-chance
                }
            }

            definition = null;
            return false;
        }

        /// <summary>
        /// Retrieves an existing definition associated with the specified hint, or adds a new definition using the
        /// provided resolver if none exists.
        /// </summary>
        public static TDefinition? GetOrAdd<TDefinition>(string hint, Func<TDefinition> resolver)
            where TDefinition : ICodeDefinition
        {
            if (string.IsNullOrWhiteSpace(hint)) throw new ArgumentException("Hint must be non-empty and whitespace-free.", nameof(hint));
            if (resolver is null) throw new ArgumentNullException(nameof(resolver));
            var result = s_definitionResolvers.GetOrAdd(hint, () => resolver());
            return (TDefinition?)result();
        }

        /// <summary>
        /// Get or add canonical definition by type-qualified fingerprint. Returns the canonical instance.
        /// </summary>
        internal static ICodeDefinition GetOrAddDefinitionByFingerprint(Type defType, string fingerprint, ICodeDefinition def)
        {
            var key = $"{defType.FullName}#{fingerprint ?? string.Empty}";
            return s_definitionByFingerprint.GetOrAdd(key, def);
        }

        /// <summary>
        /// Clears registry state and per-type tables. This operation is atomic with respect to registry state.
        /// </summary>
        internal static void ClearAllCaches()
        {
            lock (s_registryLock)
            {
                // Clear per-type tables by instantiating new static tables via reflection is not practical;
                // instead clear known generic CodeTable instances that we can locate via existing resolver map entries.
                s_definitionResolvers.Clear();
                s_definitionByFingerprint.Clear();
                s_autoResolvers = Interlocked.Exchange(ref s_autoResolvers, new ConcurrentBag<Func<string, ICodeDefinition?>>());
                s_globalKeyIndex.Clear();

                // Clear per-TDefinition tables that are currently known via resolvers (best-effort)
                ClearAllPerTypeTables();

                OnCodeCacheCleared();
            }
        }

        // Helper to clear all generic tables referenced via current resolvers or fingerprint maps.
        internal static void ClearAllPerTypeTables()
        {
            // Conservative: clear any tables that may be referenced currently.
            // We iterate keys in s_definitionByFingerprint to extract type names and attempt to clear generic CodeTable via reflection.
            // For simplicity and reliability, clear all known entries in global key index (per-type clearing via CodeTable<T>.Table.Clear())
            // cannot be done for arbitrary T without reflection—so we clear CodeTable caches for the top-level assemblies where types are known
            // (best-effort).
            // Practical hosts that need deterministic clear should call CodeRegistry.ConfigureCache(...) with a cache they control.
            foreach (var kv in s_globalKeyIndex)
            {
                // attempt to clear CodeTable<T> for the registered type if accessible
                var type = kv.Value;
                try
                {
                    var genericType = typeof(CodeTable<>).MakeGenericType(type);
                    var field = genericType.GetField("Table", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                    if (field?.GetValue(null) is System.Collections.IDictionary dict)
                    {
                        dict.Clear();
                    }
                    else if (field?.GetValue(null) is System.Collections.ICollection col)
                    {
                        // nothing: we tried best-effort
                    }
                }
                catch
                {
                    // ignore: clearing per-type tables is best-effort.
                }
            }
        }

        /// <summary>
        /// Configure global key uniqueness enforcement behavior.
        /// </summary>
        /// <param name="requireUnique">When true, no two different definition types may register the same key.</param>
        public static void ConfigureKeyUniqueness(bool requireUnique)
        {
            Volatile.Write(ref s_requireKeysUniqueAcrossDefinitionTypes, requireUnique);
        }

        /// <summary>
        /// Attempts to retrieve a hint string associated with the specified definition type.
        /// </summary>
        /// <remarks>This method searches the registered definition resolvers for a match to the provided
        /// type. If a match is found, the corresponding hint string is returned via the out parameter.</remarks>
        /// <param name="defType">The type of the definition for which to retrieve the hint. Cannot be null.</param>
        /// <param name="hint">When this method returns, contains the hint string associated with the specified definition type, if found;
        /// otherwise, null.</param>
        /// <returns>true if a hint was found for the specified definition type; otherwise, false.</returns>
        public static bool TryGetHintForDefinitionType(Type defType, out string? hint)
        {
            foreach (var kv in s_definitionResolvers)
            {
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
