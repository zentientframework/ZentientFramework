// <copyright file="Governance.cs" author="Ulf Bourelius">
// (c) 2025 Zentient Framework Team. Licensed under the Apache License, Version 2.0.
// See LICENSE in the project root for license information.
// </copyright>

namespace Zentient.Governance
{
    using System;
    using System.Collections.Generic;
    using Zentient.Core;
    using Zentient.Ontology;

    /// <summary>
    /// Represents a lifecycle model as a governance concept.
    /// A lifecycle is composed of named states and describes permitted transitions or the overall life-cycle semantics
    /// for a governed concept (for example a resource, policy, or artifact).
    /// </summary>
    public interface ILifecycle : IConcept
    {
        /// <summary>
        /// Gets the collection of states that belong to this lifecycle.
        /// Implementations SHOULD present states in the intended ordering or provide ordering metadata separately.
        /// </summary>
        IReadOnlyCollection<ILifecycleState> States { get; }
    }

    /// <summary>
    /// Represents a single state within an <see cref="ILifecycle"/>.
    /// States are named and optionally carry the time the lifecycle entered this state.
    /// </summary>
    public interface ILifecycleState : INamed
    {
        /// <summary>
        /// Gets the timestamp when this lifecycle entered the state, or <c>null</c> if not available.
        /// This value is optional and may be used for auditing or history purposes.
        /// </summary>
        DateTimeOffset? EnteredAt { get; }
    }

    /// <summary>
    /// Marker interface indicating that a concept is versioned.
    /// Versioned concepts expose the current active version and the historical versions.
    /// </summary>
    public interface IVersioned : IConcept
    {
        /// <summary>
        /// Gets the current (active) version for the concept.
        /// </summary>
        IVersion Current { get; }

        /// <summary>
        /// Gets a read-only collection representing the version history of the concept.
        /// Implementations SHOULD order history from oldest to newest or document the ordering.
        /// </summary>
        IReadOnlyCollection<IVersion> History { get; }
    }

    /// <summary>
    /// Represents a specific version of a versioned concept.
    /// </summary>
    public interface IVersion : IConcept
    {
        /// <summary>
        /// Gets a stable identifier for the version (for example a semantic version string or unique token).
        /// </summary>
        string Identifier { get; }

        /// <summary>
        /// Gets the point in time when the version was released.
        /// </summary>
        DateTimeOffset ReleasedAt { get; }
    }

    /// <summary>
    /// Defines a governance policy as a domain definition.
    /// Policies describe constraints, obligations, and rules that apply to concepts within the domain.
    /// </summary>
    public interface IPolicy : IDefinition
    {
        /// <summary>
        /// Gets the policy domain classification (for example Security, Compliance, Quality).
        /// </summary>
        PolicyDomain Domain { get; }

        /// <summary>
        /// Gets the enforcement tier indicating how strongly the policy should be applied.
        /// </summary>
        EnforcementTier Enforcement { get; }

        /// <summary>
        /// Gets the collection of rules that compose this policy.
        /// Rules are definitions that specify concrete checks, constraints, or behavioral expectations.
        /// </summary>
        IReadOnlyCollection<IPolicyRule> Rules { get; }
    }

    /// <summary>
    /// Represents an individual rule contained within a policy.
    /// Rules are named and provide a short description explaining the rule's intent.
    /// </summary>
    public interface IPolicyRule : INamed
    {
        /// <summary>
        /// Gets a human-readable description of the rule.
        /// Implementations SHOULD keep descriptions concise and suitable for display in UIs or policy reports.
        /// </summary>
        string Description { get; }
    }

    /// <summary>
    /// Indicates that a type (class or interface) has been deprecated.
    /// This attribute is intended for aspect-oriented governance and documentation purposes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class DeprecatedAttribute : Attribute
    {
        /// <summary>
        /// Gets a short reason or guidance why the type was deprecated.
        /// Prefer including migration guidance or replacement types where applicable.
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeprecatedAttribute"/> class.
        /// </summary>
        /// <param name="reason">A short human-readable reason or guidance for the deprecation.</param>
        public DeprecatedAttribute(string reason) => Reason = reason;
    }
}