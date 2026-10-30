# Entity Filter — Object-Declared, Recursive FilterArgs

## Status (complete)

- Steps 1–4 implemented: `script_entity_filter.rs` shared evaluator (registered
  first in `install_host_api`), real `entity.filter` API in `script_rest.rs`,
  all three context templates delegate `getEntityBy` to `__evalEntityFilter`,
  types rewritten (`suite/types/EntityFilter.d.ts`, `Effect.d.ts`), spec docs
  updated.
- Step 5: 7 scenes under `Tests/GameScene/Stage_14/EntityFilter/`
  (ByIdMatch, FiltersLeaf, AndOrNot, NoMatch, EmptyArrays, Malformed,
  NamedRules) — all passing.
- Full suite green: 189/189 (`dotnet test --settings .runsettings`),
  including legacy consumers (Stage_5 reoccurAfterMs*, Stage_6
  EmitChildEvents, Ffi HotReload, Stage_11 Area).

## Purpose

Replace the current filter mechanism — where `getEntityBy` stringifies the
filter and regex-parses its source, falling back to the *first* entity on no
match — with a data-driven, recursive filter argument. Module authors declare
filters as plain objects; the runtime interprets them. No source-code parsing,
no builder chaining, no first-entity fallback.

Current pain points being replaced:

- `hostApi.entity.filter.create()` is a stub (js_host_api/script_rest.rs:1-3);
  only `byId` exists and its `{fn}` result is never consumed.
- All three `getEntityBy` implementations (js_executor/pending_ctx_p1.rs:7,
  scheduled_ctx_p1.rs:7, sim_tpl_p2.rs:6) resolve filters via
  `filter.toString()` + regex for `isContainingExactly(hostApi.string.of("X"))`.
  Tests only pass because scenes have one entity or the wanted entity is
  created first.
- The spec (specification/queries/entityFilter.md) composable API
  (`hasTextValue`, `byClassification`, `and`, `or`, `not`, ...) is entirely
  unimplemented at runtime.

## FilterArg

A filter is a plain object with **exactly one** of four keys. Each is a closed
variant:

```ts
type FilterLambda = (e: EntityExpression) => ConditionExpression;

type FilterPredicates = { filters: FilterLambda[] };  // leaf: every lambda must hold
type FilterAnd = { and: FilterArg[] };                // every child must match
type FilterOr = { or: FilterArg[] };                  // at least one child matches
type FilterNot = { not: FilterArg[] };                // no child may match

type FilterArg = FilterPredicates | FilterAnd | FilterOr | FilterNot;
```

### Semantics

- `filters`: entity matches iff every lambda, invoked with the entity
  expression, yields a truthy condition.
- `and`: entity matches iff every child FilterArg matches.
- `or`: entity matches iff at least one child FilterArg matches.
- `not`: entity matches iff no child FilterArg matches.
- **Empty arrays match everything** (uniform vacuous truth):
  - `filters: []` → no predicates to fail → all
  - `and: []` → nothing to satisfy → all
  - `or: []` → all (documented deliberately; match-none is expressed with `not`)
  - `not: []` → nothing excluded → all
- Validation: exactly one of the four keys present, value is an array.
  Anything else is malformed → deterministic log + **no matches** (never a
  partial result, never a fallback entity).

### Lambda contract

Every predicate, at every depth, has the same shape:

```js
(e: EntityExpression) => ConditionExpression
```

`e` is the lazy entity proxy the runtime already hands out: `id`,
`getText(key)`, `getNumber(key)`, `containers`, `getEntitiesInsideArea(name)`.
The returned condition is read by one shared helper:

```js
function condToBool(c) {
  if (typeof c === 'boolean') return c;
  if (c && typeof c.value === 'boolean') return c.value;
  var out = null;
  if (c && typeof c.ifTrue === 'function') c.ifTrue(function() { out = true; });
  if (c && typeof c.ifFalse === 'function') c.ifFalse(function() { out = false; });
  return out === true;
}
```

### Examples

```js
const { string, number } = hostApi.runtime;

// leaf
const blacksmiths = {
  filters: [
    (e) => e.getText(string.of("job")).equals(string.of("blacksmith")),
  ],
};

// and
const activeSmiths = {
  and: [
    blacksmiths,
    { filters: [(e) => e.getText(string.of("state")).equals(string.of("active"))] },
  ],
};

// or, nested
const triage = {
  or: [
    { filters: [(e) => e.getText(string.of("role")).equals(string.of("healer"))] },
    {
      and: [
        { filters: [(e) => e.getNumber(string.of("hp")).lte(number.of(5))] },
        { filters: [(e) => e.getText(string.of("team")).equals(string.of("red"))] },
      ],
    },
  ],
};

// not
const notGhosts = {
  not: [
    { filters: [(e) => e.getText(string.of("kind")).equals(string.of("spirit"))] },
  ],
};

context.getEntityBy(triage).map(...);
```

## Evaluation

One recursive rule, shared by every context:

```js
function evalArg(arg, e) {
  if (arg.filters) return arg.filters.every(f => condToBool(f(e)));
  if (arg.and)     return arg.and.every(c => evalArg(c, e));
  if (arg.or)      return arg.or.length === 0
                          || arg.or.some(c => evalArg(c, e));
  if (arg.not)     return arg.not.every(c => !evalArg(c, e));
  return false; // malformed
}
```

The evaluator walks `globalThis.__entityData` (or a caller-supplied source
list, preserving order) in deterministic insertion order, builds the entity
proxy per candidate, and collects matches. Result is a ListExpression-shaped
object supporting what consumers already use: `.map(cb)`, `.get(index)`,
`.forEach(cb)`, `.size()`, `.randomElement()`.

No match → **empty list** plus a logged marker (e.g.
`___filter-no-match:<label>`). The first-entity fallback is removed.

## Legacy input normalization (back-compat)

`getEntityBy(filter)` accepts, in this order:

| Input | Normalized to |
|---|---|
| plain string `"room"` | `{ filters: [(e) => e.id equals "room"] }` |
| bare function `fn` | `{ filters: [fn-as-id-predicate] }` |
| `{ id: fn }` (legacy object, no other keys) | `{ filters: [fn-as-id-predicate] }` |
| `{ filters/and/or/not: ... }` | evaluated directly |
| anything else | malformed → log, empty result |

`fn-as-id-predicate` invokes the legacy callback with an id
StringExpression and reads the condition via `condToBool`. Existing test
modules (HotReload, EmitChildEvents, FindEntitiesInside, EntityTextValueUpdate,
Area/NoPresence) keep working unchanged.

## Implementation plan

### Step 1 — shared filter evaluator (new file)

`application/runtime/src/js_host_api/script_entity_filter.rs` exporting one
script that installs on `globalThis`:

- `globalThis.__evalEntityFilter(filter, srcIds?)` — normalize (table above) +
  walk `__entityData` (or `srcIds`) + `evalArg` per candidate → returns the
  ListExpression-shaped result built from the shared entity-accessor builder.
- `globalThis.__entityProxy(id)` — the entity accessor builder, factored out
  of the duplicated `ee`/wrapper objects in pending_ctx_p1.rs and
  scheduled_ctx_p1.rs (getNumber map/sum/divide/modulo, getText ifPresent
  get/set, getEntitiesInsideArea).
- `globalThis.__condToBool(c)` — the condition reader.
- Named rules: `globalThis.__entityFilterRules = {}`;
  `entity.filter.asRule(name, arg)` / `entity.filter.getRule(name)` operating
  on plain FilterArg objects.

Registered in `host_api_script_rest()` (mod.rs:47) so it is installed in
every host context.

### Step 2 — real filter API on the host object

Replace the stub `entity:{filter:{create:...}}` (script_rest.rs:1-3) with:

```js
entity: { filter: {
  create: function() { return {}; },   // identity; authors write plain objects
  asRule: function(name, arg) { __entityFilterRules[name] = arg; return hostApi.entity.filter; },
  getRule: function(name) { return __entityFilterRules[name]; },
  type: {}
}}
```

### Step 3 — delegate the three context templates

- pending_ctx_p1.rs: `getEntityBy: function(filter) { return __evalEntityFilter(filter); }`
  (keep the existing `.map`/`.get` accessor surface by having the evaluator
  return the same shapes; delete the regex block, lines 8-23).
- scheduled_ctx_p1.rs: same.
- sim_tpl_p2.rs: `getEntityBy` in `buildEventContext` delegates likewise;
  `.randomElement().ifPresent(cb)` shape preserved.

### Step 4 — types

Rewrite `application/suite/types/EntityFilter.d.ts`:

```ts
export type FilterLambda = (e: EntityExpression) => ConditionExpression;

export type FilterPredicates = { filters: FilterLambda[] };
export type FilterAnd = { and: FilterArg[] };
export type FilterOr = { or: FilterArg[] };
export type FilterNot = { not: FilterArg[] };

export type FilterArg = FilterPredicates | FilterAnd | FilterOr | FilterNot;

export type EntityFilterApi = {
  create: () => FilterArg;
  asRule: (ruleName: string, arg: FilterArg) => EntityFilterApi;
  getRule: (ruleName: string) => FilterArg;
  type: EntityFilterType;
};
```

`Effect.d.ts` `getEntityBy` signature becomes
`(entityFilter: FilterArg | string | FilterLambda | { id: FilterLambda }) =>
ListExpression<Entity>`.
Update `specification/queries/entityFilter.md` and
`specification/types/filters/entityFilter.ts` to match (the old
method-chaining surface is retired).

### Step 5 — tests

Cucumber scenes under `Tests/GameScene/Stage_N/EntityFilter/`:

1. `byIdMatch` — string id and legacy `{id: fn}` select the right entity
   among several (multi-entity scene; asserts the fallback is gone).
2. `filtersLeaf` — `filters` with text and number predicates; multi-value
   existential semantics.
3. `andOrNot` — composition incl. nesting (`and` inside `or` inside `not`).
4. `noMatch` — filter matching nothing yields empty list +
   `___filter-no-match` log; `.get(0)` chain degrades to `orElse`.
5. `emptyArrays` — `filters: []`, `and: []`, `or: []`, `not: []` each match
   everything.
6. `malformed` — object with two keys / non-array value → log + empty.
7. `namedRules` — `asRule`/`getRule` reuse across two effects.

Fix any existing scene that relied on first-entity fallback (audit:
FindEntitiesInside, NoPresence).

## Open notes

- `or: []` = match-all is a deliberate, documented choice (vacuous-truth
  mandate). Match-none = `not` of a tautology, or just omit the filter.
- Structured sugar (`hasTextValue`, `byClassification`, ...) is dropped.
  Re-add later only if a pattern repeats in real modules.
- `hasContainer`/container membership is out of scope for this change;
  `e.containers` inside a lambda covers ad-hoc cases.
- Determinism: evaluation order = `__entityData` insertion order; randomness
  (if any is added) derives from ExecutionContext per spec.
