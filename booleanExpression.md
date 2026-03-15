# Condition expression — Concepts

This document describes the core `Condition expression` concept.

## Summary

`ConditionExpression` is an `immutable`, `lazily-evaluated` expression model representing game booleans as host `boolean` values. The host API builds expression trees via `of`, `and`, `or`, `not`, `xor`, `implies`, `group`, `ref`, `oneOf` (deterministic choice). `of()` eagerly converts a JS `boolean` to the host `boolean` (and caches it); other ops produce nodes evaluated on extraction.

From JS, a `ConditionExpression` is a truthy wrapper — implicit boolean coercion should throw. Evaluation of binary operators uses short-circuit semantics (see below) and choices use the same deterministic random table as other expressions.

## Purpose

Provide a composable, deterministic boolean algebra usable from host scripts that mirrors the `number` and `string` expression semantics while integrating with the rule repository model (`asRule` / `getRule`) used by `name` rules.

Use cases:
- Compute condition values used in guards, rule activation, or conditional expression selection.
- Build complex, reusable boolean fragments with refs to rule entries.
- Support deterministic randomness for unit tests and reproducible runs.

## Conversions (JS ↔ host)

- Input (JS -> host): only JS `boolean` values are accepted by `of()`; other types must be explicitly converted by callers. `null`/`undefined` → error.
- Output (host -> JS): Evaluated result is a host `boolean`. The `ConditionExpression` wrapper is intentionally not implicitly coerced to JS primitive; explicit evaluation is required.

## Evaluation semantics

- Evaluation is lazy: nodes are evaluated when the expression is extracted.
- Short-circuiting:
  - `and`: evaluate left; if false, return false without evaluating the right operand.
  - `or`: evaluate left; if true, return true without evaluating the right operand.
  - `implies` is implemented as `!A || B` with left-to-right evaluation and short-circuiting.
- `oneOf(list)` selects exactly one entry via the deterministic selection API (`worldStepInstance.randomFrom(list)`), then evaluates the chosen branch.
- `ref(ruleId)` resolves via the boolean repository; unresolved refs default to `false` (fail-soft) and are logged unless strict mode is enabled.

## Host API

The server exposes the following TypeScript declaration (augmenting HostApi):

```typescript
export type HostApi = {
  /* ... rest of declarations ... */
  boolean: ConditionExpressionApi
}

export type ConditionExpressionApi = {
  of: (b: boolean) => ConditionExpression,
  asRule: (ruleName: string, expr: ConditionExpression) => ConditionExpressionApi,
  getRule: (ruleName: string) => ConditionExpressionApi,
  type: ConditionExpressionType,
}

export type ConditionExpressionType = {
  // used when declaring argument types dynamically in HostApi clients
}

export type ConditionExpression = {
  /** Literal */
  of: (b: boolean) => ConditionExpression,
  /** Logical ops (short-circuiting where applicable) */
  and: (other: ConditionExpression) => ConditionExpression,
  or: (other: ConditionExpression) => ConditionExpression,
  xor: (other: ConditionExpression) => ConditionExpression,
  not: () => ConditionExpression,
  implies: (other: ConditionExpression) => ConditionExpression,
  equals: (other: ConditionExpression) => ConditionExpression,
  /** Deterministic choice among alternatives (evaluate one branch) */
  oneOf: (choices: ConditionExpression[]) => ConditionExpression,
  /** Reference another rule by id (resolved at evaluation time) */
  ref: (ruleId: string) => ConditionExpression,
  /** Grouping node to control evaluation order */
  group: (expr: ConditionExpression) => ConditionExpression,
  /** Interop helpers */
  toNumber?: () => any, // NumberExpression: true->1, false->0
  toString?: () => any, // StringExpression: "true"/"false"
}
```

Notes:
- `of()` is eagerly computed and cached; other constructors are lazy.
- `asRule` / `getRule` provide repository-backed resolution semantics for `ref(ruleId)`.

## Resolution algorithm (runtime)

- Start from a `ConditionExpression` or a `ref` string.
- If the node is a literal → return the boolean value.
- If `not` → evaluate operand and negate the result.
- If `and` → evaluate left; if false, return false; otherwise evaluate right and return result.
- If `or` → evaluate left; if true, return true; otherwise evaluate right and return result.
- If `xor` → evaluate both operands (no short-circuit) and return left !== right.
- If `implies` → evaluate left; if false return true; otherwise evaluate right and return right.
- If `oneOf(list)` → pick index = deterministicRandomIndex(list.size()) using `worldStepInstance.randomFrom(list)` semantics; evaluate chosen branch and return.
- If `ref(ruleId)` → repository lookup: `booleanRepository.getEntryById(ruleId)` → evaluate registered expression. If not found, return `false` (fail-soft) and log.
- For Java-based implementations consider returning `Optional<Boolean>` where appropriate.

## Examples

- Simple logical operations:
```ts
const a = hostApi.boolean.of(true);
const b = hostApi.boolean.of(false);
const r = a.and(b).not(); // !(true && false) -> true
```

- Using refs:
```ts
hostApi.boolean.asRule("isAdult", hostApi.boolean.of(true));
const check = hostApi.boolean.ref("isAdult");
// Evaluating check -> true
```

- oneOf example (choose boolean branch):
```ts
const choice = hostApi.boolean.oneOf([hostApi.boolean.of(true), hostApi.boolean.of(false)]);
// deterministic selection of true or false depending on instance random table
```

- Short-circuiting example (right side not evaluated if left decides):
```ts
const sideEffect = hostApi.boolean.ref("sideEffectRule");
const guard = hostApi.boolean.of(false).and(sideEffect);
// Evaluating guard -> false; sideEffect is NOT evaluated
```

## Repository & Validation

- Build `id -> Entry` maps at boot by streaming rule groups → boolean rules → entries (same pattern as `name`/`string`).
- Provide `BooleanRuleRefValidator.getAllowedValues(WorldStep)` which streams registered entry ids for tooling and tests.
## Failure modes & Action items

- Unresolved refs: default fail-soft (false) with logging; provide strict mode for CI.
- Recursion: detect cycles and cap evaluation/expansion depth (configurable, e.g., 16) to avoid infinite loops.
- Randomness: ensure `oneOf` selection use deterministic table with inclusive behavior matching other expression types.
- Short-circuiting: ensure implementation respects short-circuit to avoid unintended evaluation of side-effecting refs.

Suggested tests:
- Short-circuit correctness tests where right-hand refs should not be invoked when left decides.
- Deterministic random probability boundary tests (0.0, 1.0, inclusive midpoints).
- Repository/ref resolution tests and strict mode behavior.

---

This spec brings ConditionExpression in line with NumberExpression and StringExpression semantics while supplying the boolean algebra primitives and deterministic, repository-backed resolution used across the system.

