# Maybe / Optional expression — Concepts

This document describes the core `MaybeExpression` (aka Optional) concept.

## Summary

`MaybeExpression` is an immutable, lazily-evaluated expression model representing optional values: a value that may be present (Some) or absent (None). It composes with other expressions via `map`, `flatMap`, `filter`, and explicit unwrapping (`orElse`). The design favors explicit unwrapping to avoid surprising implicit coercions between present/absent values.

## Purpose

Provide a first-class, composable optional/nullable expression type for host scripts and rules so code can safely represent computed values that may not exist. Typical uses:
- Represent optional lookups (e.g., repository ref that might not exist)
- Model conditional computations that may not produce values
- Compose safely with mapping/flat-mapping semantics without throwing by default

## Conversions (JS ↔ host)

- Input (JS -> host):
  - `hostApi.maybe.of(expr)` wraps an expression/value into a `MaybeExpression` that will evaluate as present.
  - `hostApi.maybe.none()` produces an absent `MaybeExpression`.


- Output (host -> JS):
  - Evaluating a `MaybeExpression` yields either a present host value or an absent marker. Host bindings SHOULD represent absence as `null` in JS (or an idiomatic host Optional type in typed hosts such as Java's `Optional<T>`). Consumers should prefer `orElse`/`map`/`flatMap` rather than relying on raw host null checks.

## Evaluation semantics

`MaybeExpression` nodes evaluate lazily. Node kinds:
- `Some(expr)` — present; evaluating `expr` produces the contained value.
- `None` — absent; evaluating returns absent marker.

Operations:
- `map(fn)` — if present, evaluate contained value, then invoke `fn(value)` to produce a new value which is wrapped as `Some(result)`; if absent, return `None`.
- `flatMap(fn)` — similar to `map` but `fn` returns a `MaybeExpression` directly and is flattened.
- `filter(predicate)` — if present and `predicate(value)` evaluates to true, keep value; otherwise return `None`.
- `isPresent()` / `isEmpty()` — return a `ConditionExpression` evaluating to true/false depending on presence.
- `orElse(defaultExpr)` — returns the contained value when present; otherwise evaluates and returns `defaultExpr`.


- `get()` — return the value (convenience; prefer `orElse`).
- `ifPresent(cb)` — side-effecting: if present, evaluate and invoke callback with value; returns void.

Failure semantics:
- By default the runtime is *fail-soft*: if evaluating an inner expression throws or a referenced rule is missing, the operation treats it as `None` and logs the event. A strict mode can be provided to convert these into thrown errors for CI/test runs.

Determinism
- Presence may depend on deterministic choices (`oneOf`, `randomFrom`) contained within inner expressions; those choices follow the same instance RNG semantics as other expressions.

## Host API (TypeScript)

```ts
export type HostApi = {
  /* ...rest of declarations... */
  maybe: MaybeExpressionApi<any>
}

export type MaybeExpressionApi = {
  /** Wrap an expression/value as present */
  of: <T>(v: T) => MaybeExpression<T>,
  /** Create an absent value */
  none: () => MaybeExpression,

  /** Register/lookup named maybe rules (optional) */
  asRule: (ruleName: string, expr: MaybeExpression<T>) => MaybeExpressionApi,
  getRule: (ruleName: string) => MaybeExpression<unknown>,
  type: MaybeExpressionType,
}

export type MaybeExpressionType = {
  // marker for HostApi surfaces
}

export type MaybeExpression<T> = {
  of: (v: T) => MaybeExpression<T>,
  none: () => MaybeExpression<T>,

  /** Presence checks */
  isPresent: () => ConditionExpression,
  isEmpty: () => ConditionExpression,

  /** Transformations */
  map: <U>(mapper: (v: T) => U) => MaybeExpression<U>,
  flatMap: <U>(mapper: (v: T) => MaybeExpression<U>) => MaybeExpression<U>,
  filter: (predicate: (v: T) => ConditionExpression) => MaybeExpression<T>,

  /** Unwrapping */
  orElse: (defaultValue: T) => T,

  /** Side-effects */
  ifPresent: (cb: (v: T) => void) => void,
}
```

Notes:
- `T` above denotes the contained value type; concrete HostApi bindings SHOULD provide typed helpers where feasible (e.g., Maybe<StringExpression> helpers).


## Examples

- Simple present/absent

```ts
const maybeName = hostApi.maybe.of(hostApi.string.of("Alice"));
maybeName.isPresent(); // ConditionExpression -> true
const nameOrAnon = maybeName.orElse(hostApi.string.of("Anonymous"));
// nameOrAnon evaluates to the inner string when present, otherwise "Anonymous".
```

- Mapping

```ts
const maybeLen = maybeName.map(s => hostApi.number.of(/* length extract expression */));
// if maybeName is absent, maybeLen is None
```

- Chaining & flatMap

```ts
const maybeTitle = hostApi.maybe.of(getOptionalTitle()); // JS->host
const greeting = maybeTitle
  .map(t => hostApi.string.of("Sir ").concat(hostApi.string.of(t)))
  .orElse(hostApi.string.of("Friend"));
```

- Side-effects

```ts
maybeName.ifPresent(name => hostApi.string.asRule("greeting", hostApi.string.of("Hello ").concat(hostApi.string.of(name))));
```

## Interop with other expressions

Design decision (explicit unwrapping preferred): expressions that expect a concrete type (e.g., `StringExpression.concat`) SHALL require callers to pass a concrete `StringExpression`. Passing a `Maybe<StringExpression>` must be explicitly unwrapped (e.g., `maybeString.orElse(hostApi.string.of(""))`).

Alternative (implicit coercion) is dangerous because absent values might silently become context-dependent defaults and lead to subtle bugs. Explicit unwrapping reduces surprises and is recommended for predictable behavior.

## Repository & Validation

- `asRule` / `getRule` may be supported: rules that produce optional results can be registered and looked up during evaluation.
- Provide tooling helpers that enumerate maybe-rule ids for validation.
- If the implementation mirrors XML-backed model, follow `LinkedNode` invariants and detect cycles.

## Failure modes & Edge cases

- Exceptions during inner evaluation: default is fail-soft → treat as `None` and log. Strict mode can convert to thrown errors.
- Unwrapping mistakes: callers forgetting to `orElse` may receive `null` values — make this visible in runtime logs and consider linting/tooling to catch common patterns.
- Nested `Maybe` values: `map` vs `flatMap` semantics must be clear; implementations MUST flatten on `flatMap`.
- Deterministic randomness inside inner expressions: presence can depend on `oneOf` choices; deterministic RNG semantics apply.
- Cyclic refs: detect and cap recursion depth (configurable, e.g., 16) to avoid infinite expansion.

Mitigations:
- Provide `orElse` convenience functions and typed helpers to encourage explicit unwrapping.
- Provide strict vs fail-soft runtime modes configurable per instance.
- Add linter/editor checks for common misuse (e.g., passing Maybe where concrete expected).

## Tradeoffs

- + Explicit unwrapping reduces surprising implicit semantics.
- + Composable map/flatMap makes functional-style transformations easy and expressive.
- - Extra ceremony for callers: must unwrap when mixing with other expression types.
- - Strict vs fail-soft modes increase runtime configuration surface.

## Next Iteration

- Decide host representation for absence: JS `null` vs an explicit host Maybe wrapper vs typed `Optional<T>` in Java bindings.
- Decide default runtime behavior: fail-soft or strict; prefer fail-soft for player-facing runtime, strict for CI/tests.
- Add typed host helpers (e.g., Maybe<StringExpression>) to reduce caller friction.
- Add linter rules or compile-time checks to flag likely misuse.

---

Architecture Review Result: Not Implementation Ready — awaits decisions on default unwrapping behavior, host absence representation (null vs Optional), and strict vs fail-soft runtime mode.
