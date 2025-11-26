namespace Zentient.Codes
{
    /// <summary>
    /// Provides a deterministic, stable fingerprint that uniquely identifies the code definition, such as a version or
    /// hash value.
    /// </summary>
    public interface ICodeDefinitionFingerprint : ICodeDefinition
    {
        /// <summary>
        /// Deterministic, stable fingerprint for the definition (e.g. version/hash).
        /// </summary>
        string IdentityFingerprint { get; }
    }
}
