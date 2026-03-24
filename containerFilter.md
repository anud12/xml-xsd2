# Container Filter — Concepts

## Summary

`ContainerFilter` is an immutable, lazily-evaluated expression that extracts a subset of containers from either a caller-supplied input list (ListExpression) or the global container repository. It understands the engine's container shape (`id`, `type`, `classifications`, `children`, `members`, optional text_map/number_map metadata) and is implemented by composing existing expression primitives (`ConditionExpression`, `StringExpression`, `NumberExpression`, `ListExpression`, and TextMap/NumberMap helpers). Filters are pure, side-effect-free and deterministic when evaluated against the same `ExecutionContext`.

## Purpose

ContainerFilter is a declarative, composable mechanism used by rules, effects, system queries and runtime logic to locate containers. Typical uses:

- target containers for spatial effects (move, spawn, region-wide modifiers)
- locate containers that currently hold matching entities
- provide container lists for UI, analytics, or batching
- serve as inputs for other expressions (e.g., container-based name resolution)

Filters are evaluated against a snapshot of the reading buffer and must not perform writes or observe transient mutation.

## Container shape

Containers are assumed to expose the following read-only shape:

- `id`: String
- `type`: String (semantic type, e.g., "region", "zone", "room")
- `classifications`: List<String>
- `children`: List<ContainerRef>
- `members`: List<EntityRef> (entities currently contained)
- `text_map`: Map<String, List<String>> (optional metadata)
- `number_map`: Map<String, List<Long>> (optional metadata)

(See `containers.md` and `entities.md` for details.)

## Evaluation semantics

- **Source**: A filter is evaluated against either a provided input `ListExpression` (the "source list") or the global container repository. If a source list is provided only containers in that list are considered (order preserved); otherwise the repository is scanned.

- **Laziness**: Filters are represented as `ListExpression` nodes and evaluation is deferred until consumed.

- **Determinism**: Any randomness used must be derived from the `ExecutionContext` so results are repeatable for a given context.

- **Short-circuiting**: Boolean combinators should short-circuit where possible while preserving deterministic semantics.

- **Membership semantics**: When matching `members` (entity lists), predicates operate over entity references or may accept an `EntityFilter`; a container matches if any member satisfies the predicate (existential semantics).

- **Missing keys/metadata**: Absent text_map/number_map keys are treated as empty (no matches).

## Host API

```ts
// Conceptual — builder-style filters in the module API.
type ContainerFilter = {
  // Narrow by id using a StringExpression -> ConditionExpression function
  byId: (fn: (id: StringExpression) => ConditionExpression) => ContainerFilter

  // Narrow by container semantic type (e.g., "region", "zone")
  byType: (typeExpr: StringExpression) => ContainerFilter

  // Match containers that have the given classification/tag
  byClassification: (classification: StringExpression) => ContainerFilter

  // Match containers whose metadata text entry for `key` satisfies `fn`
  hasTextValue: (key: StringExpression, fn: (value: StringExpression) => ConditionExpression) => ContainerFilter

  // Match containers whose metadata numeric entry for `key` satisfies `fn`
  hasNumberValue: (key: StringExpression, fn: (value: NumberExpression) => ConditionExpression) => ContainerFilter

  // Match containers that contain an entity with id matching `fn`
  containsEntityById: (fn: (id: StringExpression) => ConditionExpression) => ContainerFilter

  // Match containers that contain at least one entity matching the provided EntityFilter
  // (cross-reference to EntityFilter host API)
  containsEntityMatching: (entityFilter: EntityFilter) => ContainerFilter

  // Invert another filter (complement relative to the chosen source)
  not: (containerFilter: ContainerFilter) => ContainerFilter

  // Composition helpers (intersection/union)
  and: (...others: ContainerFilter[]) => ContainerFilter
  or:  (...others: ContainerFilter[]) => ContainerFilter
}
```

```ts
// HostApi augmentation: expose container.filter API to modules
export type HostApi = {
  /* ... rest of declarations ... */
  container: ContainerApi
}

export type ContainerApi = {
  // Other container helpers may exist here; this snippet focuses on filter API surface.
  filter: ContainerFilterApi
}

export type ContainerFilterApi = {
  /** Create a new filter builder (start composing predicates) */
  create: () => ContainerFilter

  /** Register a named container filter for reuse in modules/tests */
  asRule: (ruleName: string, filter: ContainerFilter) => ContainerFilterApi

  /** Retrieve a previously registered named filter */
  getRule: (ruleName: string) => ContainerFilter

  /** Marker for HostApi typing */
  type: ContainerFilterType
}

export type ContainerFilterType = {
  // Marker for HostApi typing; implementations may extend if needed.
}
```

Notes:
- The API above is conceptual; concrete host bindings may expose builder functions rather than methods.
- `containsEntityMatching` accepts an `EntityFilter` handle (as defined in `entityFilter.md`) so implementations must resolve/compose cross-repository predicates lazily.

## Container evaluation context

When a predicate callback runs it executes with an implicit "current container" context. Helpers available:

- `id`: `StringExpression` — the container id
- `type`: `StringExpression` — container semantic type
- `text(key: StringExpression)`: `MaybeExpression<StringExpression | ListExpression>` — metadata values
- `number(key: StringExpression)`: `MaybeExpression<NumberExpression | ListExpression>` — numeric metadata values
- `classifications`: `ListExpression<StringExpression>` — container tags
- `children`: `ListExpression<ContainerRefExpression>` — child container refs
- `members`: `ListExpression<EntityRefExpression>` — entity references contained

Predicates must remain pure and use only expression primitives.

## Examples

1) Find containers of type "region":

```ts
const regions = containerApi.filter.create().byType(hostApi.string.of("region"));
```

2) Containers with a numeric capacity >= 50:

```ts
const large = containerApi.filter.create().hasNumberValue(hostApi.string.of("capacity"),
  n => n.gte(hostApi.number.of(50))
);
```

3) Containers that hold at least one entity classified "player":

```ts
const playerContainers = containerApi.filter.create().containsEntityMatching(
  hostApi.entity.getRule("players") // assumes a named EntityFilter "players" is registered
);
```

4) Regions that are not classified as "deprecated":

```ts
const activeRegions = containerApi.filter.create()
  .byType(hostApi.string.of("region"))
  .and(containerApi.filter.create().not(containerApi.filter.create().byClassification(hostApi.string.of("deprecated"))));
```

(Examples are conceptual — host bindings may provide fluent builders or factory helpers.)

## Performance & implementation notes

- **Index lookups**: Provide efficient indexes for id, type, classification and a reverse mapping from entity id -> member containers to efficiently implement containsEntityById/containsEntityMatching without full scans.
- **Lazy evaluation**: Keep filters lazy until consumed by a list-producing operator or an enclosing evaluation.
- **Short-circuiting**: Use short-circuit evaluation for boolean combinators.
- **Memory**: Materialization should yield references to existing container objects in the reading-buffer snapshot, avoid deep copies.
- **Cycle detection**: Parent/child graphs may contain cycles in malformed data; traversal helpers must guard against infinite recursion with configurable depth limits.
- **Caching**: Cache per-container predicate results for the duration of a single filter evaluation pass.

## Failure modes and edge cases

- malformed expressions: surface deterministic errors rather than partial results
- very large container hierarchies: provide runtime-side limits and prefer source-limiting lists for callers
- concurrent reads: filters read from the reading-buffer snapshot and must not rely on live mutations
- cycles: parent/child traversals must detect cycles and handle them gracefully
- missing member references: tolerate dangling entity refs (log and treat as non-matching)

## Determinism & Randomness

Any randomized helper used for selection must draw randomness exclusively from `ExecutionContext`. Filters themselves must not mutate the context.

## Cross-references

- `containers.md` — container model and fields
- `entities.md` — entity model and EntityFilter cross-usage
- `conditionExpression.md`, `stringExpression.md`, `numberExpression.md` — expression primitives used to build predicates

## Implementation checklist

- [ ] Evaluate ContainerFilter lazily as a `ListExpression`
- [ ] Provide index-backed implementations for byId/byType/byClassification/containsEntity
- [ ] Expose ContainerFilterApi in HostApi (create/asRule/getRule)
- [ ] Ensure predicates run in a Container evaluation context with the helpers listed above
- [ ] Document concrete host bindings in modules.md

---

This document provides a stable, implementation-agnostic specification of ContainerFilter semantics, composition and expected behavior.
