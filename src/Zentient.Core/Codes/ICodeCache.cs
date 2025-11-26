namespace Zentient.Codes
{
    /// <summary>
    /// Abstraction for a cache that stores canonical <see cref="ICode{TDefinition}"/> instances.
    /// Implementations should be thread-safe and efficient.
    /// </summary>
    public interface ICodeCache
    {
        bool TryGet<TDefinition>(string key, out ICode<TDefinition>? code) where TDefinition : ICodeDefinition;
        ICode<TDefinition> AddOrGet<TDefinition>(string key, ICode<TDefinition> created) where TDefinition : ICodeDefinition;
        void Clear<TDefinition>() where TDefinition : ICodeDefinition;
        void ClearAll();
    }
}
