// <copyright file="Code{TDefinition}.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Codes
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using Zentient.Metadata;

    /// <summary>
    /// The immutable, canonical representation of a structured code instance.
    /// This type is the core value object managed by the <see cref="CodeRegistry"/> and its cache.
    /// </summary>
    /// <typeparam name="TDefinition">The domain-specific definition type associated with this code.</typeparam>
    [DebuggerDisplay("{Key,nq}")]
    public sealed class Code<TDefinition> : ICode<TDefinition>, IInternalCode
        where TDefinition : ICodeDefinition
    {
        private static ICodeCache Cache => CodeRegistry.Cache;
        private readonly string? _fingerprint;
        private readonly int _hashCode;

        /// <inheritdoc/>
        public string Key { get; }

        /// <inheritdoc/>
        public string? DisplayName { get; }

        /// <inheritdoc/>
        public IMetadata Metadata { get; }

        /// <summary>
        /// The domain-specific definition object that characterizes this code instance.
        /// </summary>
        public TDefinition Definition { get; }

        /// <inheritdoc/>
        public Type DefinitionType
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return typeof(TDefinition);
            }
        }

        // Internal property for fast fingerprint access without reflection
        string? IInternalCode.Fingerprint
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _fingerprint;
            }
        }

        // Internal constructor used by the GetOrCreate factory
        internal Code(string key, TDefinition definition, IMetadata metadata, string? displayName)
        {
            CodeValidation.ValidateKey(key);
            CodeValidation.ValidateDefinition(definition);
            CodeValidation.ValidateDisplay(displayName);

            // Optimization: StringPool ensures only one instance of the key string exists globally
            Key = StringPool.Get(key);
            Definition = definition;
            Metadata = metadata ?? Zentient.Metadata.Metadata.Empty;
            DisplayName = displayName;

            _fingerprint = ComputeDefinitionFingerprint(definition);
            _hashCode = ComputeHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        // Determines the canonical identity of the definition object.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string? ComputeDefinitionFingerprint(TDefinition definition)
        {
            if (definition is ICodeDefinitionFingerprint f) return f.IdentityFingerprint;
            return definition.GetType().FullName; // Fallback to full type name
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => Key;

        /// <summary>
        /// The primary factory method for creating or retrieving a canonical <see cref="ICode{TDefinition}"/> instance.
        /// This method guarantees that for a given <paramref name="key"/> and equivalent <paramref name="definition"/>, 
        /// only a single instance exists in the cache (the "canonical" instance).
        /// </summary>
        /// <param name="key">The unique canonical key for the code.</param>
        /// <param name="definition">The domain definition to associate with the code.</param>
        /// <param name="metadata">Optional diagnostic metadata.</param>
        /// <param name="displayName">Optional human-readable display name.</param>
        /// <param name="equivalenceComparer">Optional custom comparer to check if a new definition is equivalent to an existing one.</param>
        /// <param name="allowOverride">If <see langword="true"/> and a non-equivalent definition is detected, the operation proceeds (but the cached instance is not replaced in the default cache).</param>
        /// <returns>The canonical cached <see cref="ICode{TDefinition}"/> instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown if an existing code is found with a non-equivalent definition and <paramref name="allowOverride"/> is <see langword="false"/>.</exception>
        public static ICode<TDefinition> GetOrCreate(
            string key,
            TDefinition definition,
            IMetadata? metadata = null,
            string? displayName = null,
            Func<TDefinition, TDefinition, bool>? equivalenceComparer = null,
            bool allowOverride = false)
        {
            CodeValidation.ValidateKey(key);
            CodeValidation.ValidateDefinition(definition);
            CodeValidation.ValidateDisplay(displayName);

            var cache = Cache;

            // 1. Check cache for key (Fast Path)
            if (cache.TryGet<TDefinition>(key, out var existing))
            {
                // 2. Validate definition equivalence if a hit occurs
                bool equivalent = equivalenceComparer != null
                    ? equivalenceComparer(existing!.Definition, definition)
                    : CodeRegistry.DefinitionComparer.AreEquivalent(existing!.Definition!, definition);

                if (!equivalent && !allowOverride)
                {
                    throw new InvalidOperationException($"A Code with key '{key}' already exists for definition type '{typeof(TDefinition).FullName}' with a different definition. Provide an equivalence comparer or enable allowOverride (note: allowOverride does not replace cached instances in the default cache).");
                }

                CodeRegistry.OnCodeReused(existing);
                return existing;
            }

            // 3. Definition Fingerprinting (Slow Path: Cache miss)
            var fp = ComputeDefinitionFingerprint(definition) ?? string.Empty;
            // Get or add the canonical *definition* instance for this type/fingerprint
            var canonicalDef = (TDefinition)CodeRegistry.GetOrAddDefinitionByFingerprint(typeof(TDefinition), fp, definition);

            // 4. Create and cache the canonical *code* instance
            var created = new Code<TDefinition>(key, canonicalDef, metadata ?? Zentient.Metadata.Metadata.Empty, displayName);
            var added = cache.AddOrGet<TDefinition>(created.Key, _ => created);

            // 5. Fire events based on whether we won the race (ReferenceEquals)
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
        /// Creates a new <see cref="CodeBuilder{TDefinition}"/> instance initialized with the current definition.
        /// </summary>
        /// <returns>A <see cref="CodeBuilder{TDefinition}"/> that can be used to further configure or modify the current
        /// definition.</returns>
        public CodeBuilder<TDefinition> ToBuilder() => Code.NewBuilder<TDefinition>(this);

        /// <summary>
        /// A lightweight, performance-optimized factory method for creating or retrieving a canonical instance, 
        /// which avoids the overhead of optional parameters (metadata, display name, custom comparers).
        /// </summary>
        /// <param name="key">The unique canonical key for the code.</param>
        /// <param name="definition">The domain definition to associate with the code.</param>
        /// <returns>The canonical cached <see cref="ICode{TDefinition}"/> instance.</returns>
        public static ICode<TDefinition> GetOrCreateFast(string key, TDefinition definition)
        {
            CodeValidation.ValidateKey(key);
            CodeValidation.ValidateDefinition(definition);

            if (Cache.TryGet<TDefinition>(key, out var existing) && existing is ICode<TDefinition> existingDefinition)
            {
                CodeRegistry.OnCodeReused(existingDefinition);
                return existing;
            }

            var fp = ComputeDefinitionFingerprint(definition) ?? string.Empty;
            TDefinition canonicalDefinition = (TDefinition)CodeRegistry.GetOrAddDefinitionByFingerprint(typeof(TDefinition), fp, definition);

            // Optimization: factory delegate for AddOrGet minimizes allocations on cache miss
            var added = Cache.AddOrGet<TDefinition>(key, _ => new Code<TDefinition>(key, canonicalDefinition, Zentient.Metadata.Metadata.Empty, null));
            CodeRegistry.OnCodeCreated(added);

            return added;
        }

        /// <summary>
        /// Safe, non-throwing version of <see cref="GetOrCreate(string, TDefinition, IMetadata?, string?, Func{TDefinition, TDefinition, bool}?, bool)"/>.
        /// Catches any validation or consistency errors and returns <see langword="false"/>.
        /// </summary>
        /// <param name="key">The unique canonical key for the code.</param>
        /// <param name="definition">The domain definition to associate with the code.</param>
        /// <param name="code">When the method returns, contains the resulting code instance; otherwise, <see langword="null"/>.</param>
        /// <param name="allowOverride">If <see langword="true"/>, relaxes the definition equivalence check for existing codes.</param>
        /// <returns><see langword="true"/> if the code was successfully retrieved or created; otherwise, <see langword="false"/>.</returns>
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
        /// Attempts to create and cache a *new* code instance, failing if a code with the same key already exists.
        /// Useful for guaranteeing the first registration wins.
        /// </summary>
        /// <param name="key">The unique canonical key for the code.</param>
        /// <param name="definition">The domain definition to associate with the code.</param>
        /// <param name="code">When the method returns, contains the newly created or existing code instance.</param>
        /// <returns><see langword="true"/> if a new instance was created and added to the cache; <see langword="false"/> if an existing code was found.</returns>
        public static bool TryCreate(string key, TDefinition definition, out ICode<TDefinition> code)
        {
            CodeValidation.ValidateKey(key);
            CodeValidation.ValidateDefinition(definition);

            // Check cache for existence
            if (Cache.TryGet<TDefinition>(key, out var existing) && existing is ICode<TDefinition> existingCode)
            {
                code = existingCode;
                return false;
            }

            // Proceed with creation if cache miss
            var fp = ComputeDefinitionFingerprint(definition) ?? string.Empty;
            var canonicalDef = (TDefinition)CodeRegistry.GetOrAddDefinitionByFingerprint(typeof(TDefinition), fp, definition);

            var created = new Code<TDefinition>(key, canonicalDef, Zentient.Metadata.Metadata.Empty, null);

            // Use AddOrGet to handle the race condition
            code = Cache.AddOrGet<TDefinition>(created.Key, _ => created);

            // Determine if we created it (won the race)
            if (ReferenceEquals(code, created))
            {
                CodeRegistry.OnCodeCreated(code);
                return true;
            }

            // Lost the race, an existing one was returned
            CodeRegistry.OnCodeReused(code);
            return false;
        }

        /// <summary>
        /// Determines if the current code instance is equivalent to another object.
        /// Equality is based on: Key, Definition Type, and Definition Fingerprint.
        /// </summary>
        /// <param name="obj">The object to compare against.</param>
        /// <returns><see langword="true"/> if the objects are equivalent; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (obj is ICode other)
            {
                if (!string.Equals(Key, other.Key, StringComparison.Ordinal)) return false;
                if (DefinitionType != other.DefinitionType) return false;

                // Fast check using the internal fingerprint
                if (other is IInternalCode ic)
                {
                    return string.Equals(_fingerprint, ic.Fingerprint, StringComparison.Ordinal);
                }

                // Fallback (less efficient) check
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the hash code for the code instance, computed from the Key, Definition Type, and Definition Fingerprint.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => _hashCode;

        // --- String Pooling -----------------------------------------------------

        private static class StringPool
        {
            // ConcurrentDictionary is used as a thread-safe, low-allocation string pool.
            private static readonly ConcurrentDictionary<string, string> s_pool = new(StringComparer.Ordinal);

            /// <summary>
            /// Retrieves a canonical, interned instance of the provided string key.
            /// </summary>
            /// <param name="key">The key string.</param>
            /// <returns>The canonical string instance from the pool.</returns>
            /// <exception cref="ArgumentException">Thrown if the key is empty or whitespace.</exception>
            /// <exception cref="ArgumentNullException">Thrown if the key is null.</exception>
            public static string Get(string key)
            {
                if (key is null) throw new ArgumentNullException(nameof(key));
                if (key.Length == 0) throw new ArgumentException("Key cannot be empty.", nameof(key));
                if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key cannot be whitespace.", nameof(key));

                // Fast path: TryGetValue saves a delegate allocation and dictionary overhead if the key is present.
                if (s_pool.TryGetValue(key, out var existing)) return existing;

                // Slow path: AddOrGet ensures the string is interned into the pool.
                // Using the key as the factory value avoids a closure delegate allocation.
                return s_pool.GetOrAdd(key, key);
            }
        }
    }
}
