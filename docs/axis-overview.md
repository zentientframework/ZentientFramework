# Zentient Axes Overview

| Axis         | Enum Type          | Attribute/Contract        | Required In |
|--------------|-------------------|--------------------------|-------------|
| Ontic        | PresenceMode, BeingKind   | [OnticAttribute]        | IConcept, IEntity, IDefinition |
| Temporal     | TemporalIndex, MutationSemantics | [TemporalAttribute]   | IEntity, IValue |
| Policy       | PolicyDomain, EnforcementTier, ObligationType | [PolicyAttribute] | IDefinition, IPolicy |
| Structure    | LinkType, RoleTaxonomy | [StructureAttribute]     | IRelation |
| Context      | ContextBinding, ScopeKind | [ContextAttribute]      | IConfigurationSource, IContextual |
| Capability   | CapabilityDeclaration, ConditionType, CapabilityState | [CapabilityAttribute] | ICapability, IOperation |
| Evidence     | EvidenceModel, EvidenceStrength, ProvenanceType | [EvidenceAttribute] | IMetric, IProvenance |