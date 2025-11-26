// <copyright file="FileName.cs" author="Zentient Framework Team">
// (c) 2025 Zentient Framework Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Zentient.Definitions
{
    using Zentient.Concepts;

    /// <summary>
    /// Represents a conceptual definition within the domain model.
    /// </summary>
    /// <remarks>Implementations of this interface typically provide metadata or descriptive information about
    /// a concept. This interface is intended to be used as part of a larger abstraction for domain-driven design
    /// scenarios.</remarks>
    public interface IDefinition : IConcept { }

    /// <summary>
    /// Defines a method for determining whether two definitions are considered equivalent according to custom
    /// comparison logic.
    /// </summary>
    /// <remarks>Implementations of this interface can be used to customize equality checks for definitions,
    /// such as in collections or validation scenarios. The criteria for equivalence may vary depending on the specific
    /// implementation.</remarks>
    /// <typeparam name="TDefinition">The type of definition to compare. Must implement <see cref="IDefinition"/>.</typeparam>
    public interface IDefinitionComparer<TDefinition>
        where TDefinition : IDefinition
    {
        /// <summary>
        /// Determines whether two code definitions are considered equivalent based on their identity fingerprints or
        /// runtime types.
        /// </summary>
        /// <remarks>If both parameters implement ICodeDefinitionFingerprint, equivalence is determined by
        /// comparing their identity fingerprints. Otherwise, equivalence is determined by comparing their runtime
        /// types. This method does not perform deep structural comparison.</remarks>
        /// <param name="a">The first code definition to compare. Cannot be null.</param>
        /// <param name="b">The second code definition to compare. Cannot be null.</param>
        /// <returns>true if the code definitions are considered equivalent; otherwise, false.</returns>
        bool AreEquivalent(TDefinition a, TDefinition b);
    }
}
