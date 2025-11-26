namespace Zentient.Codes
{
    /// <summary>
    /// Default unbounded concurrent cache implementation used by <see cref="CodeRegistry"/>.
    /// Uses per-TDefinition generic tables for reduced allocations.
    /// </summary>
    internal sealed class DefaultUnboundedCodeCache : ICodeCache
    {
        /// <inheritdoc/>
        public bool TryGet<TDefinition>(string key, out ICode<TDefinition>? code) where TDefinition : ICodeDefinition
        {
            return CodeTable<TDefinition>.Table.TryGetValue(key, out code);
        }

        /// <inheritdoc/>
        public ICode<TDefinition> AddOrGet<TDefinition>(string key, ICode<TDefinition> created) where TDefinition : ICodeDefinition
        {
            return CodeTable<TDefinition>.Table.GetOrAdd(key, created);
        }

        /// <inheritdoc/>
        public void Clear<TDefinition>() where TDefinition : ICodeDefinition
        {
            CodeTable<TDefinition>.Table.Clear();
        }

        /// <inheritdoc/>
        public void ClearAll()
        {
            // Clear all generic tables by iterating types currently present in registry maps.
            // This is conservative; individual per-type clears are more efficient when caller knows the type.
            // We cannot enumerate all possible generic instantiations—so clear known global registries instead.
            CodeRegistry.ClearAllPerTypeTables();
        }
    }
}
