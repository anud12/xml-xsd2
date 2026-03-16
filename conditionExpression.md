# Condition expression — Minimal API

This document specifies a small ConditionExpression API. The surface provides core factories and a concise set of combinators and helpers to compose boolean expression trees used by the host runtime.

---

## Summary

Factory functions:
- `true()` => ConditionExpression
- `false()` => ConditionExpression

Combinators / helpers on ConditionExpression:
- `and(other: ConditionExpression) => ConditionExpression`
- `or(other: ConditionExpression) => ConditionExpression`
- `negate() => ConditionExpression` (logical NOT)
- `ifTrue(other: ConditionExpression) => ConditionExpression` (evaluate other only if left is true)
- `ifFalse(other: ConditionExpression) => ConditionExpression` (evaluate other only if left is false)

Expressions are immutable and evaluation is performed by the runtime when the expression is applied.

---

## Rationale

Adding `negate`, `ifTrue` and `ifFalse` gives callers readable, intent-bearing operations while keeping the core surface intentionally small. 
- `negate` supplies logical inversion as a first-class node.
- `ifTrue` and `ifFalse` are convenience combinators that express common conditional composition without requiring callers to write explicit negation or nested combinators.

These helpers do not force eager evaluation — they construct lazy tree nodes whose semantics are honored by the runtime evaluator.

---

## Host API (TypeScript)

```ts
export type ConditionExpression = {
  /** Short-circuiting combinators. Immutable. */
  and: (other: ConditionExpression) => ConditionExpression;
  or:  (other: ConditionExpression) => ConditionExpression;

  /** Logical inversion. */
  negate: () => ConditionExpression;

  /** Convenience combinators. Evaluate `other` only when the receiver's truth value matches the condition. */
  ifTrue:  (other: ConditionExpression) => ConditionExpression; // equivalent to receiver.and(other) semantics
  ifFalse: (other: ConditionExpression) => ConditionExpression; // equivalent to receiver.negate().and(other)
};

export type ConditionExpressionApi = {
  /** Factory functions (exact names required by spec) */
  'true':  () => ConditionExpression;
  'false': () => ConditionExpression;

  /** Marker for HostApi surfaces */
  type: ConditionExpressionType;
};
```

Notes:
- All nodes are lazy; factories construct tree nodes and the runtime is responsible for evaluation.
- `ifTrue` and `ifFalse` are provided for readability; they compose to the same semantics as combinations of `and` and `negate`.

---

## Evaluation semantics

- Lazy evaluation of nodes; combinators use short-circuit semantics:
  - `and`: evaluate left; if false, result is false without evaluating right.
  - `or`:  evaluate left; if true, result is true without evaluating right.
  - `negate`: evaluate operand and invert the boolean result.
  - `ifTrue(other)`: evaluate left; if true, evaluate `other` and return its result; otherwise return false without evaluating `other`.
  - `ifFalse(other)`: evaluate left; if false, evaluate `other` and return its result; otherwise return false without evaluating `other`.

Short-circuiting ensures side-effectful evaluations (if any exist elsewhere in the runtime) are not invoked unless necessary.

---

## Examples

```ts
const T = hostApi.boolean.true();
const F = hostApi.boolean.false();

const notT = T.negate(); // logical NOT

const composed = T.ifTrue(F.or(T)); // evaluate right side only if T is true; equivalent to T.and(F.or(T))

const fallback = T.ifFalse(F); // evaluate F only if T is false; equivalent to T.negate().and(F)
```

---

## Migration notes

- If previous code used other boolean helpers (not, ref, oneOf, of, etc.), map them to companion APIs or to the combinators here where appropriate.
- `ifTrue` and `ifFalse` are convenience aliases—calls may be refactored to `and` and `negate().and()` if desired.

---

## Failure modes & Edge Cases

- Expressiveness: while richer operators (refs, deterministic choice) remain outside this core surface, `negate`, `ifTrue` and `ifFalse` restore common boolean needs.
- Deeply-nested trees: prefer iterative or bounded evaluators to avoid stack overflow.
- Side effects: callers must rely on documented short-circuiting guarantees.

Mitigations:
- Keep evaluation in the runtime and avoid introducing side-effects into expression nodes.
- Provide companion utilities if advanced behavior is needed.

---

## Tradeoffs

- + Improves readability and expressiveness without substantially increasing API surface.
- + Keeps evaluation model lazy and predictable.
- - Still omits repository refs and deterministic choice; those belong to companion APIs.

---

## Open Questions

- Singleton vs. fresh-node factories for `true()`/`false()`? (memory vs identity semantics)
- Should evaluation be exposed (e.g., `ConditionExpression.evaluate(world): boolean`) for testing/debugging?
- Should expressions be serializable for tooling?

---

## Next Iteration

- Decide on factory identity (singleton vs new instance).
- Consider companion helper module for refs/choices if required.
- Add guidelines for implementing evaluator (iterative evaluation, max depth, logging).

---
Architecture Review Result: Not Implementation Ready — readiness awaits decisions on factory identity and serialization/evaluation helper exposure.

