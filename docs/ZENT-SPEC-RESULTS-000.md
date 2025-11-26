# **ZENT-SPEC-RESULTS-000**

### *Constitutional Specification for Zentient.Results*

**Status:** Final
**Layer:** 3 (Behavior/Outcome Layer)
**Stability:** Stable
**Depends on:** Metadata

---

## **1. Purpose**

Zentient.Results expresses the **outcome semantics** of computations.
It is the behavioral complement to Metadata’s meaning layer.

A Result expresses:

* success
* failure
* uncertain outcomes
* associated metadata
* associated provenance

---

## **2. Structural Model of Results**

Only three canonical outcome states exist:

1. **Success**

   * contains a value
   * contains metadata
   * contains provenance

2. **Failure**

   * contains a reason
   * contains metadata
   * contains provenance

3. **Uncertain**

   * contains a reason
   * contains metadata
   * contains provenance

### There are **no additional states**, subtypes, or special-cases.

---

## **3. Immutability & Encapsulation**

Results:

* are immutable
* wrap internal sealed implementations
* expose only interface-level abstraction
* use façade creators (`Ok`, `Fail`, `Uncertain`)

No consumer can access concrete types.

---

## **4. Functional Laws**

Results MUST satisfy:

### 4.1 Left Identity

`Bind(Ok(x), f)` ≡ `f(x)`

### 4.2 Right Identity

`Bind(r, Ok)` ≡ `r`

### 4.3 Associativity

`Bind(Bind(r, f), g)` ≡ `Bind(r, x => Bind(f(x), g))`

These laws guarantee determinism and composability.

---

## **5. Exception Semantics**

Results MUST NOT use exceptions for:

* failure
* invalid state
* domain constraints

Exceptions are allowed *only* for structural invariants.

Examples of forbidden exceptions:

* “Invalid argument for key X”
* “Operation failed”
* “Missing business field Y”

These MUST be represented:

* as failure results
* as metadata annotations
* as provenance steps

---

## **6. Integration with Metadata**

Every Result instance MUST contain:

* metadata container
* provenance trace

Merges follow the rules of:

* success → success merges metadata
* failure → failure preserves or combines metadata depending on rules
* uncertain → uncertain merges identical

All merges are deterministic.

---

## **7. Async Composition**

All bind/select/transform operations:

* MUST be async-first
* MUST use `ValueTask` for hot paths
* MUST propagate cancellation deterministically
* MUST NOT block threads

Synchronous composition is permitted only when no async operations exist.

---

## **8. Provenance Model**

Every Result must contain a provenance stream describing:

* the steps taken
* the inputs
* the lineage of transformations
* timestamps
* execution context identifiers

Provenance is:

* immutable
* append-only
* deterministic
* mergeable

---

## **9. Utility Contracts**

Results include utility concepts:

* `IReason`
* `IOutcome`
* `IResult<T>`
* `INonGenericResult`

These abstractions:

* must not reveal internal specifics
* must not violate immutability
* must not enable illegal mutation

---

## **10. Compliance Requirements**

A compliant Results layer MUST:

* strictly apply monadic laws
* use only the three allowed states
* hide concrete implementations
* use Try-decorated metadata mutation internally
* be deterministic
* propagate provenance
* support async-first composition
* avoid domain exceptions entirely

Results MUST depend on Metadata.
Metadata MUST NOT depend on Results.

---

# End of ZENT-SPEC-RESULTS-000

---
