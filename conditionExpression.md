# Condition expression — Minimal API

This document specifies a small ConditionExpression API. The surface provides core factories and a concise set of combinators and helpers to compose boolean expression trees used by the host runtime.

---

## Summary

Factory functions:
- \`true()\` => ConditionExpression
- \`false()\` => ConditionExpression

Combinators / helpers on ConditionExpression:
- \`and(other: ConditionExpression) => ConditionExpression\`
- \`or(other: ConditionExpression) => ConditionExpression\`
- \`negate() => ConditionExpression\` (logical NOT)
- \`ifTrue(cb: () => ConditionExpression) => ConditionExpression\` (invoke cb only if the receiver evaluates to true)
- \`ifFalse(cb: () => ConditionExpression) => ConditionExpression\` (invoke cb only if the receiver evaluates to false)

Expressions are immutable and evaluation is performed by the runtime when the expression is applied.

---

## Rationale

Callback-based \`ifTrue\` and \`ifFalse\` provide explicit, lazy branching: the callback is not invoked unless the receiver's truth value dictates it. This improves control over side effects and allows building dependency-free expression trees where expensive sub-expressions are only constructed/evaluated when necessary.

---

## Host API (TypeScript)

\`\`\`ts
export type ConditionExpression = {
  /** Short-circuiting combinators. Immutable. */
  and: (other: ConditionExpression) => ConditionExpression;
  or:  (other: ConditionExpression) => ConditionExpression;

  /** Logical inversion. */
  negate: () => ConditionExpression;

  /** Convenience combinators that accept callbacks producing ConditionExpression values lazily. */
  ifTrue:  (cb: () => ConditionExpression) => ConditionExpression; // invoke cb only when receiver is true
  ifFalse: (cb: () => ConditionExpression) => ConditionExpression; // invoke cb only when receiver is false
};

export type ConditionExpressionApi = {
  /** Factory functions (exact names required by spec) */
  'true':  () => ConditionExpression;
  'false': () => ConditionExpression;

  /** Marker for HostApi surfaces */
  type: ConditionExpressionType;
};
\`\`\`

Notes:
- All nodes are lazy; factories construct tree nodes and the runtime is responsible for evaluation.
- Callbacks supplied to \`ifTrue\`/\`ifFalse\` must return a ConditionExpression. Callbacks are not invoked until evaluation time and only when the receiver's evaluation result triggers them.

---

## Evaluation semantics

- Lazy evaluation of nodes; combinators use short-circuit semantics:
  - \`and\`: evaluate left; if false, result is false without evaluating right.
  - \`or\`:  evaluate left; if true, result is true without evaluating right.
  - \`negate\`: evaluate operand and invert the boolean result.
  - \`ifTrue(cb)\`: evaluate the receiver (left); if true, invoke \`cb()\` to obtain a ConditionExpression, evaluate that expression and return its result; otherwise, return false without invoking \`cb\` or evaluating the callback result.
  - \`ifFalse(cb)\`: evaluate the receiver; if false, invoke \`cb()\` to obtain a ConditionExpression, evaluate that expression and return its result; otherwise, return false without invoking \`cb\`.

Short-circuiting ensures side-effectful evaluations (if any exist elsewhere in the runtime) are not invoked unless necessary.

---

## Examples

\`\`\`ts
const T = hostApi.boolean.true();
const F = hostApi.boolean.false();

// Callback-based branching — cb not called unless needed
const branch = T.ifTrue(() => F.or(T)); // cb invoked because T is true; equivalent to T.and(F.or(T))

// Lazy fallback
const fallback = T.ifFalse(() => F); // cb not invoked because T is true; equivalent to T.negate().and(F)
\`\`\`

---

## Migration notes

- If previous code used \`ifTrue\`/\`ifFalse\` with direct ConditionExpression arguments, migrate to passing a zero-argument callback returning the expression. Example:

\`\`\`ts
// old
left.ifTrue(right)

// new
left.ifTrue(() => right)
\`\`\`

---

## Failure modes & Edge Cases

- Expressiveness: while richer operators (refs, deterministic choice) remain outside this core surface, \`negate\` and callback-based branching restore common boolean needs.
- Deeply-nested trees: prefer iterative or bounded evaluators to avoid stack overflow.
- Callback side effects: callbacks must be pure or their side-effects must be acceptable because they will run at evaluation time.

Mitigations:
- Keep evaluation in the runtime and avoid introducing side-effects into expression nodes except by design.
- Provide companion utilities if advanced behavior is needed.

---

## Tradeoffs

- + Improves readability and expressiveness while preserving lazy evaluation.
- + Callbacks prevent unnecessary construction/evaluation of heavy sub-expressions.
- - Slightly more complex API surface and a migration step for existing call sites.

---

## Open Questions

- Singleton vs. fresh-node factories for \`true()\`/\`false()\`? (memory vs identity semantics)
- Should evaluation be exposed (e.g., \`ConditionExpression.evaluate(world): boolean\`) for testing/debugging?
- Should expressions be serializable for tooling?

---

## Next Iteration

- Decide on factory identity (singleton vs new instance).
- Consider companion helper module for refs/choices if required.
- Add guidelines for implementing evaluator (iterative evaluation, max depth, logging).

---

Architecture Review Result: Not Implementation Ready — readiness awaits decisions on factory identity and serialization/evaluation helper exposure.
