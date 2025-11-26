namespace Zentient.Codes
{
    using System;

    /// <summary>
    /// Default comparer that determines equivalence between code definitions.
    /// Compares fingerprints when available; otherwise falls back to runtime type equality.
    /// </summary>
    internal sealed class DefaultDefinitionComparer : ICodeDefinitionComparer
    {
        public bool AreEquivalent(ICodeDefinition a, ICodeDefinition b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is ICodeDefinitionFingerprint fa && b is ICodeDefinitionFingerprint fb)
            {
                return string.Equals(fa.IdentityFingerprint, fb.IdentityFingerprint, StringComparison.Ordinal);
            }

            return a.GetType() == b.GetType();
        }
    }
}
