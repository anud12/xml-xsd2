# Entity Filter — Concepts

## Summary

`EntityFilter` is an immutable, lazily-evaluated expression that extracts a subset of entities from either a caller-supplied input list (ListExpression) or the global entity repository. It understands the engine's entity shape (`id`, `text_map`, `number_map`, `containers`, `classifications`) and is implemented by composing existing expression primitives (`ConditionExpression`, `StringExpression`, `NumberExpression`, `ListExpression`, and TextMap/NumberMap helpers). Filters are pure, side-effect-free and deterministic when evaluated against the same `ExecutionContext`.

## Purpose

EntityFilter is a declarative, composable mechanism used by rules, effects, UI queries and runtime logic to locate entities. Filters are used to:

- select targets for effects (add/remove properties, move between containers)
- act as source lists for other expressions

Because filters are evaluated against a snapshot of the reading buffer must not observe transient mutation.

## Entity shape

Entities are assumed to expose the following read-only shape:

- `id`: String
- `classifications`: List<String>
- `text_map`: Map<String, List<String>> (each key maps to zero-or-more string values)
- `number_map`: Map<String, List<Long>> (each key maps to zero-or-more numeric values)
- `containers`: List<ContainerRef> (references to containers this entity is a member of)

(See `entities.md` and `containers.md` for details.)

## Evaluation semantics

- **Source**: A filter is evaluated against either:
  - a provided input `ListExpression` (the "source list"), or
  - the global entity repository (all indexed entities).
  If a source list is provided, only entities in that list are considered (order preserved). Otherwise the entire repository is scanned.

- **Laziness**: Filters are represented as `ListExpression` nodes and evaluated lazily. Composed filters do not iterate the source until the containing `ListExpression` is evaluated.

- **Determinism**: When evaluation objects use randomness (e.g., tie-breaking helpers or explicit random selection operators), randomness is derived from the `ExecutionContext` (World Seed, Tick, Source, Action, Call Index) so results are deterministic for a given context.

- **Short-circuiting**: Boolean combinators (AND/OR) should short-circuit for performance where possible, but must preserve deterministic behavior and not observe side-effects.

- **Multi-valued fields**: When matching `text_map` or `number_map` keys, if multiple values exist for the key the predicate is evaluated against each value; a match occurs if any value satisfies the predicate (existential semantics).

- **Missing keys**: If a `text_map` or `number_map` key is absent the value is treated as empty (no matches).

## Host API

```ts
// Conceptual — the runtime exposes builder-style filters in the module API.
type EntityFilter = {
  // Narrow by id using a StringExpression -> ConditionExpression function
  byId: (fn: (id: StringExpression) => ConditionExpression) => EntityFilter

  // Match where an entity has a text_map entry for the given key,
  // and at least one value for that key satisfies `fn`
  hasTextValue: (key: StringExpression, fn: (value: StringExpression) => ConditionExpression) => EntityFilter

  // Same semantics for numeric values (number_map)
  hasNumberValue: (key: StringExpression, fn: (value: NumberExpression) => ConditionExpression) => EntityFilter

  // Match entities that are members of containers matched by the given ContainerFilter
  hasContainer: (containerFilter: ContainerFilter) => EntityFilter

  // Invert another filter (set complement relative to the chosen source)
  not: (entityFilter: EntityFilter) => EntityFilter

  // Composition helpers (intersection/union)
  and: (...others: EntityFilter[]) => EntityFilter
  or: (...others: EntityFilter[]) => EntityFilter

}
```

```ts
// HostApi augmentation: expose entity.filter API to modules
export type HostApi = {
  /* ... rest of declarations ... */
  entity: EntityApi
}

export type EntityApi = {
  // Other entity helpers may exist here; this snippet focuses on filter API surface.
  filter: EntityFilterApi
}

export type EntityFilterApi = {
  /** Create a new filter builder (start composing filter predicates) */
  create: () => EntityFilter

  /** Register a named filter in the entity filter repository for reuse in modules/tests */
  asRule: (ruleName: string, filter: EntityFilter) => EntityFilterApi

  /** Retrieve a previously registered named filter by rule id */
  getRule: (ruleName: string) => EntityFilter

  /** Marker for type usage in HostApi clients */
  type: EntityFilterType
}

export type EntityFilterType = {
  // Marker for HostApi typing; implementations may extend if needed.
}
```

Notes:
- The API above is conceptual; concrete host bindings may expose builder functions instead of methods.
- `ConditionExpression`, `StringExpression` and `NumberExpression` callbacks receive expression handles that evaluate in the entity context (see "Entity evaluation context" below).

## Entity evaluation context

When a callback is invoked while evaluating a predicate it runs with an implicit "current entity" context. The following helpers/expressions are available:

- `id`: `StringExpression` — the entity id
- `text(key: StringExpression)`: `MaybeExpression<StringExpression | ListExpression>` — read `text_map` value(s) for key
- `number(key: StringExpression)`: `MaybeExpression<NumberExpression | ListExpression>` — read `number_map` values for key
- `classifications`: `ListExpression<StringExpression>` — entity classifications
- `containers`: `ListExpression<ContainerRefExpression>` — container references

Predicates must use only expression primitives and are evaluated lazily; they must not mutate the world.

## Examples

1) Find NPCs with a "job" text value equal to "blacksmith":

```ts
const filter = entityFilter
  .hasTextValue(hostApi.string.of("job"), value => value.equals(hostApi.string.of("blacksmith")));
```

2) Entities with a numeric "level" >= 10:

```ts
const highLevel = entityFilter.hasNumberValue(hostApi.string.of("level"),
  n => n.gte(hostApi.number.of(10))
);
```

3) Entities in a region container but not classified "ghost":

```ts
const inRegion = entityFilter
  .hasContainer(containerFilter.byId(hostApi.string.of("region_42")))
  .and(entityFilter.not(entityFilter.byClassification(hostApi.string.of("ghost"))))
```

(These examples show conceptual compositions using expression primitives.)

## Performance & implementation notes

- **Index lookups**: Implementations should leverage indexes for id, classification and single-key text/number lookups to avoid full scans when possible.
- **Caching**: Predicate evaluation results may be cached per-entity per-evaluation to avoid redundant computations within a single filter evaluation pass.

## Failure modes and edge cases

- malformed expressions: evaluation should surface deterministic errors rather than partial results.
- very large repositories: callers may provide a source `ListExpression` to limit scope; add server-side limits to protect memory/time.
- concurrent reads: filters read the reading-buffer snapshot and must not rely on live mutations.
- cycles: container-based checks that traverse container graphs must guard against cycles and control recursion depth.

## Determinism & Randomness

Any operator that uses random selection (for tie-breaking or explicit randomized helpers) must derive randomness solely from the `ExecutionContext`. Filters themselves do not change the `ExecutionContext`; they may call into deterministic randomness helpers only.

## Cross-references

- `entities.md` — entity shape and fields
- `containers.md` — ContainerFilter API and semantics
- `conditionExpression.md`, `stringExpression.md`, `numberExpression.md` — expression primitives used to build predicates

## Implementation checklist

- [ ] Evaluate filter lazily as a `ListExpression`
- [ ] Provide efficient index-backed implementations for byId/hasTextValue/hasNumberValue/hasContainer
- [ ] Ensure predicates run in an Entity evaluation context with the fields listed above
- [ ] Document the concrete host bindings (JS/TS) in module API (for modules.md)

---

This doc provides a stable, implementation-agnostic specification of EntityFilter semantics, composition and expected behavior.
