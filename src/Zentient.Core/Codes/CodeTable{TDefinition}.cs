namespace Zentient.Codes
{
    using System;
    using System.Collections.Concurrent;

    /// <summary>
    /// Provides a thread-safe lookup table for code definitions of type <typeparamref name="TDefinition"/>.
    /// </summary>
    /// <remarks>This class is intended for internal use to efficiently store and retrieve code definitions by
    /// their string keys. The table uses ordinal string comparison for key lookups and supports concurrent
    /// access.</remarks>
    /// <typeparam name="TDefinition">The type of code definition stored in the table. Must implement <see cref="ICodeDefinition"/>.</typeparam>
    internal static class CodeTable<TDefinition>
        where TDefinition : ICodeDefinition
    {
        /// <summary>
        /// Provides a thread-safe mapping of string keys to code definitions of type <see cref="ICode{TDefinition}"/>
        /// using ordinal string comparison.
        /// </summary>
        /// <remarks>This dictionary allows concurrent access and updates from multiple threads. Keys are
        /// compared using ordinal string comparison, which is case-sensitive and culture-insensitive.</remarks>
        internal static readonly ConcurrentDictionary<string, ICode<TDefinition>> Table = new(StringComparer.Ordinal);
    }
}
