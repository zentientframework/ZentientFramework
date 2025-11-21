# Zentient.Core

Concrete implementations of the Zentient.Core.

This package provides:

- Internal concrete types implementing the public abstractions.
- Public static facades for creation and registry operations.
- A small, DX-first design that keeps implementations internal to the package but exposes friendly factories.

Guiding principles:

- Internal implementations to keep the contract package stable.
- Factories for ergonomic creation and discoverability.
- Thread-safe registry and minimal dependencies.

