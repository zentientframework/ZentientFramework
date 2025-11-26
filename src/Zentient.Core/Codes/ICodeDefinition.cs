namespace Zentient.Codes
{
    using Zentient.Definitions;

    /// <summary>
    /// Marker interface for types that describe a code definition payload.
    /// </summary>
    /// <remarks>
    /// Implement this interface for types that act as canonical definition objects used to
    /// parameterize a <see cref="Code{TDefinition}"/> instance.
    /// </remarks>
    public interface ICodeDefinition : IDefinition { }
}
