// <copyright file="Concept.cs" author="Ulf Bourelius">
// (c) 2025 Zentient Framework Team. Licensed under the Apache License, Version 2.0.
// See LICENSE in the project root for license information.
// </copyright>

namespace Zentient.Ontology
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents an object that exposes a stable unique identifier.
    /// </summary>
    public interface IIdentifiable
    {
        /// <summary>
        /// Gets the unique identifier for the object.
        /// Implementations SHOULD use a globally unique value (for example a GUID).
        /// </summary>
        Guid Id { get; }
    }

    /// <summary>
    /// Represents an object that exposes a human-readable name.
    /// </summary>
    public interface INamed
    {
        /// <summary>
        /// Gets the display name for the object.
        /// This is intended for presentation and debugging, not as a canonical identifier.
        /// </summary>
        string Name { get; }
    }

    /// <summary>
    /// Represents an object that provides a textual description.
    /// </summary>
    public interface IDescribed
    {
        /// <summary>
        /// Gets a textual description that provides additional context about the object.
        /// </summary>
        string Description { get; }
    }

    /// <summary>
    /// Represents an object that carries a set of annotations as key/value metadata.
    /// </summary>
    public interface IAnnotated
    {
        /// <summary>
        /// Gets a read-only map of annotation keys to values.
        /// Values are implementation-defined; keys SHOULD be stable strings.
        /// </summary>
        IReadOnlyDictionary<string, object> Annotations { get; }
    }

    /// <summary>
    /// A standard, identifiable concept in the domain model.
    /// Combines identity, naming, description and annotation mix-ins.
    /// </summary>
    [Ontic(PresenceMode.Actual, BeingKind.Entity)]
    public interface IDomainConcept : IConcept, IIdentifiable, INamed, IDescribed, IAnnotated { }

    /// <summary>
    /// The definition or meta-model of a concept.
    /// Definitions are abstract, immutable meta-entities that describe domain concepts.
    /// </summary>
    [Ontic(PresenceMode.Abstract, BeingKind.Entity)]
    [Temporal(TemporalIndex.Timeless, MutationSemantics.Immutable)]
    public interface IDefinition : IDomainConcept { }

    /// <summary>
    /// A typed definition that constrains or describes a specific <see cref="IConcept"/> subtype.
    /// </summary>
    /// <typeparam name="TConcept">The concept type this definition targets.</typeparam>
    public interface IDefinition<out TConcept> : IDefinition where TConcept : IConcept { }

    /// <summary>
    /// Kinds of domain relations used to classify structural relationships between concepts.
    /// </summary>
    public enum RelationKind
    {
        /// <summary>A general association between two concepts.</summary>
        Association,

        /// <summary>A composition implying ownership/lifecycle coupling.</summary>
        Composition,

        /// <summary>An aggregation indicating a looser container relationship.</summary>
        Aggregation,

        /// <summary>An inheritance (is-a) relationship.</summary>
        Inheritance,

        /// <summary>A dependency relationship.</summary>
        Dependency
    }

    /// <summary>
    /// Cardinality options for relation endpoints.
    /// </summary>
    public enum Cardinality
    {
        /// <summary>Exactly one.</summary>
        One,

        /// <summary>Zero or one.</summary>
        ZeroOrOne,

        /// <summary>One to many.</summary>
        OneToMany,

        /// <summary>Zero to many.</summary>
        ZeroToMany
    }

    /// <summary>
    /// Represents a named relation (edge) between two concepts.
    /// Relations are value-like concepts that describe how concepts are connected.
    /// </summary>
    [Ontic(PresenceMode.Actual, BeingKind.Value)]
    public interface IRelation : IConcept, INamed
    {
        /// <summary>
        /// Gets the source concept for the relation.
        /// </summary>
        IConcept Source { get; }

        /// <summary>
        /// Gets the target concept for the relation.
        /// </summary>
        IConcept Target { get; }

        /// <summary>
        /// Gets the kind or semantic classification of the relation.
        /// </summary>
        RelationKind Kind { get; }

        /// <summary>
        /// Gets the cardinality for the source endpoint.
        /// </summary>
        Cardinality SourceCardinality { get; }

        /// <summary>
        /// Gets the cardinality for the target endpoint.
        /// </summary>
        Cardinality TargetCardinality { get; }
    }

    /// <summary>
    /// A generic, typed relation between two specific concept types.
    /// Provides strongly-typed accessors for source and target.
    /// </summary>
    /// <typeparam name="TSource">The concrete concept type for the source.</typeparam>
    /// <typeparam name="TTarget">The concrete concept type for the target.</typeparam>
    public interface IRelation<out TSource, out TTarget> : IRelation
        where TSource : IConcept
        where TTarget : IConcept
    {
        /// <summary>
        /// Gets the strongly-typed source concept.
        /// </summary>
        new TSource Source { get; }

        /// <summary>
        /// Gets the strongly-typed target concept.
        /// </summary>
        new TTarget Target { get; }
    }
}
