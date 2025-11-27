namespace Zentient.Codes
{
    using System;

    /// <summary>
    /// Defines the contract for a caching mechanism responsible for storing and retrieving canonical <see cref="ICode{TDefinition}"/> instances.
    /// </summary>
    public interface ICodeCache
    {
        /// <summary>
        /// Attempts to retrieve a code instance by its key for a specific definition type.
        /// </summary>
        /// <typeparam name="TDefinition">The expected type of the code definition.</typeparam>
        /// <param name="key">The code's canonical key.</param>
        /// <param name="code">When the method returns, contains the code instance; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the code was found; otherwise, <c>false</c>.</returns>
        bool TryGet<TDefinition>(string key, out ICode<TDefinition>? code) where TDefinition : ICodeDefinition;

        /// <summary>
        /// Atomically adds a new code instance created by the factory function, or retrieves the existing one if a key collision occurs.
        /// </summary>
        /// <typeparam name="TDefinition">The code definition type.</typeparam>
        /// <param name="key">The code's canonical key.</param>
        /// <param name="factory">The function to create the new code instance if one does not exist.</param>
        /// <returns>The newly added or existing code instance.</returns>
        ICode<TDefinition> AddOrGet<TDefinition>(string key, Func<string, ICode<TDefinition>> factory) where TDefinition : ICodeDefinition;

        /// <summary>
        /// Clears all cached code instances associated with a specific code definition type.
        /// </summary>
        /// <typeparam name="TDefinition">The code definition type whose cache should be cleared.</typeparam>
        void Clear<TDefinition>() where TDefinition : ICodeDefinition;

        /// <summary>
        /// Clears all code caches across all registered code definition types.
        /// </summary>
        void ClearAll();
    }
}
