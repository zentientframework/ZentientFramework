# **ZENT-SPEC-CORE-000**

### *Constitutional Specification for Zentient.Core*

**Status:** Final
**Layer:** 1 (Base)
**Stability:** Ultra-stable
**Dependency:** None
**Parent:** ZENT-SPEC-FRAMEWORK-000

---

## **1. Purpose**

The Core layer defines the **structural substrate** of the entire Zentient Framework.
It provides the *minimum conceptual machinery* required to support:

* identity
* immutability
* ordering
* deterministic async flows
* concept modeling
* registries
* low-level primitives

Nothing outside Core may redefine these primitives.

---

## **2. Core Design Principles**

1. **Minimalism:** Only structural concepts belong here.
2. **Purity:** No business semantics, no meaning layer, no outcomes.
3. **Determinism:** All behaviors are stable under identical inputs.
4. **Immutability:** All Core types are deeply immutable.
5. **Async-first:** Cancellation-propagating async is the default.
6. **Predictability:** No domain exceptions; only invariant errors.

---

## **3. Concept System**

Core defines the conceptual identity system:

### 3.1 IConcept

Every central abstraction implements `IConcept`:

* globally stable ID
* type identity
* optional short name
* deterministic equality

Concrete concept types cannot leak through public APIs in higher layers.

### 3.2 Concept Taxonomy

Core defines the four structural categories:

1. **Atomic Concepts** (primitives: ids, spans, hashes)
2. **Composite Concepts** (immutable structures)
3. **Registry Concepts** (global catalogs, lazy populated)
4. **Context Concepts** (ambient-free context objects passed explicitly)

Nothing above Core may introduce a fifth category.

---

## **4. Registries**

Registries support deterministic concept lookup:

* immutable snapshots
* sealed implementations
* no global state
* thread-safe lazy get-or-add
* observable slow path
* deterministic iteration ordering

Registries are *not* service locators; they are structural catalogs.

---

## **5. Execution Model**

Core defines:

* `ZExecutionContext` — explicit environment carrier
* deterministic cancellation propagation
* zero ambient state
* monotonic tracing

All higher layers must use this model.

---

## **6. Serialization**

Core defines the abstract serialization primitives:

* deterministic
* ordering-stable
* no runtime-type polymorphism
* type-info-free binary/text representations

Specific formats live outside Core.

---

## **7. Compliance Requirements**

Any type in Core:

* MUST be immutable
* MUST be sealed or private
* MUST have deterministic behavior
* MUST avoid reflection for core paths
* MUST avoid throwing domain exceptions

Violations break compatibility.

---

# End of ZENT-SPEC-CORE-000
