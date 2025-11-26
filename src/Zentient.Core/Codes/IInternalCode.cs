namespace Zentient.Codes
{
    /// <summary>
    /// Internal helper used to extract the stored fingerprint without reflection.
    /// Implemented by the canonical <see cref="Code{TDefinition}"/> to provide a fast-path fingerprint.
    /// </summary>
    internal interface IInternalCode
    {
        string? Fingerprint { get; }
    }
}
