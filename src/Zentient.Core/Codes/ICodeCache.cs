using System;

namespace Zentient.Codes
{
    /// <summary>
    /// Abstraction for a cache that stores canonical <see cref="ICode{TDefinition}"/> instances.
    /// Implementations should be thread-safe and efficient.
    /// </summary>
    public interface ICodeCache
    {
        /// <summary>
        /// Attempts to retrieve the code definition associated with the specified key.
        /// </summary>
        /// <typeparam name="TDefinition">The type of code definition to retrieve. Must implement <see cref="ICodeDefinition"/>.</typeparam>
        /// <param name="key">The key that identifies the code definition to retrieve. Cannot be null.</param>
        /// <param name="code">When this method returns, contains the code definition associated with the specified key if found;
        /// otherwise, <see langword="null"/>. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if a code definition with the specified key is found; otherwise, <see
        /// langword="false"/>.</returns>
        bool TryGet<TDefinition>(string key, out ICode<TDefinition>? code) where TDefinition : ICodeDefinition;

        /// <summary>
        /// Gets the existing code instance associated with the specified key, or adds a new one using the provided
        /// factory if none exists.
        /// </summary>
        /// <remarks>If a code instance for the given key already exists, the factory is not invoked and
        /// the existing instance is returned. This method is thread-safe.</remarks>
        /// <typeparam name="TDefinition">The type of code definition associated with the code instance. Must implement <see cref="ICodeDefinition"/>.</typeparam>
        /// <param name="key">The unique key used to identify the code instance. Cannot be null.</param>
        /// <param name="factory">A function that creates a new <see cref="ICode{TDefinition}"/> instance for the specified key if one does
        /// not already exist. Cannot be null.</param>
        /// <returns>The <see cref="ICode{TDefinition}"/> instance associated with the specified key. If no instance exists, a
        /// new one is created and returned.</returns>
        ICode<TDefinition> AddOrGet<TDefinition>(string key, Func<string, ICode<TDefinition>> factory) where TDefinition : ICodeDefinition;

        /// <summary>
        /// Removes all items associated with the specified code definition type.
        /// </summary>
        /// <typeparam name="TDefinition">The type of code definition whose items will be cleared. Must implement <see cref="ICodeDefinition"/>.</typeparam>
        void Clear<TDefinition>() where TDefinition : ICodeDefinition;

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        /// <remarks>After calling this method, the collection will be empty. This operation may affect
        /// any observers or listeners that depend on the collection's contents.</remarks>
        void ClearAll();
    }
}
