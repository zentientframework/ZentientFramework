namespace Zentient.Codes
{
    using Zentient.Metadata;

    /// <summary>
    /// Builder API for constructing a <see cref="ICode{TDefinition}"/>.
    /// This builder is intentionally lightweight and DX-first.
    /// </summary>
    /// <typeparam name="TDefinition">The concrete definition type used by the code.</typeparam>
    public interface ICodeBuilder<TDefinition>
    where TDefinition : ICodeDefinition
    {
        ICodeBuilder<TDefinition> WithDefinition(TDefinition definition);
        ICodeBuilder<TDefinition> WithKey(string key);
        ICodeBuilder<TDefinition> WithDisplayName(string displayName);
        ICodeBuilder<TDefinition> WithMetadata(string key, object? value);
        ICodeBuilder<TDefinition> WithMetadata(IMetadata metadata);
        ICodeBuilder<TDefinition> WithMetadataIf(bool condition, string key, object? value);
        ICodeBuilder<TDefinition> WithMetadataIf(bool condition, IMetadata metadata);
        ICode<TDefinition> Build();
    }
}
