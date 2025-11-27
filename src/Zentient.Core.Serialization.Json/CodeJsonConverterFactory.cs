// <copyright file="CodeJsonConverterFactory.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Serialization.Json
{
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using Zentient.Codes;
    using Zentient.Facades;
    using Zentient.Metadata;

    /// <summary>
    /// System.Text.Json converter factory for <see cref="ICode"/> and <see cref="ICode{TDefinition}"/>.
    /// </summary>
    public sealed class CodeJsonConverterFactory : JsonConverterFactory
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type typeToConvert)
        {
            if (typeof(ICode).IsAssignableFrom(typeToConvert)) return true;
            if (typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(ICode<>)) return true;
            return false;
        }

        /// <inheritdoc/>
        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeToConvert == typeof(ICode) || typeof(ICode).IsAssignableFrom(typeToConvert))
            {
                return new UntypedCodeConverter();
            }

            if (typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(ICode<>))
            {
                var defType = typeToConvert.GetGenericArguments()[0];
                var converterType = typeof(TypedCodeConverter<>).MakeGenericType(defType);
                return (JsonConverter?)Activator.CreateInstance(converterType)!;
            }

            return null;
        }

        // Converter for ICode untyped
        private sealed class UntypedCodeConverter : JsonConverter<ICode>
        {
            public override ICode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                var root = doc.RootElement;

                var key = root.GetProperty("key").GetString() ?? throw new JsonException("Missing key");
                var display = root.TryGetProperty("displayName", out var d) ? d.GetString() : null;
                var meta = root.TryGetProperty("metadata", out var m) ? DeserializeMetadata(m) : Metadata.Empty;
                var hint = root.TryGetProperty("definitionHint", out var th) ? th.GetString() : null;

                if (hint is not null && CodeRegistry.TryResolve(hint, out var defObj) && defObj is ICodeDefinition def)
                {
                    // Use reflection to call Code<T>.GetOrCreate
                    var defType = def.GetType();
                    var codeType = typeof(Code<>).MakeGenericType(defType);
                    var method = codeType.GetMethod("GetOrCreate", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (method is null) throw new JsonException("Cannot find GetOrCreate");
                    var created = method.Invoke(null, new object?[] { key, def, meta, display });
                    if (created is ICode ic) return ic;
                }

                // If host allows untrusted fallback, return lightweight; otherwise fail
                if (CodeRegistry.AllowUntrustedTypeFallback)
                {
                    return new LightweightCode(key, display, meta);
                }

                throw new JsonException($"Unable to resolve definitionHint '{hint ?? "<none>"}' for untyped ICode deserialization. Resolver not registered.");
            }

            public override void Write(Utf8JsonWriter writer, ICode value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteString("key", value.Key);
                if (value.DisplayName is not null) writer.WriteString("displayName", value.DisplayName);

                if (CodeRegistry.TryGetHintForDefinitionType(value.DefinitionType, out var hint) && hint is not null)
                {
                    writer.WriteString("definitionHint", hint);
                }
                else
                {
                    // For legacy support, write a lightweight marker but do not rely on AssemblyQualifiedName by default
                    writer.WriteNull("definitionHint");
                }

                writer.WritePropertyName("metadata");
                WriteMetadata(writer, value.Metadata);
                writer.WriteEndObject();
            }

            private static IMetadata DeserializeMetadata(JsonElement element)
            {
                var b = Metadata.NewBuilder();
                // Assumes Metadata exposes deterministic iteration contract by property order
                foreach (var prop in element.EnumerateObject())
                {
                    b.Set(prop.Name, JsonElementToObject(prop.Value));
                }
                return b.Build();
            }

            private static object? JsonElementToObject(JsonElement e) =>
                e.ValueKind switch
                {
                    JsonValueKind.String => e.GetString(),
                    JsonValueKind.Number => e.TryGetInt64(out var l) ? (object)l : e.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => e.GetRawText()
                };

            private static void WriteMetadata(Utf8JsonWriter writer, IMetadata meta)
            {
                writer.WriteStartObject();
                // Require deterministic OrderedEntries; fallback to enumeration
                if (meta is IDeterministicMetadata det)
                {
                    foreach (var kv in det.OrderedEntries)
                    {
                        writer.WritePropertyName(kv.Key);
                        WriteDeterministicValue(writer, kv.Value);
                    }
                }
                else
                {
                    foreach (var kv in meta)
                    {
                        writer.WritePropertyName(kv.Key);
                        WriteDeterministicValue(writer, kv.Value);
                    }
                }
                writer.WriteEndObject();
            }

            private static void WriteDeterministicValue(Utf8JsonWriter writer, object? value)
            {
                switch (value)
                {
                    case null: writer.WriteNullValue(); break;
                    case string s: writer.WriteStringValue(s); break;
                    case bool b: writer.WriteBooleanValue(b); break;
                    case int i: writer.WriteNumberValue(i); break;
                    case long l: writer.WriteNumberValue(l); break;
                    case double d: writer.WriteNumberValue(d); break;
                    default:
                        // For complex values, serialize via JsonSerializer but do not include runtime-type-qualified shapes
                        JsonSerializer.Serialize(writer, value);
                        break;
                }
            }

            // Lightweight fallback code when typed definition cannot be resolved
            private sealed class LightweightCode : ICode
            {
                public string Key { get; }
                public string? DisplayName { get; }
                public IMetadata Metadata { get; }
                public Type DefinitionType => typeof(void);

                [JsonIgnore]
                public bool HasDisplayName => !string.IsNullOrWhiteSpace(DisplayName);

                [JsonIgnore]
                public bool HasMetadata => Metadata != null;

                public LightweightCode(string key, string? displayName, IMetadata metadata)
                {
                    Key = key;
                    DisplayName = displayName;
                    Metadata = metadata;
                }
            }
        }

        // Converter for ICode<TDefinition>
        private sealed class TypedCodeConverter<TDefinition> : JsonConverter<ICode<TDefinition>>
            where TDefinition : ICodeDefinition
        {
            public override ICode<TDefinition> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                var root = doc.RootElement;

                var key = root.GetProperty("key").GetString() ?? throw new JsonException("Missing key");
                var display = root.TryGetProperty("displayName", out var d) ? d.GetString() : null;
                var meta = root.TryGetProperty("metadata", out var m) ? DeserializeMetadata(m) : Metadata.Empty;
                var hint = root.TryGetProperty("definitionHint", out var th) ? th.GetString() : null;

                if (hint is null) throw new JsonException("Missing definitionHint for typed ICode deserialization.");

                if (CodeRegistry.TryResolve(hint, out var defObj) && defObj is TDefinition def)
                {
                    return Code<TDefinition>.GetOrCreate(key, def, meta, display);
                }

                // If registry doesn't have a resolver for this hint, fail (host may enable fallback but for typed target we require registry)
                throw new JsonException($"Unable to resolve definitionHint '{hint}' to a '{typeof(TDefinition).FullName}' resolver.");
            }

            public override void Write(Utf8JsonWriter writer, ICode<TDefinition> value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteString("key", value.Key);
                if (value.DisplayName is not null) writer.WriteString("displayName", value.DisplayName);

                if (CodeRegistry.TryGetHintForDefinitionType(value.Definition.GetType(), out var hint) && hint is not null)
                {
                    writer.WriteString("definitionHint", hint);
                }
                else
                {
                    writer.WriteNull("definitionHint");
                }

                writer.WritePropertyName("metadata");
                WriteMetadata(writer, value.Metadata);
                writer.WriteEndObject();
            }

            private static IMetadata DeserializeMetadata(JsonElement element)
            {
                var b = Metadata.NewBuilder();
                foreach (var prop in element.EnumerateObject())
                {
                    b.Set(prop.Name, JsonElementToObject(prop.Value));
                }
                return b.Build();
            }

            private static object? JsonElementToObject(JsonElement e) =>
                e.ValueKind switch
                {
                    JsonValueKind.String => e.GetString(),
                    JsonValueKind.Number => e.TryGetInt64(out var l) ? (object)l : e.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => e.GetRawText()
                };

            private static void WriteMetadata(Utf8JsonWriter writer, IMetadata meta)
            {
                writer.WriteStartObject();
                if (meta is IDeterministicMetadata det)
                {
                    foreach (var kv in det.OrderedEntries)
                    {
                        writer.WritePropertyName(kv.Key);
                        WriteDeterministicValue(writer, kv.Value);
                    }
                }
                else
                {
                    foreach (var kv in meta)
                    {
                        writer.WritePropertyName(kv.Key);
                        WriteDeterministicValue(writer, kv.Value);
                    }
                }
                writer.WriteEndObject();
            }

            private static void WriteDeterministicValue(Utf8JsonWriter writer, object? value)
            {
                switch (value)
                {
                    case null: writer.WriteNullValue(); break;
                    case string s: writer.WriteStringValue(s); break;
                    case bool b: writer.WriteBooleanValue(b); break;
                    case int i: writer.WriteNumberValue(i); break;
                    case long l: writer.WriteNumberValue(l); break;
                    case double d: writer.WriteNumberValue(d); break;
                    default:
                        JsonSerializer.Serialize(writer, value);
                        break;
                }
            }
        }
    }
}
