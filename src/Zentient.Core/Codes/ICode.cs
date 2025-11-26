namespace Zentient.Codes
{
    using System;
    using Zentient.Metadata;

    /// <summary>
    /// Non-generic read-only surface that represents a canonical code instance.
    /// </summary>
    public interface ICode
    {
        /// <summary>
        /// Gets the stable key that identifies the code within a definition type.
        /// Keys are compared using ordinal semantics.
        /// </summary>
        string Key { get; }

        /// <summary>
        /// Gets an optional human-friendly display name for the code.
        /// </summary>
        string? DisplayName { get; }

        /// <summary>
        /// Gets arbitrary metadata attached to the code.
        /// </summary>
        IMetadata Metadata { get; }

        /// <summary>
        /// Gets the <see cref="Type"/> of the definition associated with this code.
        /// </summary>
        Type DefinitionType { get; }

        /// <summary>
        /// Gets the unique identifier for the definition, composed of the type's full name and key.
        /// This is a lightweight computed string and may be used for logging or debugging.
        /// </summary>
        string Identifier => $"{DefinitionType.FullName ?? DefinitionType.Name}:{Key}";

        /// <summary>
        /// Gets the canonical identifier for the definition, formatted as a unique string.
        /// </summary>
        string CanonicalId => $"code:{DefinitionType.FullName ?? DefinitionType.Name}:{Key}";
    }
}
