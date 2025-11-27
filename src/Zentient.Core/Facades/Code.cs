// <copyright file="Code.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Facades
{
    using System;
    using System.Runtime.CompilerServices;
    using Zentient.Codes;
    using Zentient.Exceptions;
    using Zentient.Metadata;
    using Zentient.Validation;

    /// <summary>
    /// Provides static methods for creating, resolving, and building code definitions and code instances.
    /// </summary>
    /// <remarks>Use the methods in this class to obtain code instances by key, optionally supplying a
    /// definition and metadata, or to create new code builders for constructing code definitions. All members are
    /// thread-safe and intended for use as entry points to the code registry and builder infrastructure.</remarks>
    public static class Code
    {
        /// <summary>
        /// Creates or retrieves an <see cref="ICode{TDefinition}"/> instance for the specified key, optionally using
        /// the provided definition, metadata, and display name.
        /// </summary>
        /// <remarks>If <paramref name="definition"/> is not provided, the method attempts to resolve an
        /// existing definition from the code registry using the specified key. If resolution fails and untrusted type
        /// fallback is allowed, a new instance of <typeparamref name="TDefinition"/> is created. This method may throw
        /// if neither resolution nor creation succeeds.</remarks>
        /// <typeparam name="TDefinition">The type of code definition to associate with the code instance. Must implement <see
        /// cref="ICodeDefinition"/>.</typeparam>
        /// <param name="key">The unique key that identifies the code instance. Cannot be null or empty.</param>
        /// <param name="definition">An optional definition to associate with the code instance. If null, the method attempts to resolve or
        /// create a definition of type <typeparamref name="TDefinition"/>.</param>
        /// <param name="metadata">Optional metadata to associate with the code instance. May be null.</param>
        /// <param name="displayName">An optional display name for the code instance. May be null.</param>
        /// <returns>An <see cref="ICode{TDefinition}"/> instance associated with the specified key and definition.</returns>
        /// <exception cref="InvalidOperationException">Thrown if a definition of type <typeparamref name="TDefinition"/> cannot be resolved or created for the
        /// specified key.</exception>
        public static ICode<TDefinition> From<TDefinition>(string key, TDefinition? definition = default, IMetadata? metadata = null, string? displayName = null)
            where TDefinition : ICodeDefinition
        {
            key = Guard.AgainstNullOrWhitespace(key);

            if (definition is null)
            {
                try
                {
                    if (CodeRegistry.TryResolve(key, out var resolved) && resolved is TDefinition resolvedDef)
                    {
                        definition = resolvedDef;
                    }
                    else if (CodeRegistry.AllowUntrustedTypeFallback)
                    {
                        // Activator.CreateInstance may throw; we catch below and wrap.
                        if (Activator.CreateInstance(typeof(TDefinition)) is TDefinition created)
                        {
                            definition = created;
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new DefaultConstructorMissingException(key, ex.Message);
                }
            }

            if (definition is null) throw new InvalidOperationException($"Definition for key '{key}' could not be resolved or created.");

            return Code<TDefinition>.GetOrCreate(key, definition, metadata, displayName);
        }

        /// <summary>
        /// Creates a new instance of a code builder for the specified code definition type.
        /// </summary>
        /// <typeparam name="TDefinition">The type of code definition to be used by the builder. Must implement <see cref="ICodeDefinition"/>.</typeparam>
        /// <returns>A new <see cref="CodeBuilder{TDefinition}"/> instance configured for the specified code definition type.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CodeBuilder<TDefinition> NewBuilder<TDefinition>()
            where TDefinition : ICodeDefinition
            => new CodeBuilder<TDefinition>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static CodeBuilder<TDefinition> NewBuilder<TDefinition>(ICode<TDefinition> code)
            where TDefinition : ICodeDefinition
        {
            var builder = new CodeBuilder<TDefinition>(code);
            return builder;
        }
    }
}
