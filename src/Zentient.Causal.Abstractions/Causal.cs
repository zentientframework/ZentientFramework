namespace Zentient.Causal
{
    using System;
    using System.Collections.Generic;

    using Zentient.Core;

    public interface ICausalConcept : IConcept { }
    public interface ICausalDefinition : IDefinition<ICausalConcept> { }

    // Correlation
    public interface ICorrelatable : ICausalConcept
    {
        IReadOnlyCollection<ICorrelationId> CorrelationIds { get; }
    }

    public interface ICorrelationId : ICausalConcept
    {
        string Namespace { get; }
        string Value { get; }
    }

    // Provenance
    public interface IProvenance<out TSubject> : ICausalConcept where TSubject : IConcept
    {
        TSubject Subject { get; }
        IReadOnlyList<IOrigin> Origins { get; }
        ILineage? Lineage { get; }
    }

    public interface IOrigin : ICausalConcept
    {
        string OriginId { get; }
        DateTimeOffset OccurredAt { get; }
        EvidenceModel Type { get; set; } // From Evidence Axis
    }

    public interface ILineage : ICausalConcept
    {
        IReadOnlyCollection<ILineageEdge> Edges { get; }
    }

    public interface ILineageEdge : ICausalConcept
    {
        string FromNodeId { get; }
        string ToNodeId { get; }
        string Predicate { get; }
    }
}
