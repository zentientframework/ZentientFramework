namespace Zentient.Codes
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using Zentient.Metadata;

    /// <summary>
    /// Canonical, cached, immutable code implementation.
    /// </summary>
    /// <typeparam name="TDefinition">The definition type used by the code.</typeparam>
    [DebuggerDisplay("{Key,nq}")]
    public sealed class Code<TDefinition> : ICode<TDefinition>, IInternalCode
        where TDefinition : ICodeDefinition
    {
        private static ICodeCache Cache => CodeRegistry.Cache;

        // Fingerprint cached for this instance
        private readonly string? _fingerprint;

        // precomputed hash code
        private readonly int _hashCode;

        /// <inheritdoc/>
        public string Key { get; }

        /// <inheritdoc/>
        public string? DisplayName { get; }

        /// <inheritdoc/>
        public IMetadata Metadata { get; }

        /// <inheritdoc/>
        public TDefinition Definition { get; }

        /// <inheritdoc/>
        public Type DefinitionType => typeof(TDefinition);

        /// <inheritdoc/>
        string? IInternalCode.Fingerprint => _fingerprint;

        /// <summary>
        /// Initializes a new instance of the Code class with the specified key, definition, metadata, and optional
        /// display name.
        /// </summary>
        /// <param name="key">The unique key that identifies the code instance. Cannot be null or empty.</param>
        /// <param name="definition">The definition object that describes the code. Cannot be null.</param>
        /// <param name="metadata">The metadata associated with the code instance. If null, an empty metadata object is used.</param>
        /// <param name="displayName">An optional display name for the code instance. May be null.</param>
        internal Code(string key, TDefinition definition, IMetadata metadata, string? displayName)
        {
            CodeValidation.ValidateKey(key);
            CodeValidation.ValidateDefinition(definition);
            CodeValidation.ValidateDisplay(displayName);

            Key = StringPool.Get(key);
            Definition = definition;
            Metadata = metadata ?? Zentient.Metadata.Metadata.Empty;
            DisplayName = displayName;
            _fingerprint = ComputeDefinitionFingerprint(definition);
            _hashCode = ComputeHashCode();
        }

        private int ComputeHashCode()
        {
            unchecked
            {
                var hc = StringComparer.Ordinal.GetHashCode(Key);
                hc = (hc * 397) ^ DefinitionType.GetHashCode();
                hc = (hc * 397) ^ (_fingerprint?.GetHashCode(StringComparison.Ordinal) ?? 0);
                return hc;
            }
        }

        private static string? ComputeDefinitionFingerprint(TDefinition definition)
        {
            if (definition is ICodeDefinitionFingerprint f) return f.IdentityFingerprint;
            return definition.GetType().FullName;
        }

        /// <inheritdoc/>
        public override string ToString() => Key;

        /// <summary>
        /// Get or create canonical instance per (TDefinition, key) using metadata (default).
        /// </summary>
        public static ICode<TDefinition> GetOrCreate(
            string key,
            TDefinition definition,
            IMetadata? metadata = null,
            string? displayName = null,
            Func<TDefinition, TDefinition, bool>? equivalenceComparer = null,
            bool allowOverride = false)
        {
            // Validate inputs early
            CodeValidation.ValidateKey(key);
            CodeValidation.ValidateDefinition(definition);
            CodeValidation.ValidateDisplay(displayName);

            // Fast local cache reference
            var cache = Cache;

            if (cache.TryGet<TDefinition>(key, out var existing))
            {
                // If there's an equivalence comparer provided, use it; otherwise use registry comparer.
                if (equivalenceComparer != null)
                {
                    if (!equivalenceComparer(existing!.Definition, definition))
                    {
                        if (!allowOverride)
                        {
                            throw new InvalidOperationException($"A Code with key '{key}' already exists for definition type '{typeof(TDefinition).FullName}' with a non-equivalent definition.");
                        }
                        // allowOverride == true: we do not replace existing instance in this cache implementation.
                        // Hosts that require replacement should provide a custom ICodeCache implementation.
                    }
                }
                else
                {
                    if (!CodeRegistry.DefinitionComparer.AreEquivalent(existing!.Definition!, definition))
                    {
                        if (!allowOverride)
                        {
                            throw new InvalidOperationException($"A Code with key '{key}' already exists for definition type '{typeof(TDefinition).FullName}' with a different definition. Provide an equivalence comparer or enable allowOverride (note: allowOverride does not replace cached instances in the default cache).");
                        }
                        // allowOverride == true: same note as above.
                    }
                }

                // Notify reuse and return existing canonical instance
                CodeRegistry.OnCodeReused(existing);
                return existing;
            }

            // Deduplicate definition by fingerprint before creating the Code instance to reduce memory.
            var fp = ComputeDefinitionFingerprint(definition) ?? string.Empty;
            var canonicalDef = (TDefinition)CodeRegistry.GetOrAddDefinitionByFingerprint(typeof(TDefinition), fp, definition);

            var created = new Code<TDefinition>(key, canonicalDef, metadata ?? Zentient.Metadata.Metadata.Empty, displayName);
            var added = cache.AddOrGet<TDefinition>(created.Key, created);

            // If the returned instance is the one we created then it's new; otherwise it's a reused existing.
            if (ReferenceEquals(added, created))
            {
                CodeRegistry.OnCodeCreated(added);
            }
            else
            {
                CodeRegistry.OnCodeReused(added);
            }

            return added;
        }

        /// <summary>
        /// Fast-path GetOrCreate that avoids metadata merging and allocations beyond lookup/creation.
        /// </summary>
        public static ICode<TDefinition> GetOrCreateFast(string key, TDefinition definition)
        {
            CodeValidation.ValidateKey(key);
            CodeValidation.ValidateDefinition(definition);

            if (Cache.TryGet<TDefinition>(key, out var existing))
            {
                CodeRegistry.OnCodeReused(existing);
                return existing;
            }

            // Deduplicate by fingerprint quickly
            var fp = ComputeDefinitionFingerprint(definition) ?? string.Empty;
            var canonicalDef = (TDefinition)CodeRegistry.GetOrAddDefinitionByFingerprint(typeof(TDefinition), fp, definition);

            var created = new Code<TDefinition>(key, canonicalDef, Zentient.Metadata.Metadata.Empty, null);
            var added = Cache.AddOrGet<TDefinition>(created.Key, created);
            CodeRegistry.OnCodeCreated(added);
            return added;
        }

        /// <summary>
        /// TryGetOrCreate variant that avoids exceptions.
        /// </summary>
        public static bool TryGetOrCreate(string key, TDefinition definition, out ICode<TDefinition> code, bool allowOverride = false)
        {
            try
            {
                code = GetOrCreate(key, definition, Zentient.Metadata.Metadata.Empty, null, null, allowOverride);
                return true;
            }
            catch
            {
                code = default!;
                return false;
            }
        }

        /// <summary>
        /// TryCreate only when a code doesn't exist. Returns false if an existing code is present.
        /// </summary>
        public static bool TryCreate(string key, TDefinition definition, out ICode<TDefinition> code)
        {
            CodeValidation.ValidateKey(key);
            CodeValidation.ValidateDefinition(definition);

            if (Cache.TryGet<TDefinition>(key, out var existing))
            {
                code = existing;
                return false;
            }

            var fp = ComputeDefinitionFingerprint(definition) ?? string.Empty;
            var canonicalDef = (TDefinition)CodeRegistry.GetOrAddDefinitionByFingerprint(typeof(TDefinition), fp, definition);
            var created = new Code<TDefinition>(key, canonicalDef, Zentient.Metadata.Metadata.Empty, null);
            code = Cache.AddOrGet<TDefinition>(created.Key, created);
            CodeRegistry.OnCodeCreated(code);
            return true;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (obj is ICode other)
            {
                if (!string.Equals(Key, other.Key, StringComparison.Ordinal)) return false;
                if (DefinitionType != other.DefinitionType) return false;

                // fast fingerprint extraction via internal interface
                if (other is IInternalCode ic)
                {
                    return string.Equals(_fingerprint, ic.Fingerprint, StringComparison.Ordinal);
                }

                // Fallback: same key + same definition type is considered equal in absence of fingerprints.
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode() => _hashCode;

        // Internal small string pool to reduce repeated allocations for high-cardinality systems.
        private static class StringPool
        {
            private static readonly ConcurrentDictionary<string, string> s_pool = new(StringComparer.Ordinal);
            public static string Get(string key)
            {
                if (key is null) throw new ArgumentNullException(nameof(key));
                if (key.Length == 0) throw new ArgumentException("Key cannot be empty.", nameof(key));
                if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key cannot be whitespace.", nameof(key));
                if (s_pool.TryGetValue(key, out var existing)) return existing;
                return s_pool.GetOrAdd(key, key);
            }
        }
    }
}
