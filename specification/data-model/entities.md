# Entities — Concepts

This document describes the core `Entity` concept.

## Summary
An Entity is a discrete object in the world with identity, attributes, optional container relationships and lifecycle events.


## Identity
- id (unique within a world_step)

## Structure

```typescript
type Entity = {
  id: UniqueGlobalEntityId, // unique id within global `entity` records.
  textMap: TextMap, 
  numberMap: NumberMap, 
  containers: ContainerList[]
}
type TextMap = {
  [name:string]: StringExpression, //colection of `StringExpression` values accesible by `name`.
}
type NumberMap = {
  [name:string]: NumberExpression, //colection of `NumberExpression` values accesible by `name`.
}
type ContainerList = {
  containerIdReference: UniqueGlobalContainerId,
}
```

---

## EntityExpression — Concepts

This document specifies the `EntityExpression` builder: an immutable, lazily-evaluated expression model focused on constructing `Entity` instances with three primary concerns: `text_map`, `number_map` and `containers`.

## Summary

- `EntityExpression` is a small, fluent builder whose nodes are immutable and evaluated by the runtime.
- The builder surface intentionally exposes only `create`, `withTextMap`, `withNumberMap` and `withContainer` to keep host usage focused and composable.
- Use `StringExpression` for text_map values and `NumberExpression` for number_map values; these wrappers are provided by the string/number expression surfaces.
- See [TextMap](./textMap.md) and [NumberMap](./numberMap.md) for their specific definitions.

## Purpose

Provide a host-friendly API to declare entity instances and templates with precise semantics for keyed maps and container membership. The design mirrors the lean expression APIs used elsewhere in the spec (boolean/number/string) and keeps the entity surface minimal.

## Evaluation semantics

- `withTextMap(textMap)` and `withNumberMap(numberMap)` replace the entity's corresponding maps with the evaluated result of the supplied map expression.
- `withContainer(container)` appends a container membership; multiple calls append in declaration order.


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

### Examples

```ts
// Construct maps
const nameMap = hostApi.textMap.create()
  .put("name", /* intent: StringExpression literal "Gruk" */)
  .put("title", /* intent: StringExpression literal "Scourge" */);

const stats = hostApi.numberMap.create()
  .put("hp", /* intent: NumberExpression literal 12 */);

// Build entity using the maps and a container reference
const goblin = hostApi.entity.create()
  .withTextMap(nameMap)
  .withNumberMap(stats)
  .withContainer({ containerIdRef: "cave_entrance" });

// Queries
const hasName = nameMap.has("name");
const nameIsGruk = nameMap.equals("name", /* intent: StringExpression literal "Gruk" */);
```