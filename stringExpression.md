# String expression — Concepts

This document describes the core `String expression` concept.

## Summary

`StringExpression` is an `immutable`, `lazily-evaluated` expression model representing game strings as host `String` values. The host API builds expression trees via `of`, `concat`, `group`, `ref`, and `oneOf` (deterministic choice). `of()` eagerly converts a JS `string` to the host `String` (and caches it); other operations produce nodes evaluated on extraction.

From JS, a `StringExpression` is a truthy wrapper — implicit string coercion should throw. Composition follows the same token-style semantics used by `name` rules: literal segments (prefixes), optional `ref` lookups, and `one_of` groups for deterministic selection.

## Purpose

Provide a composable, deterministic string construction API that mirrors the `name` rule model (tokens with `prefix`, `ref`, `one_of`) but exposes it as a programmable expression algebra to hosts and scripts.

Use cases:
- Build complex text from constants, referenced rule entries, and deterministic choices.
- Reuse fragments via rule registration (`asRule` / `getRule`).
- Preserve deterministic randomness via the instance random table.

## Conversions (JS ↔ host)

- Input (JS -> host): `null`/`undefined` → error. A JS `string` passed to `of()` becomes a host `String` and is cached.
- Output (host -> JS): The evaluated result is a host `String`. The `StringExpression` wrapper is intentionally not implicitly coerced to JS primitive; explicit evaluation (instance API) is required.

## Evaluation semantics

When a `StringExpression` is extracted, nodes are evaluated recursively in declaration order:

- Literal (`of`) nodes return their value.
- `ref(ruleId)` resolves `ruleId` through the string repository (see Validation). If the ref is unresolved, the implementation SHOULD fail-soft and substitute an empty segment (logging the missing ref). A strict mode can be provided for CI.
- `oneOf(list)` selects exactly one entry via the deterministic selection API (`worldStepInstance.randomFrom(list)`), then evaluates the chosen expression.
- `group(expr)` influences grouping/evaluation order (useful for nested choice boundaries).


## Host API

The server exposes the following TypeScript declaration (augmenting HostApi):

```typescript
export type HostApi = {
  /* ... rest of declarations ... */
  string: StringExpressionApi
}

export type StringExpressionApi = {
  /** Create a literal */
  of: (s: string) => StringExpression,
  /** Register an expression under a named rule for later getRule(ref) lookups */
  asRule: (ruleName: string, expr: StringExpression) => StringExpressionApi,
  /** Retrieve an API scoped to a previously registered rule */
  getRule: (ruleName: string) => StringExpressionApi,
  type: StringExpressionType,
}

export type StringExpressionType = {
  // used when declaring argument types dynamically in HostApi clients
}

export type StringExpression = {
  /** Convenience to create a literal */
  of: (s: string) => StringExpression,
  /** Concatenate two expressions */
  concat: (other: StringExpression) => StringExpression,
  /** Join multiple expressions using an optional separator */
  join: (parts: StringExpression[], separator?: StringExpression) => StringExpression,
  /** Convenience: prefix a literal string to this expression */
  prefix: (s: string) => StringExpression,
  /** Convenience: sufix a literal string to this expression */
  sufix: (s: string) => StringExpression,
  /** Grouping node to control evaluation order */
  group: (expr: StringExpression) => StringExpression,
  /** Deterministic choice among alternatives */
  oneOf: (choices: StringExpression[]) => StringExpression,
  /** Reference another rule by id (resolved at evaluation time) */
  ref: (ruleId: string) => StringExpression,
  /** Return index of first possible match of `other` inside this expression's language, or -1 if none */
  indexOfExpression: (other: StringExpression, fromInclusive?: any) => any,
  /** Check whether this expression MAY produce a string that contains any possible evaluation of `other`.
   *  Returns a ConditionExpression.
   */
  containsExpression: (other: StringExpression) => ConditionExpression,
  /** Optional simple transforms (implementation may provide) */
  upper?: () => StringExpression,
  lower?: () => StringExpression,
  trim?: () => StringExpression,
}
```

Notes:
- `of()` is eagerly computed and cached to allow small constant optimizations; other constructors remain lazy.
- `asRule` / `getRule` mirror the registration/lookups required to resolve `ref(ruleId)` when evaluating; this follows the same repository/indexing pattern used by `name` rules.

## Resolution algorithm (runtime)

- Start from a `StringExpression` or a `ref` string.
- If the node is a literal → return the literal.
- If `concat` → evaluate left, evaluate right, return left + right.
- If `join(parts, separator)` → evaluate each element of `parts` in declaration order, evaluate `separator` if present (defaults to empty string), then join the evaluated parts using the separator and return the result.
- If `group` → evaluate contained expression and return result.
- If `ref(ruleId)` → repository lookup: `stringRepository.getEntryById(ruleId)` → evaluate the registered expression for that entry. If not found, substitute empty string (fail-soft) and log.
- If `oneOf(list)` → pick index = deterministicRandomIndex(list.size()) using `worldStepInstance.randomFrom(list)` semantics (same inclusive behavior as name); evaluate the chosen entry and return.
- `indexOfExpression(other)` / `containsExpression(other)` — set-semantic evaluation:
  - Semantics: exists s in L(this) and t in L(other) such that t is a substring of s.
  - Implementation approaches:
    1. Small-finite enumeration: if both languages are provably small (product of oneOf branch counts under threshold), enumerate expansions of `this` and `other` and perform substring/index search.
    2. Automata-based: convert expressions to NFAs (treating concat/oneOf/group/of/ref-expanded-as-NFA) and check emptiness of intersection between NFA(this) and Σ* · NFA(other) · Σ* (i.e., does there exist an accepted string with a substring matching other?). This is the robust solution for large or combinatorial expressions.
    3. Conservative fallback: if expressions contain non-regular transforms (complex regex replace, dynamic repeat counts, or transforms that break regular-language assumptions), either conservatively return MAYBE (implementation choice) or require the operation to be unsupported for those nodes.
  - Refs: expand `ref` nodes via repository lookups; detect recursion and cap expansion depth (configurable, e.g., 16) to avoid infinite automata.
  - Deterministic randomness: `containsExpression` is existential — it checks whether some valid choice sequence could produce the match. This differs from runtime evaluation (which uses deterministic `randomFrom` to pick a single branch). If a runtime deterministic check is needed, evaluate both expressions under the same instance RNG and perform a concrete substring check instead.

- Return an Optional-like result e.g., a NumberExpression with -1/0/1 semantics (see API above). Implementations on Java should provide `Optional<Long>` / `OptionalInt` or `Optional<Boolean>` as appropriate.

## Examples

- Simple concat:
```ts
const hello = hostApi.string.of("Hello, ");
const nameExpr = hostApi.string.of("Alice");
const greeting = hello.concat(nameExpr); // "Hello, Alice" when evaluated
```

- Using refs (registering a rule):
```ts
hostApi.string.asRule("title", hostApi.string.of("Gallant"));
const hero = hostApi.string.of("Sir ").concat(hostApi.string.ref("title"));
// Evaluating hero -> "Sir Gallant"
```

- Deterministic choice (`oneOf`):
```ts
const colours = hostApi.string.oneOf([hostApi.string.of("red"), hostApi.string.of("blue"), hostApi.string.of("green")]);
// worldStepInstance.randomFrom([...]) chooses one index deterministically
```

- Join example:
```ts
const words = [hostApi.string.of("red"), hostApi.string.of("blue"), hostApi.string.of("green")];
const joined = hostApi.string.of("").join(words, hostApi.string.of(", "));
// Evaluating joined -> "red, blue, green"
```

- containsExpression example (oneOf-aware):
```ts
// s may evaluate to "AC" or "BC"
const s = hostApi.string.oneOf([hostApi.string.of("A"), hostApi.string.of("B")]).concat(hostApi.string.of("C"));
const a = hostApi.string.of("A");
// containsExpression is existential: returns true because "AC" contains "A"
const containsA = s.containsExpression(a);
```

- Nested example `(A|B)|(B|C)`:
```ts
const left = hostApi.string.oneOf([hostApi.string.of("A"), hostApi.string.of("B")]);
const right = hostApi.string.oneOf([hostApi.string.of("B"), hostApi.string.of("C")]);
const s2 = hostApi.string.oneOf([left, right]);
// s2.containsExpression(hostApi.string.of("A")) -> 1
```

## Repository & Validation

Follow the same repository/indexing pattern used by `name` rules:

- Build `id -> Entry` maps at boot by streaming rule groups → string rules → entries.
- Provide an attribute validator `StringRuleRefValidator.getAllowedValues(WorldStep)` which streams all registered entry ids (for tooling and tests).
- Maintain `LinkedNode` invariants if the implementation mirrors the XML-backed model.

## Failure modes & Action items

- Unresolved refs: current expected default is fail-soft (empty string) with logging; provide an optional strict mode for CI.
- Random selection correctness: ensure index computation is inclusive and matches `name`'s implementation (`int idx = (int)Math.floor(random() * size);`).
- Complexity: automata conversion and product construction can explode. Provide heuristics: enumerate for small choices, automata for larger ones, and sensible timeouts/limits.
- Non-regular transforms: either disallow in contains/indexOfExpression or implement conservative approximations.

---

This spec aligns `StringExpression` behavior with the `number` expression semantics (immutability, lazy evaluation, host wrappers) while adopting the `name` composability primitives (literal/prefix, `ref`, `one_of`) so host scripts can build deterministic, composable string expressions.

