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
  getText: (key: StringExpression) => MaybeExpression<StringExpression>,
  getNumber: (key: StringExpression) => MaybeExpression<NumberExpression>,
  getTextKeys: () => ListExpression<string>,
  getNumberKeys: () => ListExpression<string>,
  containers: ListExpression<ContainerExpression>
}
type ContainerList = {
  containerIdReference: UniqueGlobalContainerId,
}
```

---

## Compilation & Execution Model

Entity accessors return `Maybe<ExpressionBuilder>` that compile to imperative code at module load time. The compilation model enables efficient runtime execution while maintaining functional semantics during declaration.

**Key behavior:**

- **Keys are `StringExpression` values:** Keys support both static string literals and dynamic key access via expression composition.
- **Accessor semantics:** `getText(key)` and `getNumber(key)` return `None` if the key doesn't exist in the respective namespace.
- **Separate namespaces:** Text and number keys occupy separate namespaces — the same name can exist in both without conflict.
- **Mutation ordering:** Multiple mutations on the same key within an effect execute in **lexical order** (code appearance order).
- **Cross-entity mutations are safe:** The double-buffer model ensures safe cross-entity reads and writes: all reads come from the read buffer, all writes go to the write buffer.
- **Compile-time validation:** Module load failures occur if compilation detects errors such as expression cycles or invalid expressions.

---

## EntityExpression — Concepts

This document specifies the `EntityExpression` builder: an immutable, lazily-evaluated expression model focused on constructing `Entity` instances with three primary concerns: `text_map`, `number_map` and `containers`.

**Note:** The `EntityExpression` builder is used during effect **declaration** to create entities. The runtime `Entity` type (returned by queries) uses accessor methods instead of direct map access.

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

// Queries on TextMapExpression / NumberMapExpression during declaration
const hasName = nameMap.has("name");
const nameIsGruk = nameMap.equals("name", /* intent: StringExpression literal "Gruk" */);

// Querying and mutating entities (after retrieval from repository)

const queriedGoblin = repository.getEntity(hostApi.string.of("goblin_id"));

// Check if a text key exists:
const hasName = queriedGoblin.getTextKeys()
  .contains(hostApi.string.of("name"));

// Read and mutate a text attribute:
queriedGoblin.getText(hostApi.string.of("name"))
  .map(name => name.concat(hostApi.string.of(" the Terrible")));

// Read and mutate a number attribute:
queriedGoblin.getNumber(hostApi.string.of("hp"))
  .map(hp => hp.add(hostApi.number.of(5)));

// Cross-entity mutation (reads are safe via double-buffer):
const target = repository.getEntity(hostApi.string.of("target_id"));
queriedGoblin.getNumber(hostApi.string.of("attack"))
  .map(atk => {
    target.getNumber(hostApi.string.of("hp"))
      .map(hp => hp.subtract(atk));
  });

// Dynamic key access:
const keyExpr = hostApi.string.concat(prefix, suffix);
queriedGoblin.getNumber(keyExpr)
  .map(value => value.multiply(hostApi.number.of(2)));
```