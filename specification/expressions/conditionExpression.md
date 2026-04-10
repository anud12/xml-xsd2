# Condition expression — Minimal API

This document specifies a small ConditionExpression API. The surface provides core factories, rule registration and lookup helpers, and a concise set of combinators and helpers to compose boolean expression trees used by the host runtime.

---

## Summary

Factory / rule functions:
- `of(value: boolean) => ConditionExpression`
- `asRule(ruleName: string, expr: ConditionExpression) => ConditionExpressionApi`
- `getRule(ruleName: string) => ConditionExpression`

Combinators / helpers on ConditionExpression:
- `and(other: ConditionExpression) => ConditionExpression`
- `or(other: ConditionExpression) => ConditionExpression`
- `negate() => ConditionExpression` (logical NOT)
- `ifTrue(cb: () => ConditionExpression) => ConditionExpression` (invoke cb only if the receiver evaluates to true)
- `ifFalse(cb: () => ConditionExpression) => ConditionExpression` (invoke cb only if the receiver evaluates to false)

Expressions are immutable and evaluation is performed by the runtime when the expression is applied.

---

## Rationale

Callback-based `ifTrue` and `ifFalse` provide explicit, lazy branching: the callback is not invoked unless the receiver's truth value dictates it. This improves control over side effects and allows building dependency-free expression trees where expensive sub-expressions are only constructed/evaluated when necessary.

---

## Host API (TypeScript)

### API Structure

**ConditionOperations** is the factory and combinator builder:
```ts
export type ConditionApi = {
  /** Create a constant boolean value (returns ConditionExpression immediately) */
  of: (value: boolean) => ConditionExpression;
  
  /** Build a combinator: logical AND */
  and: (other: ConditionExpression) => ConditionOperations;
  
  /** Build a combinator: logical OR */
  or: (other: ConditionExpression) => ConditionOperations;
  
  /** Build a combinator: logical NOT */
  negate: () => ConditionOperations;
  
  /** Evaluate this combinator sequence against a given condition */
  evaluate: (value: ConditionExpression) => ConditionExpression;
  
  /** Register and retrieve named condition rules */
  asRule: (ruleName: string, expr: ConditionExpression) => ConditionOperations;
  getRule: (ruleName: string) => ConditionExpression;
  
  /** Marker for HostApi surfaces */
  type: ConditionExpressionType;
};

export type ConditionExpressionType = {
  // marker for dynamic HostApi typing
};
```

**ConditionExpression** is the lazy expression tree (composition only):
```ts
export type ConditionExpression = {
  /** Apply a combinator to build a new condition tree. Returns self for chaining. */
  apply: (operation: ConditionOperations) => ConditionExpression;
  
  /** Replace current value (reset point). Returns self for chaining. */
  set: (value: ConditionExpression) => ConditionExpression;
  
  /** Short-circuiting AND combinator. Immutable. */
  and: (other: ConditionExpression) => ConditionExpression;
  
  /** Short-circuiting OR combinator. Immutable. */
  or: (other: ConditionExpression) => ConditionExpression;

  /** Logical inversion. */
  negate: () => ConditionExpression;

  /** Convenience combinator that accepts callback producing ConditionExpression lazily. */
  ifTrue: (cb: () => ConditionExpression) => ConditionExpression;
  
  /** Convenience combinator that accepts callback producing ConditionExpression lazily. */
  ifFalse: (cb: () => ConditionExpression) => ConditionExpression;
};
```

### Implementation Notes

- **`ConditionExpression` is immutable** with a combinator queue. The underlying truth value never changes; only the queued combinators grow.
- **`.apply(operation)`** appends the combinator to the queue and returns `this` for chaining.
- **`.set(value)`** discards the current queue and replaces the value with a new one. Returns `this` for chaining.
- **Short-circuit semantics** apply during evaluation: `and` stops on first false, `or` stops on first true.
- **Sequential execution**: combinators in the queue apply in declaration order when the expression is evaluated.

Notes:
- All nodes are lazy; factories construct tree nodes and the runtime is responsible for evaluation.
- `of(value: boolean)` returns a fresh literal node on each call. Callers must not rely on object identity across separate `of(...)` invocations.
- `asRule(ruleName, expr)` registers or replaces the named rule in the condition rule repository and returns the API surface for fluent host usage.
- `getRule(ruleName)` returns a `ConditionExpression` that resolves the named rule at evaluation time.
- Callbacks supplied to `ifTrue`/`ifFalse` must return a ConditionExpression. Callbacks are not invoked until evaluation time and only when the receiver's evaluation result triggers them.

---

## Evaluation semantics

- Lazy evaluation of nodes; combinators use short-circuit semantics:
  - `getRule(ruleName)`: produce a rule-reference expression. At evaluation time, resolve `ruleName` from the condition rule repository and evaluate the resolved expression.
  - `and`: evaluate left; if false, result is false without evaluating right.
  - `or`:  evaluate left; if true, result is true without evaluating right.
  - `negate`: evaluate operand and invert the boolean result.
  - `ifTrue(cb)`: evaluate the receiver (left); if true, invoke `cb()` to obtain a ConditionExpression, evaluate that expression and return its result; otherwise, return false without invoking `cb` or evaluating the callback result.
  - `ifFalse(cb)`: evaluate the receiver; if false, invoke `cb()` to obtain a ConditionExpression, evaluate that expression and return its result; otherwise, return false without invoking `cb`.

Short-circuiting ensures side-effectful evaluations (if any exist elsewhere in the runtime) are not invoked unless necessary.

---

## Examples

```ts
const T = hostApi.boolean.of(true);
const F = hostApi.boolean.of(false);

hostApi.boolean.asRule("isEnabled", T);
const isEnabled = hostApi.boolean.getRule("isEnabled");

// Callback-based branching — cb not called unless needed
const branch = isEnabled.ifTrue(() => F.or(T)); // cb invoked because isEnabled resolves to true

// Lazy fallback
const fallback = T.ifFalse(() => F); // cb not invoked because T is true; equivalent to T.negate().and(F)
```

---

## Failure modes & Edge Cases

- Expressiveness: while richer operators (refs, deterministic choice) remain outside this core surface, `negate` and callback-based branching restore common boolean needs.
- Deeply-nested trees: prefer iterative or bounded evaluators to avoid stack overflow.
- Callback side effects: callbacks must be pure or their side-effects must be acceptable because they will run at evaluation time.
- Missing rules: `getRule(ruleName)` requires a defined repository entry; unresolved names must fail predictably or be specified explicitly by the runtime.

Mitigations:
- Keep evaluation in the runtime and avoid introducing side-effects into expression nodes except by design.
- Provide companion utilities if advanced behavior is needed.
- Define missing-rule behavior explicitly in the runtime contract, including logging and whether resolution is fail-fast or fail-soft.

---

## Tradeoffs

- + Improves readability and expressiveness while preserving lazy evaluation.
- + Callbacks prevent unnecessary construction/evaluation of heavy sub-expressions.
- + Named rules let callers share reusable condition fragments across host code.
- - Slightly more complex API surface and a migration step for existing call sites.

---

## Next Iteration

- Consider companion helper module for refs/choices if required.
- Add guidelines for implementing evaluator (iterative evaluation, max depth, logging).

---

Architecture Review Result: Not Implementation Ready — readiness awaits decisions on serialization and evaluation helper exposure.
