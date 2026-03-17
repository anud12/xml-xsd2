# Entities — Concepts

This document describes the core `Entity` concept.

## Summary
An Entity is a discrete object in the world with identity, attributes, optional container relationships and lifecycle events.


## Identity
- id (unique within a world_step)

## Structure

- Fields:
  - `id`: type `string` unique id within global `entity` records.
  - `entity_rule_ref`: `string` reference to the entity rule
  - `text_map`: colection of `string` values accesible by `name`.
    - `name`: attribute key
    - `value`: attribute `string` value
  - `number_map` — colection of `long` values accesible by `name`
    - `name`: attribute key
    - `value`: attribute `long` value
  - `containers` — contains `container[]` elements representing container membership

- Reference entity (container-only)
  - Use-case: Minimal wrapper to express container membership by reference without re-defining attributes.
  - Pattern: The inner `<entity>` uses `entity_id_ref` to reference an entity defined elsewhere in the same `world_step`.

- Notes
  - When `entity_id_ref` is present the parser treats the element as a reference and does not re-define attributes.
  - `container` elements include `container_rule_ref` and `id`. Container ids are unique within the `world_step` and can be referenced by other entities.

## HostApi

## EntityExpression — Concepts

This document specifies the `EntityExpression` builder: an immutable, lazily-evaluated expression model focused on constructing `Entity` instances with three primary concerns: `text_map`, `number_map` and `containers`.

## Summary

- `EntityExpression` is a small, fluent builder whose nodes are immutable and evaluated by the runtime.
- The builder surface intentionally exposes only `create`, `withTextMap`, `withNumberMap` and `withContainer` to keep host usage focused and composable.
- Use `StringExpression` for text_map values and `NumberExpression` for number_map values; these wrappers are provided by the string/number expression surfaces.

## Purpose

Provide a host-friendly API to declare entity instances and templates with precise semantics for keyed maps and container membership. The design mirrors the lean expression APIs used elsewhere in the spec (boolean/number/string) and keeps the entity surface minimal.

## Evaluation semantics

- `withTextMap(textMap)` and `withNumberMap(numberMap)` replace the entity's corresponding maps with the evaluated result of the supplied map expression.
- `withContainer(container)` appends a container membership; multiple calls append in declaration order.
- `TextMapExpression.put(key, value)` replaces any existing value at `key` with the provided `StringExpression`.
- `NumberMapExpression.put(key, value)` behaves analogously for numbers.
- Existence/equality checks (`has`, `equals`) return `ConditionExpression` values and are evaluated lazily.

## Host API (TypeScript)

```ts
export type HostApi = {
  /* ... rest of declarations ... */
  entity: EntityExpressionApi,
  textMap: TextMapExpressionApi,
  numberMap: NumberMapExpressionApi,
}

export type EntityExpressionApi = {
  /** Create an empty entity builder */
  create: () => EntityExpression,

  /** Optional rule registration helpers (follow repository pattern) */
  asRule?: (ruleName: string, expr: EntityExpression) => EntityExpressionApi,
  getRule?: (ruleName: string) => EntityExpression,

  type: EntityExpressionType,
}

export type EntityExpressionType = {
  // marker for dynamic HostApi typing
}

export type EntityExpression = {
  /** Replace the entity's text_map with the supplied TextMapExpression */
  withTextMap: (textMap: TextMapExpression) => EntityExpression,
  /** Replace the entity's number_map with the supplied NumberMapExpression */
  withNumberMap: (numberMap: NumberMapExpression) => EntityExpression,
  /** Append a container membership (ContainerExpression or ContainerReference) */
  withContainer: (container: ContainerExpression | ContainerReference) => EntityExpression,
}
```

### TextMap / NumberMap expressions

```ts
export type TextMapExpressionApi = {
  create: () => TextMapExpression,
}

export type TextMapExpression = {
  /** Insert or replace a key's value with a StringExpression */
  put: (key: string, value: StringExpression) => TextMapExpression,
  /** Remove a key (optional) */
  remove?: (key: string) => TextMapExpression,
  /** Retrieve the value expression for a key (missing keys may produce an empty StringExpression) */
  get: (key: string) => StringExpression,
  /** Existence check: returns a ConditionExpression */
  has: (key: string) => ConditionExpression,
  /** Equality check: compare stored value to provided StringExpression */
  equals: (key: string, value: StringExpression) => ConditionExpression,
}

export type NumberMapExpressionApi = {
  create: () => NumberMapExpression,
}

export type NumberMapExpression = {
  put: (key: string, value: NumberExpression) => NumberMapExpression,
  remove?: (key: string) => NumberMapExpression,
  get: (key: string) => NumberExpression,
  has: (key: string) => ConditionExpression,
  equals: (key: string, value: NumberExpression) => ConditionExpression,
}
```

### Container helpers

```ts
export type ContainerReference = {
  containerIdRef: string,
}

export type ContainerExpressionApi = {
  create: (containerRuleRef: string) => ContainerExpression,
}

export type ContainerExpression = {
  withId: (id: string) => ContainerExpression,
  withEntityRef: (entityIdRef: string) => ContainerExpression,
}
```

## Examples

```ts
// Construct maps
const nameMap = hostApi.textMap.create()
  .put("name", hostApi.string.of("Gruk"))
  .put("title", hostApi.string.of("Scourge"));

const stats = hostApi.numberMap.create()
  .put("hp", hostApi.number.of(12));

// Build entity using the maps and a container reference
const goblin = hostApi.entity.create()
  .withTextMap(nameMap)
  .withNumberMap(stats)
  .withContainer({ containerIdRef: "cave_entrance" });

// Queries
const hasName = nameMap.has("name");
const nameIsGruk = nameMap.equals("name", hostApi.string.of("Gruk"));
```

## Notes

- `put` replaces an existing keyed value for that map key.
- Always wrap primitive literals using `hostApi.string.of(...)` and `hostApi.number.of(...)`.
- Follow the repository/indexing pattern for optional `asRule`/`getRule` support.


## Todo

for "entities.md" createa a "EntityExpression" for 
  entity. 
  Only functions are "of", "withTextMap", "withNumberMap" and "withContainer".

  the `TextMap` anud `NumberMap` are also expressions to add `key`/`value` pairs, check of existence/equality.
  adding keyed value replaces the value stored at that key, it exists.


   When a primitive is needed use defined wrapper expressions. use 
  Inspire usage from "conditionExpression","numberExpression" and, "stringExpression".


  cortana to write to file 