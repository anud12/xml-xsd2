# Entity Filter — Concepts

## Summary

`EntityFilter` is a data-driven, declarative filter that extracts a subset of
entities from the global entity repository. Modules declare filters as **plain
objects** (a `FilterArg`); the runtime interprets them recursively. There is no
source-code parsing, no builder chaining, no named-rule registry, and no
first-entity fallback: a filter either matches the entities it describes or it
matches none.

A filter is evaluated by `EventContext.getEntityBy(filter)`, which returns a
`ListExpression<Entity>` over the matching entities (deterministic repository
order). No match yields an **empty list** plus a `___filter-no-match` log
line; a malformed filter yields an empty list plus a `___filter-malformed`
log line.

## FilterArg

A filter argument is a plain object with **exactly one** of four keys. Each is
a closed variant:

```ts
type FilterLambda = (e: EntityFilterExpression) => ConditionExpression;

type FilterPredicates = { filters: FilterLambda[] };  // leaf: every lambda must hold
type FilterAnd = { and: FilterArg[] };                // every child must match
type FilterOr = { or: FilterArg[] };                  // at least one child matches
type FilterNot = { not: FilterArg[] };                // no child may match

type FilterArg = FilterPredicates | FilterAnd | FilterOr | FilterNot;
type EntityFilter = FilterArg;
```

### Semantics

- `filters`: the entity matches iff every lambda, invoked with the entity
  expression, yields a truthy condition.
- `and`: the entity matches iff every child `FilterArg` matches.
- `or`: the entity matches iff at least one child `FilterArg` matches.
- `not`: the entity matches iff no child `FilterArg` matches.
- **Empty arrays match everything** (uniform vacuous truth), including
  `or: []`. "Match nothing" is expressed with `not`.
- Validation: exactly one of the four keys present, value an array. Anything
  else is malformed → deterministic log + **no matches** (never a partial
  result, never a fallback entity).

### Lambda contract

Every predicate, at every depth, has the same shape:

```ts
(e: EntityFilterExpression) => ConditionExpression
```

The runtime invokes each lambda with a per-entity surface exposing:

- `id` — the entity id as a `StringExpression`
  (`isContainingExactly`, `isContaining`, ...)
- `number_map.get(key)` — a `MaybeExpression<NumberExpression>`: the entity's
  number value for `key`, or an empty Maybe when the key is absent.
- `text_map.get(key)` — a `MaybeExpression<StringExpression>`: the entity's
  text value for `key`, or an empty Maybe when the key is absent.
- `getEntitiesInsideArea(name)` — entities inside a named area
- `containers` — container memberships (currently empty at runtime)

Map lookups return a Maybe rather than a bare value, so **absent keys never
match and never throw**. A predicate lifts the comparison out of the Maybe and
falls back to a falsy condition when the key is absent:

```ts
(e) => e.number_map.get(string.of("hp"))
  .map(n => n.isLessOrEqualTo(number.of(5)))
  .orElse(condition.of(false))
```

The returned value is a `ConditionExpression`; the runtime reads it through one
shared helper that accepts a `{value: boolean}` wrapper and the
`ifTrue`/`ifFalse` protocol.

## Examples

```js
const { string, number, condition } = hostApi.runtime;

// leaf
const smiths = {
  filters: [
    (e) => e.text_map.get(string.of("job"))
      .map(t => t.isContainingExactly(string.of("blacksmith")))
      .orElse(condition.of(false)),
  ],
};

// and
const activeSmiths = {
  and: [
    smiths,
    {
      filters: [
        (e) => e.text_map.get(string.of("state"))
          .map(t => t.isContainingExactly(string.of("active")))
          .orElse(condition.of(false)),
      ],
    },
  ],
};

// or, nested
const triage = {
  or: [
    {
      filters: [
        (e) => e.text_map.get(string.of("role"))
          .map(t => t.isContainingExactly(string.of("healer")))
          .orElse(condition.of(false)),
      ],
    },
    {
      and: [
        {
          filters: [
            (e) => e.number_map.get(string.of("hp"))
              .map(n => n.isLessOrEqualTo(number.of(5)))
              .orElse(condition.of(false)),
          ],
        },
        {
          filters: [
            (e) => e.text_map.get(string.of("team"))
              .map(t => t.isContainingExactly(string.of("red")))
              .orElse(condition.of(false)),
          ],
        },
      ],
    },
  ],
};

// not
const notGhosts = {
  not: [
    {
      filters: [
        (e) => e.text_map.get(string.of("kind"))
          .map(t => t.isContainingExactly(string.of("spirit")))
          .orElse(condition.of(false)),
      ],
    },
  ],
};

// query + mutate through the list result
context.getEntityBy(triage).map(elementExpr => {
  elementExpr.text_map.get(string.of("flag")).ifPresent(v => {
    v.set(string.of("hit"));
  });
});
```

## List result

The returned `ListExpression<Entity>` supports: `map`, `forEach`, `get(index)`
(element accessor with `map`/`flatMap`, including `isCondition` →
`getOnTrueOrFalse`), `size`, `ifPresent`, and `randomElement`. Each element
exposes the same per-entity surface as the lambda parameter (`id`,
`number_map`, `text_map`, `getEntitiesInsideArea`, `containers`).

## Determinism & failure modes

- Evaluation order is the repository's insertion order; results are
  deterministic for a given repository snapshot.
- Malformed filters (more than one variant key, non-array values, a plain
  string, a bare function, a legacy `{ id: fn }` object, null) never produce
  partial results: they log `___filter-malformed` and match nothing.
- The former first-entity fallback is removed: a filter that matches nothing
  returns an empty list (logged as `___filter-no-match`).

## Cross-references

- `entities.md` — entity shape and fields
- `conditionExpression.md`, `stringExpression.md`, `numberExpression.md`,
  `maybeExpression.md` — expression primitives used by predicates
- `application/client/solution/plans/entity-filter.md` — implementation plan

---

This doc describes the implemented, data-driven EntityFilter semantics.
