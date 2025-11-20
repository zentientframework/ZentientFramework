// <copyright file="Abstractions.cs" author="Ulf Bourelius">
// (c) 2025 Zentient Framework Team. Licensed under the Apache License, Version 2.0.
// See LICENSE in the project root for license information.
// </copyright>

namespace Zentient.Core
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The root atom of the Zentient Ecology.
    /// Every distinct idea, object, or definition in the ecosystem derives from this minimal contract.
    /// Implementations should provide stable identifier and human-readable metadata.
    /// </summary>
    public interface IConcept
    {
        /// <summary>
        /// Gets a stable identifier for the concept.
        /// Implementations SHOULD use a globally unique string (for example, a GUID or a structured identifier).
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets a human-readable name for the concept.
        /// This is intended for display or logging, not as a canonical identifier.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets an optional textual description providing additional context for the concept.
        /// May be null when no description is available.
        /// </summary>
        string? Description { get; }

        /// <summary>
        /// Gets optional, read-only metadata as a key/value map for extensible annotations.
        /// Keys should be stable strings; values are implementation-defined and may be any serializable object.
        /// May be null when no metadata is present.
        /// </summary>
        IReadOnlyDictionary<string, object>? Metadata { get; }
    }

    /* ---------------------------------------------------------------------
       Seven-axis enums
       These enums encode the kernel semantic axes used throughout the project.
       --------------------------------------------------------------------- */

    /// <summary>
    /// Describes the ontic presence mode of a concept.
    /// </summary>
    public enum PresenceMode
    {
        /// <summary>The concept exists concretely in the system (instance/object).</summary>
        Actual,

        /// <summary>The concept is potential or projected but not currently realized.</summary>
        Potential,

        /// <summary>The concept is an abstract definition or meta-model element.</summary>
        Abstract
    }

    /// <summary>
    /// Classifies whether a concept is an entity (identity-bearing) or a value (data).
    /// </summary>
    public enum BeingKind
    {
        /// <summary>Identity-bearing object or resource.</summary>
        Entity,

        /// <summary>Immutable or value-like datum without distinct identity.</summary>
        Value
    }

    /// <summary>
    /// Temporal indexing strategy for a concept or definition.
    /// </summary>
    public enum TemporalIndex
    {
        /// <summary>Not subject to time semantics.</summary>
        Timeless,

        /// <summary>Indexed by a single point in time.</summary>
        TimeIndexed,

        /// <summary>Bound by an interval (start/end).</summary>
        IntervalBound,

        /// <summary>Anchored to an event or occurrence.</summary>
        EventAnchored
    }

    /// <summary>
    /// Mutation semantics describing how instances of a concept evolve over time.
    /// </summary>
    public enum MutationSemantics
    {
        /// <summary>Immutable; instances do not change.</summary>
        Immutable,

        /// <summary>Append-only: new versions are added while prior versions remain.</summary>
        AppendOnly,

        /// <summary>Mutable with history; state may change but history is preserved.</summary>
        MutableWithHistory
    }

    /// <summary>
    /// Policy domain classifications used for governance attributes.
    /// </summary>
    public enum PolicyDomain
    {
        /// <summary>Security-related policy.</summary>
        Security,

        /// <summary>Regulatory or compliance policy.</summary>
        Compliance,

        /// <summary>Quality assurance or correctness policy.</summary>
        Quality,

        /// <summary>Performance-related policy.</summary>
        Performance,

        /// <summary>Cost/financial policy.</summary>
        Cost,

        /// <summary>No particular policy domain.</summary>
        None
    }

    /// <summary>
    /// Enforcement tiers used to express how strongly a policy should be applied.
    /// </summary>
    public enum EnforcementTier
    {
        /// <summary>Hard enforcement (must be enforced).</summary>
        Hard,

        /// <summary>Soft enforcement (best-effort, advisory).</summary>
        Soft,

        /// <summary>Advisory guidance.</summary>
        Advisory,

        /// <summary>Audit-only: record for later review.</summary>
        AuditOnly
    }

    /// <summary>
    /// Role and relation typing used for structural relationships.
    /// </summary>
    public enum LinkType
    {
        /// <summary>Composition relationship.</summary>
        Composition,

        /// <summary>Association relationship.</summary>
        Association,

        /// <summary>Role-based relation.</summary>
        RoleRelation
    }

    /// <summary>
    /// Context binding kinds for contextualization of concepts.
    /// </summary>
    public enum ContextBinding
    {
        /// <summary>Context is intrinsic to the concept.</summary>
        Intrinsic,

        /// <summary>Context is extrinsic / assigned externally.</summary>
        Extrinsic
    }

    /// <summary>
    /// Scope lattice levels used to express contextual scope.
    /// </summary>
    public enum ScopeLattice
    {
        /// <summary>Environment/global scope.</summary>
        Environment,

        /// <summary>Tenant-level scope (multi-tenancy).</summary>
        Tenant,

        /// <summary>Session-level scope.</summary>
        Session,

        /// <summary>Transaction-level scope.</summary>
        Transaction
    }

    /// <summary>
    /// Capability state lifecycle for behavior-oriented concepts.
    /// </summary>
    public enum CapabilityState
    {
        /// <summary>Capability exists but is not active.</summary>
        Latent,

        /// <summary>Capability is active and available.</summary>
        Active,

        /// <summary>Capability is temporarily suspended.</summary>
        Suspended,

        /// <summary>Capability has been retired.</summary>
        Retired
    }

    /// <summary>
    /// Evidence or observability model used to indicate how a concept is observed.
    /// </summary>
    public enum EvidenceModel
    {
        /// <summary>Observed via metrics/time-series.</summary>
        Metrics,

        /// <summary>Observed via logs.</summary>
        Logs,

        /// <summary>Observed via events.</summary>
        Events,

        /// <summary>No evidentiary model specified.</summary>
        None
    }

    /// <summary>
    /// Strength or requirement level of evidence for observability.
    /// </summary>
    public enum EvidenceStrength
    {
        /// <summary>Evidence is mandatory.</summary>
        Mandatory,

        /// <summary>Evidence is conditional.</summary>
        Conditional,

        /// <summary>Evidence is optional.</summary>
        Optional
    }

    /// <summary>
    /// Provenance classification for origin metadata.
    /// </summary>
    public enum ProvenanceType
    {
        /// <summary>Declared provenance (explicitly provided).</summary>
        Declared,

        /// <summary>Captured provenance (observed or recorded by system).</summary>
        Captured,

        /// <summary>Derived provenance (inferred or computed).</summary>
        Derived
    }

    /* ---------------------------------------------------------------------
       Axis attribute types
       Attributes are intended to be applied to interfaces/classes to declare
       axis metadata. Summaries document purpose and constructor args.
       --------------------------------------------------------------------- */

    /// <summary>
    /// Declares ontic metadata for a type (presence mode and being kind).
    /// Apply to interfaces or classes to indicate whether the type is an entity,
    /// value, and whether it is actual, potential, or abstract.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
    public sealed class OnticAttribute : Attribute
    {
        /// <summary>
        /// Gets the presence mode for the annotated type.
        /// </summary>
        public PresenceMode Mode { get; }

        /// <summary>
        /// Gets the being kind for the annotated type.
        /// </summary>
        public BeingKind Kind { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OnticAttribute"/> class.
        /// </summary>
        /// <param name="mode">The presence mode (Actual, Potential, Abstract).</param>
        /// <param name="kind">The being kind (Entity or Value).</param>
        public OnticAttribute(PresenceMode mode, BeingKind kind) => (Mode, Kind) = (mode, kind);
    }

    /// <summary>
    /// Declares temporal metadata for a type (indexing and mutation semantics).
    /// Apply to interfaces or classes to indicate time-related behavior.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
    public sealed class TemporalAttribute : Attribute
    {
        /// <summary>
        /// Gets the temporal indexing strategy.
        /// </summary>
        public TemporalIndex Index { get; }

        /// <summary>
        /// Gets the mutation semantics for the annotated type.
        /// </summary>
        public MutationSemantics Mutation { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TemporalAttribute"/> class.
        /// </summary>
        /// <param name="index">The temporal index classification.</param>
        /// <param name="mutation">The mutation semantics classification.</param>
        public TemporalAttribute(TemporalIndex index, MutationSemantics mutation) => (Index, Mutation) = (index, mutation);
    }

    /// <summary>
    /// Declares a governance policy attribute for a type.
    /// Multiple <see cref="PolicyAttribute"/> instances may be applied to the same type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
    public sealed class PolicyAttribute : Attribute
    {
        /// <summary>Gets the policy domain (security, compliance, etc.).</summary>
        public PolicyDomain Domain { get; }

        /// <summary>Gets the enforcement tier for the policy.</summary>
        public EnforcementTier Tier { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PolicyAttribute"/> class.
        /// </summary>
        /// <param name="domain">The policy domain.</param>
        /// <param name="tier">The enforcement tier.</param>
        public PolicyAttribute(PolicyDomain domain, EnforcementTier tier) => (Domain, Tier) = (domain, tier);
    }

    /// <summary>
    /// Declares contextual binding metadata for a type.
    /// Indicates whether context is intrinsic or extrinsic and the scope lattice level.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
    public sealed class ContextAttribute : Attribute
    {
        /// <summary>Gets the context binding kind (intrinsic or extrinsic).</summary>
        public ContextBinding Binding { get; }

        /// <summary>Gets the scope lattice level of the context.</summary>
        public ScopeLattice Scope { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextAttribute"/> class.
        /// </summary>
        /// <param name="binding">The context binding kind.</param>
        /// <param name="scope">The scope lattice level.</param>
        public ContextAttribute(ContextBinding binding, ScopeLattice scope) => (Binding, Scope) = (binding, scope);
    }
}
