// <copyright file="Analytics.cs" author="Ulf Bourelius">
// (c) 2025 Zentient Framework Team. Licensed under the Apache License, Version 2.0.
// See LICENSE in the project root for license information.
// </copyright>

namespace Zentient.Analytics
{
    using System;
    using System.Collections.Generic;
    using Zentient.Core;
    using Zentient.Ontology;

    /// <summary>
    /// Marker interface for analytics-related concept types.
    /// Use this as a common base for concepts that belong to the Analytics area.
    /// </summary>
    public interface IAnalyticsConcept : IConcept { }

    /// <summary>
    /// Marker interface for analytics definitions.
    /// Definitions are metadata/meta-model constructs describing analytics concepts.
    /// </summary>
    public interface IAnalyticsDefinition : IDefinition<IAnalyticsConcept> { }

    /// <summary>
    /// Represents an analytics package (logical grouping of analytics artifacts).
    /// </summary>
    public interface IPackage : IAnalyticsConcept
    {
        /// <summary>
        /// Gets the package name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the package version string.
        /// </summary>
        string Version { get; }
    }

    /// <summary>
    /// A specialized analytics package that aggregates metrics, evaluations and diagnostics.
    /// </summary>
    public interface IAnalyticsPackage : IPackage
    {
        /// <summary>
        /// Gets the set of metrics provided by this package.
        /// </summary>
        IReadOnlyCollection<IMetric> Metrics { get; }

        /// <summary>
        /// Gets the collection of evaluations performed by this package.
        /// Evaluations are typed over the subject concept; here they are represented as evaluations of <see cref="IConcept"/>.
        /// </summary>
        IReadOnlyCollection<IEvaluation<IConcept>> Evaluations { get; }

        /// <summary>
        /// Gets diagnostics emitted or recognized by this package.
        /// </summary>
        IReadOnlyCollection<IDiagnostic> Diagnostics { get; }
    }

    // Metrics

    /// <summary>
    /// Represents a metric definition or metric concept.
    /// Metric instances typically carry units and optionally a typed value via <see cref="IMetric{TValue}"/>.
    /// </summary>
    public interface IMetric : IAnalyticsConcept, INamed
    {
        /// <summary>
        /// Gets the unit of measurement for this metric (for example, "ms", "requests", "%").
        /// </summary>
        string Unit { get; }
    }

    /// <summary>
    /// Represents a typed metric whose value is of type <typeparamref name="TValue"/>.
    /// </summary>
    /// <typeparam name="TValue">The CLR type of the metric value (must be non-nullable).</typeparam>
    public interface IMetric<out TValue> : IMetric where TValue : notnull
    {
        /// <summary>
        /// Gets the metric value.
        /// </summary>
        TValue Value { get; }
    }

    /// <summary>
    /// Represents a measured metric instance with a timestamp.
    /// </summary>
    /// <typeparam name="TValue">The type of the measured value.</typeparam>
    public interface IMeasure<out TValue> : IMetric<TValue> where TValue : notnull
    {
        /// <summary>
        /// Gets the time at which the measurement was taken.
        /// </summary>
        DateTimeOffset MeasuredAt { get; }
    }

    // Evaluation

    /// <summary>
    /// Represents an evaluation performed against a subject concept.
    /// Evaluations express whether a subject satisfies a set of criteria.
    /// </summary>
    /// <typeparam name="TSubject">The subject concept type being evaluated.</typeparam>
    public interface IEvaluation<out TSubject> : IAnalyticsConcept where TSubject : IConcept
    {
        /// <summary>
        /// Gets the subject of the evaluation.
        /// </summary>
        TSubject Subject { get; }

        /// <summary>
        /// Gets the collection of criteria used to evaluate the subject.
        /// </summary>
        IReadOnlyCollection<ICriterion<TSubject>> Criteria { get; }

        /// <summary>
        /// Gets a value indicating whether the evaluation passed (true) or failed (false).
        /// </summary>
        bool Passed { get; }
    }

    /// <summary>
    /// Describes a criterion that can be applied to a subject of type <typeparamref name="TSubject"/>.
    /// Criteria are definitions (meta-level) that specify evaluation rules.
    /// </summary>
    /// <typeparam name="TSubject">The subject type that the criterion targets.</typeparam>
    public interface ICriterion<out TSubject> : IAnalyticsDefinition where TSubject : IConcept { }

    // Diagnostics

    /// <summary>
    /// Represents a diagnostic message or condition discovered by analytics.
    /// Diagnostics carry a code, human-readable message and severity level.
    /// </summary>
    public interface IDiagnostic : IConcept
    {
        /// <summary>
        /// Gets the machine-readable diagnostic code used to categorize the diagnostic.
        /// </summary>
        string Code { get; }

        /// <summary>
        /// Gets the human-readable message describing the diagnostic condition.
        /// </summary>
        string Message { get; }

        /// <summary>
        /// Gets the severity level of the diagnostic.
        /// </summary>
        Severity Severity { get; }
    }

    /// <summary>
    /// Severity levels for diagnostics.
    /// </summary>
    public enum Severity
    {
        /// <summary>Informational message; no action required.</summary>
        Info,

        /// <summary>Indicates a potential issue or suboptimal condition.</summary>
        Warning,

        /// <summary>Indicates an error condition that requires attention.</summary>
        Error,

        /// <summary>Critical condition requiring immediate remediation.</summary>
        Critical
    }
}