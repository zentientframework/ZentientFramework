// <copyright file="Category.cs" author="Ulf Bourelius">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

#pragma warning disable CS1591

namespace Zentient.Metadata
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;

    // Public facade that exposes a small, stable surface for consumers.
    // All concrete implementation types are internal below.
    public static class Metadata
    {
        // Public key type that consumers use. Internally this wraps the real implementation.
        public sealed class Key<T>
        {
            internal MetadataKey<T> Impl { get; }

            public string Id => Impl.Id;
            public Cardinality Cardinality => Impl.Cardinality;
            public Func<T, bool>? Validator => Impl.Validator;
            public string? Category => Impl.Category;

            public Key(string id, Cardinality cardinality = Cardinality.Single, Func<T, bool>? validator = null, string? category = null)
            {
                Impl = new MetadataKey<T>(id, cardinality, validator, category);
            }

            internal Key(MetadataKey<T> impl) => Impl = impl;

            public override string ToString() => Impl.ToString();
            public override bool Equals(object? obj) => obj is Key<T> k && Impl.Equals(k.Impl);
            public override int GetHashCode() => Impl.GetHashCode();
        }

        // Basic facade helpers that forward to the internal implementation.
        public static IMetadata Empty => InternalMetadata.Empty;

        public static IMetadata With<T>(IMetadata metadata, Key<T> key, T value)
            => ((InternalMetadata)metadata).With(key.Impl, value);

        public static IMetadata WithList<T>(IMetadata metadata, Key<T> key, IEnumerable<T> values)
            => ((InternalMetadata)metadata).WithList(key.Impl, values);

        public static IMetadata Without(IMetadata metadata, string keyId)
            => ((InternalMetadata)metadata).Without(keyId);

        public static IMetadata Merge(IMetadata left, IMetadata right, MetadataSchema? schema = null)
            => ((InternalMetadata)left).Merge(right, schema);

        public static bool TryGet<T>(IMetadata metadata, Key<T> key, out T? value)
            => ((InternalMetadata)metadata).TryGet(key.Impl, out value);

        public static bool TryGetList<T>(IMetadata metadata, Key<T> key, out IReadOnlyList<T>? list)
            => ((InternalMetadata)metadata).TryGetList(key.Impl, out list);

        public static MetadataValue CreateValue<T>(T value, Cardinality cardinality = Cardinality.Single)
            => MetadataValue.Create(value, cardinality);
    }

    // internal concrete implementation of the key
    internal sealed class MetadataKey<T> : IEquatable<MetadataKey<T>>
    {
        public string Id { get; }
        public Cardinality Cardinality { get; }
        public Func<T, bool>? Validator { get; }
        public string? Category { get; }

        public MetadataKey(string id, Cardinality cardinality = Cardinality.Single, Func<T, bool>? validator = null, string? category = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Cardinality = cardinality;
            Validator = validator;
            Category = category;
        }

        public override bool Equals(object? obj) => obj is MetadataKey<T> k && Equals(k);
        public bool Equals(MetadataKey<T>? other) => other != null && Id == other.Id && Cardinality == other.Cardinality;
        public override int GetHashCode() => HashCode.Combine(Id, Cardinality);
        public override string ToString() => $"MetadataKey<{typeof(T).Name}>({Id},{Cardinality})";
    }

    public enum Cardinality
    {
        Single,
        List,
        Set,
        Map
    }

    // ------------------------
    // MetadataValue (wrapper for strong typing)
    // ------------------------
    public abstract record MetadataValue
    {
        public abstract object Raw { get; }
        public static MetadataValue Create<T>(T value, Cardinality cardinality = Cardinality.Single)
            => cardinality switch
            {
                Cardinality.Single => new ScalarValue<T>(value),
                Cardinality.List => new ListValue<T>(ImmutableArray.Create(value)),
                Cardinality.Set => new SetValue<T>(ImmutableHashSet.Create(value)),
                _ => throw new NotSupportedException($"Cardinality {cardinality} not supported for Create<T>")
            };
    }

    // concrete implementations made internal per request
    internal sealed record ScalarValue<T>(T Value) : MetadataValue
    {
        public override object Raw => Value!;
    }

    internal sealed record ListValue<T>(ImmutableArray<T> Values) : MetadataValue
    {
        public override object Raw => Values;
    }

    internal sealed record SetValue<T>(ImmutableHashSet<T> Values) : MetadataValue
    {
        public override object Raw => Values;
    }

    // ------------------------
    // IMetadata: immutable map of MetadataKey -> MetadataValue
    // ------------------------
    public interface IMetadata : IReadOnlyDictionary<string, MetadataValue>
    {
        IMetadata With<T>(Metadata.Key<T> key, T value);
        IMetadata WithList<T>(Metadata.Key<T> key, IEnumerable<T> values);
        IMetadata Without(string keyId);
        IMetadata Merge(IMetadata other, MetadataSchema? schema = null);
        bool TryGet<T>(Metadata.Key<T> key, out T? value);
        bool TryGetList<T>(Metadata.Key<T> key, out IReadOnlyList<T>? list);
    }

    internal sealed class InternalMetadata : IMetadata, IEquatable<InternalMetadata>
    {
        private readonly ImmutableDictionary<string, MetadataValue> _map;
        private static readonly InternalMetadata s_empty = new InternalMetadata();

        public InternalMetadata() => _map = ImmutableDictionary<string, MetadataValue>.Empty;
        private InternalMetadata(ImmutableDictionary<string, MetadataValue> map) => _map = map;

        public static InternalMetadata Empty => s_empty;

        public InternalMetadata With<T>(MetadataKey<T> key, T value)
        {
            if (key.Validator is not null && !key.Validator(value))
                throw new ArgumentException($"Value for key '{key.Id}' failed validation.");

            var mv = MetadataValue.Create(value, key.Cardinality);
            var newMap = _map.SetItem(key.Id, mv);
            return new InternalMetadata(newMap);
        }

        public InternalMetadata WithList<T>(MetadataKey<T> key, IEnumerable<T> values)
        {
            var arr = values?.ToImmutableArray() ?? ImmutableArray<T>.Empty;
            if (key.Validator is not null)
            {
                foreach (var v in arr)
                {
                    if (!key.Validator(v)) throw new ArgumentException($"One or more values for '{key.Id}' failed validation.");
                }
            }

            MetadataValue mv = key.Cardinality switch
            {
                Cardinality.List => new ListValue<T>(arr),
                Cardinality.Set => new SetValue<T>(ImmutableHashSet.CreateRange(arr)),
                _ when key.Cardinality == Cardinality.Single && arr.Length == 1 => new ScalarValue<T>(arr[0]),
                _ => throw new ArgumentException($"Cardinality mismatch for key {key.Id}")
            };

            var newMap = _map.SetItem(key.Id, mv);
            return new InternalMetadata(newMap);
        }

        public InternalMetadata Without(string keyId)
        {
            if (keyId == null) throw new ArgumentNullException(nameof(keyId));
            var newMap = _map.Remove(keyId);
            return new InternalMetadata(newMap);
        }

        public IMetadata Merge(IMetadata other, MetadataSchema? schema = null)
        {
            if (other == null) return this;

            var builder = _map.ToBuilder();

            // collect other keys and sort deterministically
            var otherKeys = new string[other.Count];
            int idx = 0;
            foreach (var k in other.Keys) otherKeys[idx++] = k;
            Array.Sort(otherKeys, StringComparer.Ordinal);

            for (int i = 0; i < otherKeys.Length; i++)
            {
                var key = otherKeys[i];
                if (!other.TryGetValue(key, out var value)) continue;

                if (schema != null && schema.IsForbidden(key))
                    throw new InvalidOperationException($"Key '{key}' is forbidden by schema.");

                var rule = schema?.GetMergeRule(key) ?? MergeRule.Overwrite;

                switch (rule)
                {
                    case MergeRule.Overwrite:
                        builder[key] = value;
                        break;
                    case MergeRule.Combine:
                        if (builder.TryGetValue(key, out var existing))
                        {
                            var combined = CombineValues(existing, value);
                            builder[key] = combined;
                        }
                        else builder[key] = value;
                        break;
                    case MergeRule.Reject:
                        if (builder.ContainsKey(key))
                            throw new InvalidOperationException($"Merge rejected for key '{key}' due to existing value.");
                        builder[key] = value;
                        break;
                    default:
                        throw new NotSupportedException($"Unknown merge rule {rule}");
                }
            }

            var merged = new InternalMetadata(builder.ToImmutable());
            if (schema != null)
            {
                schema.Validate(merged); // will throw on invalid
            }
            return merged;
        }

        private static MetadataValue CombineValues(MetadataValue a, MetadataValue b)
        {
            var aType = a.GetType();
            var bType = b.GetType();
            if (aType == bType)
            {
                if (aType.IsGenericType && aType.GetGenericTypeDefinition() == typeof(ListValue<>))
                {
                    var aRaw = (IEnumerable)a.Raw;
                    var bRaw = (IEnumerable)b.Raw;
                    var builder = ImmutableArray.CreateBuilder<object>();
                    foreach (var x in aRaw) builder.Add(x!);
                    foreach (var x in bRaw) builder.Add(x!);
                    return new ListValue<object>(builder.ToImmutable());
                }

                if (aType.IsGenericType && aType.GetGenericTypeDefinition() == typeof(SetValue<>))
                {
                    var aRaw = (IEnumerable)a.Raw;
                    var bRaw = (IEnumerable)b.Raw;
                    var setBuilder = ImmutableHashSet.CreateBuilder<object>();
                    foreach (var x in aRaw) setBuilder.Add(x!);
                    foreach (var x in bRaw) setBuilder.Add(x!);
                    return new SetValue<object>(setBuilder.ToImmutable());
                }
            }

            if (a is ListValue<string> la && b is ListValue<string> lb)
                return new ListValue<string>(la.Values.AddRange(lb.Values));
            if (a is SetValue<string> sa && b is SetValue<string> sb)
                return new SetValue<string>(sa.Values.Union(sb.Values));

            return b;
        }

        // Public IMetadata surface methods (internal implementations using internal MetadataKey)
        public bool TryGet<T>(MetadataKey<T> key, out T? value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (_map.TryGetValue(key.Id, out var mv))
            {
                if (mv is ScalarValue<T> sv)
                {
                    value = sv.Value;
                    return true;
                }
                if (mv is ListValue<T> lv && lv.Values.Length == 1)
                {
                    value = lv.Values[0];
                    return true;
                }
            }
            value = default;
            return false;
        }

        public bool TryGetList<T>(MetadataKey<T> key, out IReadOnlyList<T>? list)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (_map.TryGetValue(key.Id, out var mv))
            {
                if (mv is ListValue<T> lv)
                {
                    list = lv.Values;
                    return true;
                }
                if (mv is SetValue<T> sv)
                {
                    list = sv.Values.ToImmutableArray();
                    return true;
                }
                if (mv is ScalarValue<T> s)
                {
                    list = ImmutableArray.Create(s.Value);
                    return true;
                }
            }
            list = null;
            return false;
        }

        // IReadOnlyDictionary implementation
        public IEnumerable<string> Keys => _map.Keys;
        public IEnumerable<MetadataValue> Values => _map.Values;
        public int Count => _map.Count;
        public bool ContainsKey(string key) => _map.ContainsKey(key);

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out MetadataValue value) => _map.TryGetValue(key, out value);

        public MetadataValue this[string key] => _map[key];
        public IEnumerator<KeyValuePair<string, MetadataValue>> GetEnumerator() => _map.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _map.GetEnumerator();

        public override bool Equals(object? obj) => obj is InternalMetadata m && Equals(m);
        public bool Equals(InternalMetadata? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (_map.Count != other._map.Count) return false;

            foreach (var kv in _map)
            {
                if (!other._map.TryGetValue(kv.Key, out var otherValue)) return false;
                if (!EqualityComparer<MetadataValue>.Default.Equals(kv.Value, otherValue)) return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            var hc = new HashCode();
            hc.Add(_map.Count);
            foreach (var kv in _map)
            {
                hc.Add(kv.Key, StringComparer.Ordinal);
                hc.Add(kv.Value);
            }
            return hc.ToHashCode();
        }

        // explicit interface implementations to expose IMetadata using the public facade Key<T>
        IMetadata IMetadata.With<T>(Metadata.Key<T> key, T value) => With(key.Impl, value);
        IMetadata IMetadata.WithList<T>(Metadata.Key<T> key, IEnumerable<T> values) => WithList(key.Impl, values);
        IMetadata IMetadata.Without(string keyId) => Without(keyId);
        IMetadata IMetadata.Merge(IMetadata other, MetadataSchema? schema) => Merge(other, schema);
        bool IMetadata.TryGet<T>(Metadata.Key<T> key, out T? value) => TryGet(key.Impl, out value);
        bool IMetadata.TryGetList<T>(Metadata.Key<T> key, out IReadOnlyList<T>? list) => TryGetList(key.Impl, out list);
    }

    // ------------------------
    // MetadataSchema and Merge rules
    // ------------------------
    public enum MergeRule { Overwrite, Combine, Reject }

    public sealed class MetadataSchema
    {
        private readonly ImmutableHashSet<string> _allowed;
        private readonly ImmutableHashSet<string> _required;
        private readonly ImmutableHashSet<string> _forbidden;
        private readonly ImmutableDictionary<string, MergeRule> _mergeRules;

        public string Id { get; }
        public MetadataSchema(string id, IEnumerable<string>? allowed = null, IEnumerable<string>? required = null, IEnumerable<string>? forbidden = null, IDictionary<string, MergeRule>? mergeRules = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            _allowed = allowed?.ToImmutableHashSet(StringComparer.Ordinal) ?? ImmutableHashSet<string>.Empty;
            _required = required?.ToImmutableHashSet(StringComparer.Ordinal) ?? ImmutableHashSet<string>.Empty;
            _forbidden = forbidden?.ToImmutableHashSet(StringComparer.Ordinal) ?? ImmutableHashSet<string>.Empty;
            _mergeRules = mergeRules?.ToImmutableDictionary(StringComparer.Ordinal) ?? ImmutableDictionary<string, MergeRule>.Empty;
        }

        public bool IsAllowed(string key) => _allowed.IsEmpty || _allowed.Contains(key);
        public bool IsRequired(string key) => _required.Contains(key);
        public bool IsForbidden(string key) => _forbidden.Contains(key);
        public MergeRule GetMergeRule(string key) => _mergeRules.TryGetValue(key, out var r) ? r : MergeRule.Overwrite;

        public void Validate(IMetadata metadata)
        {
            foreach (var req in _required)
            {
                if (!metadata.ContainsKey(req))
                    throw new InvalidOperationException($"Required key '{req}' missing per schema '{Id}'.");
            }

            if (!_allowed.IsEmpty)
            {
                foreach (var k in metadata.Keys)
                {
                    if (IsForbidden(k)) throw new InvalidOperationException($"Key '{k}' is forbidden by schema '{Id}'.");
                    if (!_allowed.Contains(k)) throw new InvalidOperationException($"Key '{k}' is not allowed by schema '{Id}'.");
                }
            }
            else
            {
                foreach (var k in metadata.Keys)
                {
                    if (IsForbidden(k)) throw new InvalidOperationException($"Key '{k}' is forbidden by schema '{Id}'.");
                }
            }
        }
    }
}
