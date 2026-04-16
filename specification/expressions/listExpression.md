# List expression — Concepts

This document describes the core `List expression` concept.

## Summary

`ListExpression` is an `immutable`, `lazily-evaluated` expression model representing ordered sequences (lists/arrays) of element expressions. The host API builds expression trees via `of`, `concat`, `append`, `group`, `get`, `length`, `contains`, `forEach`, `oneOf`, and `randomElement`. `of(...items)` accepts variable arguments of element expressions and eagerly constructs a host literal list when all items are literal. Other ops produce nodes evaluated on extraction.

Lists are typed by their element expressions (e.g., lists of strings, numbers, or nested expressions). From JS the `ListExpression` wrapper is truthy and not implicitly coerced — explicit evaluation or element access is required.

## Purpose

Provide a composable, deterministic collection API that:
- Represents ordered sequences of element expressions usable by host code and rules
- Allows building and registering reusable list fragments (`asRule`/`getRule`)
- Preserves deterministic randomness (for `randomElement` and `oneOf`) via the instance RNG table

Use cases: inventories, deterministic choice lists, argument lists for other expressions, and reusable sequence fragments.

## Conversions (JS ↔ host)

- Input (JS -> host): `null`/`undefined` → error. Arguments passed to `of()` become the list elements; the API accepts variable arguments (e.g., `of(a, b, c)` or `of(expr1, expr2)`). Element values that are primitives are wrapped using the appropriate element-type `of()` where applicable.
- Output (host -> JS): Evaluating a `ListExpression` returns a host sequence (e.g., List<T> or array) whose elements are the evaluated results of the element expressions. Element evaluation uses the semantics of the element expression type (String, Number, etc.).

## Evaluation semantics

- `of(...items)`: accepts variable arguments; if all items are literal primitives, `of()` eagerly converts and caches the literal list. If items include expression nodes, `of()` stores references and evaluation of elements is deferred until list evaluation.
- `concat(other)`: at evaluation, evaluate left then right and return concatenated sequence.
- `append(item)`: evaluate receiver to a sequence, evaluate `item` and append resulting single element.
- `group(expr)`: controls grouping/evaluation boundaries for composed list nodes.
- `get(index:NumberExpression)`: evaluate the list, evaluate `index` (NumberExpression semantics apply), and return a `MaybeExpression` containing the element when present. When the index is out-of-bounds the result is an absent `MaybeExpression` (hostApi.maybe.none()) and the event is logged. Implementations MAY provide a strict mode that throws instead.
- `length()`: returns a `NumberExpression` representing the number of elements (evaluated at extraction time).

- `forEach(cb: (element: any, index?: number) => void)`: evaluate the list; for each element, evaluate the element expression to a host value and invoke `cb(elementValue, index)`. Callbacks are invoked at evaluation time for side-effects; the forEach expression returns void (or host `null`). Implementations MAY provide a strict mode for error handling.
- `map(cb: (elementExpr: any, index?: number) => any)`: lazily transform each element by invoking `cb` with the element expression (not the evaluated value). `cb` should return an expression or literal; `map` produces a new ListExpression where each element is the callback's result. This preserves laziness and allows map callbacks to build new expression trees rather than performing side-effects.

- `isContaining(element)`: returns a `ConditionExpression` that is true if some evaluated element equals the provided element (semantic depends on element equality rules).
- `oneOf(choices: ListExpression[])`: treat the list expression as a set of alternative lists and pick one deterministically using the instance RNG. Returns a `MaybeExpression` that contains the chosen `ListExpression` when choices are present; when the choices are empty the result is an absent `MaybeExpression` (hostApi.maybe.none()).
- `randomElement()`: selects one element using deterministic instance RNG semantics (see randomness.md). Returns a `MaybeExpression` containing the chosen element when the list is non-empty; when the list is empty the result is an absent `MaybeExpression` (hostApi.maybe.none()).


Short-circuiting: evaluation is lazy across nodes; implementations should avoid eagerly constructing or enumerating large lists.

## Host API (TypeScript)

### API Structure

**ListOperations** is the factory and operation builder:
```ts
export type ListOperations = {
  /** Create a literal list (elements may be primitives or expression wrappers). Accepts variable arguments. */
  of: <T> (...items: T[]) => ListExpression<T>;
  
  /** Build an operation: concatenate two lists */
  concat: <T>(other: ListExpression<T>) => ListOperations,
  
  /** Build an operation: append a single element */
  append: <T>(element: T) => ListOperations,
  
  /** Build an operation: grouping node to control evaluation order */
  group: <T>(expr: ListExpression<T>) => ListOperations,
  
  /** Build an operation: lazy transformation of each element */
  map: <T>(cb: (elementExpr: T, index?: number) => any) => ListOperations,
  
  /** Evaluate this operation sequence against a given value */
  evaluate: <T>(value: ListExpression<T>) => ListExpression<T>,
  
  /** Register and retrieve named list rules */
  asRule: (ruleName: string, expr: ListExpression<unknown>) => ListOperations,
  getRule: (ruleName: string) => ListExpression<unknown>,
}
```

**ListExpression** is the lazy expression tree (composition only):
```ts
export type ListExpression<T> = {
  /** Apply an operation to transform the current value. Returns self for chaining. */
  apply: (operation: ListOperations) => ListExpression<T>;
  
  /** Replace the current value entirely (reset point). Returns self for chaining. */
  set: (value: ListExpression<T>) => ListExpression<T>;
  
  /** Concatenate two lists */
  concat: <T>(other: ListExpression<T>) => ListExpression<T>;

  /** Append a single element */
  append: <T>(element: T) => ListExpression<T>;

  /** Grouping node to control evaluation order */
  group: <T>(expr: ListExpression<T>) => ListExpression<T>;

  /** Zero-based index access; returns a MaybeExpression containing the evaluated element when present */
  get: (index: NumberExpression) => MaybeExpression<T>;

  /** Length as a NumberExpression */
  length: () => NumberExpression;

  /** Iterate elements and invoke cb(elementValue:any, index?:number) for side-effects. Callback invoked at evaluation time. Returns void. */
  forEach: (cb: (element: T, index?: number) => void) => void;

  /** Existential membership test returning a ConditionExpression */
  isContaining: (element: T) => ConditionExpression;

  /** Treat a collection of list alternatives and pick one whole list deterministically.
   *  Accepts an array of ListExpression alternatives and returns a MaybeExpression containing the chosen ListExpression.
   */
  oneOf?: (choices: ListExpression<T>[]) => MaybeExpression<T>;

  /** Deterministic selection of an element using the instance RNG (returns MaybeExpression) */
  randomElement: () => MaybeExpression<T>;
}
```

### Implementation Notes

- **`ListExpression<T>` is immutable** with an operation queue. The underlying list value never changes; only the queued operations grow.
- **`.apply(operation)`** appends the operation to the queue and returns `this` for chaining.
- **`.set(value)`** discards the current queue and replaces the value with a new one. Returns `this` for chaining.
- **`of(...items)` is eagerly computed and cached** for literal items only; other constructors remain lazy.
- **Sequential execution**: operations in the queue apply in declaration order when the expression is evaluated.

Notes:
- `any` above denotes an element expression or literal; concrete HostApi implementations SHOULD provide typed helpers (e.g., List<StringExpression>) or specialized helpers such as `listOfStrings` for ergonomic host bindings.
- `of(...items)` is eagerly computed and cached for literal items only; other constructors remain lazy.
- `forEach(...)` invokes callbacks at evaluation time and is explicitly side-effecting; callers should avoid non-deterministic side-effects and prefer pure transforms where possible.
- See also: [MaybeExpression](./maybeExpression.md) — the optional/absent value contract used by `get()`, `randomElement()`, and `oneOf()`; read there for `map`/`flatMap`/`orElse`/`ifPresent` semantics and fail-soft vs strict modes.Use `map(...)` when a pure transformation of elements into new expressions is desired.

## Examples

```ts
// list of string expressions
const palette = hostApi.list.of(hostApi.string.of("red"), hostApi.string.of("blue"));
hostApi.list.asRule("palette", palette);

const firstMaybe = palette.get(hostApi.number.of(0)); // MaybeExpression that may contain a StringExpression
const size = palette.length(); // NumberExpression

// deterministic random element
const colMaybe = palette.randomElement(); // MaybeExpression of chosen element; unwrap with orElse(...) or ifPresent(...)

// concatenation
const more = palette.concat(hostApi.list.of(hostApi.string.of("green")));

// side-effecting iteration (callbacks run at evaluation time)
const names = hostApi.list.of(hostApi.string.of("Alice"), hostApi.string.of("Bob"));
names.forEach((name, idx) => {
  // `name` is the evaluated host string when callback runs.
  // Callback may perform host-side actions such as rule registration or logging.
  hostApi.string.asRule(`greeting-${idx}`, hostApi.string.of("Hello ").concat(hostApi.string.of(name)));
});

// lazy mapping — build a transformed ListExpression without evaluating elements now
const nums = hostApi.list.of(hostApi.number.of(1), hostApi.number.of(2));
const doubled = nums.map(nExpr => nExpr.multiply(hostApi.number.of(2))); // ListExpression of expressions
// later, evaluating `doubled` will evaluate each expression and return `[2,4]`
```

## Repository & Validation

Follow the repository/indexing pattern used by other rule types:

- Build `id -> Entry` maps at boot by streaming rule groups → list rules → entries.
- Provide tooling helpers that enumerate registered list ids for validation and editors.
- If the implementation mirrors the XML-backed model, maintain `LinkedNode` invariants and validate cycles.

## Failure modes & Edge Cases

- Out-of-bounds access: `get(index)` returns an absent `MaybeExpression` (hostApi.maybe.none()) by default (fail-soft); strict mode can throw.
- Empty lists: `randomElement()` on empty lists returns an absent `MaybeExpression` (hostApi.maybe.none()) (fail-soft).
- Deeply-nested or huge lists: avoid naive expansion; provide streaming or capped enumeration.
- Cyclic rule refs: detect and cap expansion depth (configurable) to avoid infinite loops.
- Element typing: mixing element types in a list can lead to runtime errors; prefer typed list helpers.
- Side-effects from `forEach`: callbacks may mutate shared state, register rules, or otherwise introduce non-determinism. Mitigations: prefer pure callbacks, document evaluation order, provide strict deterministic evaluation rules, and consider disabling side-effects in strict mode.

Mitigations:
- Provide strict vs fail-soft modes via runtime config.
- Implement lazy iterators and enumerators with depth/cost limits.
- Supply typed host helpers (e.g., list.ofStrings) to avoid mixed-type surprises.

## Tradeoffs

- + Simple, expressive API for ordered sequences.
- + Reuses existing rule repository and deterministic randomness semantics.
- - Generic `any` element surface is flexible but sacrifices type safety; specialized typed helpers improve ergonomics.
- - Implementing robust `isContaining` for complex element expressions may require automata or bounded enumeration.

## Next Iteration

- Define typed list variants (List<StringExpression>, List<NumberExpression>) and ergonomic HostApi helpers.
- Add streaming/iterator evaluation primitives to avoid full-materialization of large lists.
- Specify strict vs fail-soft behavior in the runtime contract and test cases.

---

Architecture Review Result: Not Implementation Ready — awaits decisions on element typing, OOB behavior, and streaming/iterator exposure.
