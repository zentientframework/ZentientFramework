# **ZENT-SPEC-METADATA-000**

### *Constitutional Specification for Zentient.Metadata*

**Status:** Final
**Layer:** 2 (Meaning Layer)
**Stability:** Stable
**Depends on:** Zentient.Core

---

## **1. Purpose**

Metadata is the authoritative **meaning layer** of the Zentient Framework.
It defines:

* semantic keys
* type-safe metadata storage
* schemas
* merge rules
* cardinality
* validation
* provenance integration

Metadata expresses “what something *means*,” not “what happened.”

---

## **2. Metadata Keys**

### 2.1 Strongly Typed Keys

A key is defined as:

* an identifier (`Id`)
* a value type (`T`)
* a cardinality (Single/List/Set/Map)
* an optional validator
* an optional category

These define metadata *meaning*, not behavior.

### 2.2 Key Rules

1. A key’s ID is globally unique.
2. A key never changes cardinality.
3. A key’s type is fixed forever.
4. Keys are immutable concepts.
5. Keys may be attached to any metadata container.

---

## **3. Metadata Containers (IMetadata)**

Metadata containers:

* are deeply immutable
* behave like typed dictionaries
* must not expose underlying concrete implementations
* must support deterministic merges
* must allow multi-valued entries
* must guarantee stable iteration order

---

## **4. Mutation Model**

Mutation must not throw for domain failure.

All mutation uses the *dual API model*:

1. **Direct mutation (With / Merge / Without)**

   * Valid only when schema permits
   * throws only for invariants, nulls, or corruption

2. **Try... variants**

   * return `bool`
   * output updated metadata
   * provide reason/conflicts

### Forbidden:

* ambient mutation
* in-place mutation
* exceptions as business logic

---

## **5. Schemas**

### 5.1 Purpose

A schema defines what keys:

* are required
* are allowed
* are forbidden
* must follow which merge rule

### 5.2 Merge Rules

* **Overwrite**
* **Combine**
* **Reject**

Schemas:

* must be immutable
* must evaluate deterministically
* must expose conflict lists for TryMerge

Schemas MUST NOT:

* require runtime-type inspection
* depend on Results

---

## **6. Merge Semantics**

Merge is:

* deterministic
* schema-controlled
* cardinality-aware
* stable-ordering

Conflicts must be:

* identifiable
* stable
* serializable
* surfaced via TryMerge

---

## **7. General Purpose Metadata**

Zentient.Metadata ships with first-class concept keys for:

* provenance
* classification
* tags
* audit
* lifecycle
* timestamps
* schemas
* confidence levels

These are part of the platform.

---

## **8. Compliance Requirements**

A compliant implementation MUST:

* maintain immutability
* maintain determinism
* provide Try APIs
* hide all concrete types
* expose only façade models
* enforce schemas without throwing domain exceptions
* propagate provenance metadata

Metadata may never depend on Results.

---

# End of ZENT-SPEC-METADATA-000
