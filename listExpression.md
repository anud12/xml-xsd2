# List expression — Concepts

This document describes the core `List expression` concept.

## Summary

`ListExpression` is an `immutable`, `lazily-evaluated` expression model representing ordered sequences (lists/arrays) of element expressions. The host API builds expression trees via `of`, `concat`, `append`, `group`, `get`, `length`, `contains`, and `randomElement`. `of(...items)` accepts variable arguments of element expressions and eagerly constructs a host literal list when all items are literal. Other ops produce nodes evaluated on extraction.

Lists are typed by their element expressions (e.g., lists of strings, numbers, or nested expressions). From JS the `ListExpression` wrapper is truthy and not implicitly coerced — explicit evaluation or element access is required.

## Purpose

Provide a composable, deterministic collection API that:
- Represents ordered sequences of element expressions usable by host code and rules
- Allows building and registering reusable list fragments (`asRule`/`getRule`)
- Preserves deterministic randomness (for `randomElement`) via the instance RNG table

Use cases: inventories, deterministic choice lists, argument lists for other expressions, and reusable sequence fragments.

## Conversions (JS ↔ host)

- Input (JS -> host): `null`/`undefined` → error. Arguments passed to `of()` become the list elements; the API accepts variable arguments (e.g., `of(a, b, c)` or `of(expr1, expr2)`). Element values that are primitives are wrapped using the appropriate element-type `of()` where applicable.
- Output (host -> JS): Evaluating a `ListExpression` returns a host sequence (e.g., List<T> or array) whose elements are the evaluated results of the element expressions. Element evaluation uses the semantics of the element expression type (String, Number, etc.).

## Evaluation semantics

- `of(...items)`: accepts variable arguments; if all items are literal primitives, `of()` eagerly converts and caches the literal list. If items include expression nodes, `of()` stores references and evaluation of elements is deferred until list evaluation.
- `concat(other)`: at evaluation, evaluate left then right and return concatenated sequence.
- `append(item)`: evaluate receiver to a sequence, evaluate `item` and append resulting single element.
- `group(expr)`: controls grouping/evaluation boundaries for composed list nodes.
- `get(index:NumberExpression)`: evaluate the list, evaluate `index` (NumberExpression semantics apply), and return the element at that zero-based index. Out-of-bounds → fail-soft: return `null` (or host `Optional.empty`) and log. Implementations MAY provide a strict mode to throw instead.
- `length()`: returns a `NumberExpression` representing the number of elements (evaluated at extraction time).
- `indexOf(element)`: existential search over evaluated elements; returns a NumberExpression index or -1 when not found.
- `containsExpression(element)`: returns a `ConditionExpression` that is true if some evaluated element equals the provided element (semantic depends on element equality rules).
- `randomElement()`: selects one element using deterministic instance RNG semantics (see randomness.md). If the list is empty, fail-soft and return `null`.


Short-circuiting: evaluation is lazy across nodes; implementations should avoid eagerly constructing or enumerating large lists.

## Host API (TypeScript)

```ts
export type HostApi = {
  /* ... rest of declarations ... */
  list: ListExpressionApi
}

export type ListExpressionApi = {
  /** Create a literal list (elements may be primitives or expression wrappers). Accepts variable arguments. */
  of: (...items: any[]) => ListExpression;

  /** Register and retrieve named list rules. */
  asRule: (ruleName: string, expr: ListExpression) => ListExpressionApi;
  getRule: (ruleName: string) => ListExpression;

  /** Marker for HostApi surfaces */
  type: ListExpressionType;
}

export type ListExpressionType = {
  // used when declaring argument types dynamically in HostApi clients
}

export type ListExpression = {
  /** Convenience to create a literal (delegates to ListExpressionApi.of) */
  of: (...items: any[]) => ListExpression;

  /** Concatenate two lists */
  concat: (other: ListExpression) => ListExpression;

  /** Append a single element */
  append: (element: any) => ListExpression;

  /** Grouping node to control evaluation order */
  group: (expr: ListExpression) => ListExpression;

  /** Zero-based index access; returns evaluated element or null when OOB */
  get: (index: NumberExpression) => any;

  /** Length as a NumberExpression */
  length: () => NumberExpression;

  /** Index of first matching element; -1 if none */
  indexOf: (element: any) => NumberExpression;

  /** Existential membership test returning a ConditionExpression */
  containsExpression: (element: any) => ConditionExpression;

  /** Deterministic selection of an element using the instance RNG */
  randomElement: () => any;


}
```

Notes:
- `any` above denotes an element expression or literal; concrete HostApi implementations SHOULD provide typed helpers (e.g., List<StringExpression>) or specialized helpers such as `listOfStrings` for ergonomic host bindings.
- `of(...items)` is eagerly computed and cached for literal items only; other constructors remain lazy.

## Examples

```ts
// list of string expressions
const palette = hostApi.list.of(hostApi.string.of("red"), hostApi.string.of("blue"));
hostApi.list.asRule("palette", palette);

const first = palette.get(hostApi.number.of(0)); // StringExpression
const size = palette.length(); // NumberExpression

// deterministic random element
const col = palette.randomElement(); // picks 'red' or 'blue' by instance RNG

// concatenation
const more = palette.concat(hostApi.list.of(hostApi.string.of("green")));
```

## Repository & Validation

Follow the repository/indexing pattern used by other rule types:

- Build `id -> Entry` maps at boot by streaming rule groups → list rules → entries.
- Provide tooling helpers that enumerate registered list ids for validation and editors.
- If the implementation mirrors the XML-backed model, maintain `LinkedNode` invariants and validate cycles.

## Failure modes & Edge Cases

- Out-of-bounds access: `get(index)` returns `null` by default (fail-soft); strict mode can throw.
- Empty lists: `randomElement()` on empty lists returns `null` (fail-soft).
- Deeply-nested or huge lists: avoid naive expansion; provide streaming or capped enumeration.
- Cyclic rule refs: detect and cap expansion depth (configurable) to avoid infinite loops.
- Element typing: mixing element types in a list can lead to runtime errors; prefer typed list helpers.

Mitigations:
- Provide strict vs fail-soft modes via runtime config.
- Implement lazy iterators and enumerators with depth/cost limits.
- Supply typed host helpers (e.g., list.ofStrings) to avoid mixed-type surprises.

## Tradeoffs

- + Simple, expressive API for ordered sequences.
- + Reuses existing rule repository and deterministic randomness semantics.
- - Generic `any` element surface is flexible but sacrifices type safety; specialized typed helpers improve ergonomics.
- - Implementing robust `containsExpression` / indexOf for complex element expressions may require automata or bounded enumeration.

## Next Iteration

- Define typed list variants (List<StringExpression>, List<NumberExpression>) and ergonomic HostApi helpers.
- Add streaming/iterator evaluation primitives to avoid full-materialization of large lists.
- Specify strict vs fail-soft behavior in the runtime contract and test cases.

---

Architecture Review Result: Not Implementation Ready — awaits decisions on element typing, OOB behavior, and streaming/iterator exposure.
