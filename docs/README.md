# Zentient Framework

A modular, concept-centric .NET ecosystem for ontology, analytics, config, governance, diagnostics, and rich DI.

## Principles

- Thin `Zentient.Abstractions` Canonical core: only IConcept, axes, and principal attributes
- Area packages: Ontology, Analytics, Config, Governance, DI, SDK, Adapters
- Seven-axis semantic lattice: Ontic, Temporal, Policy, Structure, Context, Capability, Evidence (see /charter.md)
- Mix-in governance and provenance interfaces
- Advisory analyzers with strict mode for CI enforcement
- SDK and adapters live in parallel for UX and integration

## Layout

See [`canonical-spec.md`](./canonical-spec.md) and [`charter.md`](./charter.md) for governance, axes, and invariants.

## Getting Started

- Add Zentient.Abstractions for the core `IConcept` and axes.
- Add specific area packages (e.g., Zentient.Ontology.Abstractions) per your domain needs.
- Reference SDK and Adapters for DX, integration, and runtime provisioning.
