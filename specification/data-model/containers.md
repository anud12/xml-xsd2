# Container — Concepts

This document describes the core `Container` concept.

## Summary

A Container is a structured holder for Entities.

It models parent-child ownership or storage relationships inside the world model. A container is usually attached to an owning Entity and contains zero or more member Entities. Conceptually, containers cover use-cases such as inventories, bags, equipment slots, chests, cargo holds, or any other bounded grouping where one Entity holds other Entities.

Containers are part of the data model, are rule-driven through `container_rule_ref`, and are addressable through their own `id`.

## Purpose

Containers exist to express that:

- one Entity can hold other Entities;
- membership can be serialized explicitly in the world data;
- storage structure can be distinguished from physical location;
- container behavior can be driven by reusable rules.
- Represent inventories, bags, equipment slots, chests, cargo holds, or similar owned groupings.

This means an Entity may both exist in a Zone/Region location graph and also participate in container relationships.

## Structure

```typescript
type Container = {
  id: UniqueGlobalContainerId,
  getText: (key: StringExpression) => MaybeExpression<StringExpression>,
  getNumber: (key: StringExpression) => MaybeExpression<NumberExpression>,
  getTextKeys: () => ListExpression<string>,
  getNumberKeys: () => ListExpression<string>,
  entities: ListExpression<EntityExpression>,
  dimensions?: ListExpression<Dimension>,
}

type Dimension = {
  name?: string, // friendly name (e.g., "row", "col", "slot")
  // mapping: a function that receives the member Entity and returns a NumberExpression
  mapping: (entity: Entity) => NumberExpression,
  // optional size and out-of-bounds handling
  size?: DimensionSize,
}

type DimensionSize = {
  value: NumberExpression, // e.g., hostApi.number.of(10)
  outOfBounds: OutOfBoundsRule,
}

type OutOfBoundsRule = "unbound" | "clamp" | "wrap"

**Note: TextMap and NumberMap are internal implementation details.** Containers use accessor methods (`getText`, `getNumber`, `getTextKeys`, `getNumberKeys`) for accessing attributes and should not be accessed directly from modules. These map types are not exposed to the Entity or Container public API.
type EntityReference = {
  entity?: { entityIdReference: UniqueGlobalEntityId }[],
}
```


### Notes & constraints

- Containers model membership, not physical placement: an Entity may be both located in a `Zone/Region` and also be contained by another Entity.
- Member entries use `entity_id_ref` to avoid duplicating full Entity payloads; when present the parser treats the element as a reference.
- Cardinality summary:
  - `Entity` → `containers`: 0..*
  - `container` → `entities`: 0..1 (wrapper)
  - `entities` → `entity`: 0..*
- See [entities.md](./entities.md) for matching reference semantics and overall serialization conventions.

## Identity

- `id` uniquely identifies the container within a `world_step`.

Container identity is separate from Entity identity. A container is not just an attribute on an Entity; it is a global element with its own referenceable identifier.

## Ownership and membership

The core relationship is:

- an owning Entity has one or more Containers;
- each Container contains zero or more member Entities.

This lets the model distinguish between:

- the owner of a container, and
- the contents of that container.

In practical terms, the owner could be a character, chest, vehicle, or structure, while the contained Entities could be items, resources, equipment, or even other game objects, depending on rule constraints.

## Relationship to location

A container relationship is different from world placement.

- Zone/Region/location describes where an Entity is in the world.
- Container membership describes what an Entity is inside of.

These concepts may coexist. For example, a player Entity may be located in a Region while also owning an inventory container that references item Entities.


## Lifecycle

The Entity document already identifies container operations as part of entity lifecycle behavior:

- creation;
- append / membership changes;
- destruction / removal.

So the intended runtime model is that containers are not static markup only; they participate in gameplay operations that add or remove contained Entities over time.


## Typical use-cases

Common conceptual uses for containers include:

- character inventory;
- equipment slots;
- loot chest contents;
- crafting input/output holders;
- vehicle cargo;
- building storage;
- temporary transfer buffers during actions.

The specification intentionally treats these as the same underlying concept: an owned collection of Entities with rule-defined semantics.


## Design intent

The important conceptual boundary is that a Container is not merely a list field. It is a modeled relationship node that:

- belongs to an Entity;
- has its own identity;
- is bound to a container rule;
- groups contained Entities explicitly.

That explicitness is what makes containers useful for validation, runtime operations, indexing, and behavior-specific rules.

## Dimensions

Containers may optionally declare one or more dimensions to support indexed or coordinate-like addressing of contained Entities. The current specification supports only 1D and 2D containers.

- Container type: determined by the number of declared dimensions. Only 1D and 2D are supported; a 1D container has a single dimension, a 2D container declares two dimensions.
- Dimension definition: each dimension is a mapping function from an Entity to a NumberExpression. At evaluation time the runtime computes the NumberExpression for a given Entity to yield the position (index or coordinate) in that dimension.
- Dimension size: each declared dimension may include an optional size (expressed as a NumberExpression). When present, the size describes the valid range for indices or bounds for coordinates and can be used for validation, clamping, or wrap behavior depending on container-rule semantics.

Examples:

- 1D (slots): a bag may declare a single dimension mapping items to integer 'slotIndex' positions. An optional size sets the number of slots.
- 2D (grid): a chest or grid may declare 'row' and 'col' dimensions mapping items to grid coordinates; sizes define the number of rows and columns.

Semantics note: NumberExpression results are numeric. Container rules must document how numeric results are interpreted (e.g., integer index vs. real coordinate), how non-integer values are handled (flooring, rounding), and what happens when values fall outside declared sizes (unbound, clamp, wrap). The runtime evaluates NumberExpressions deterministically; interpretation and enforcement are the responsibility of the container rule implementation.

---

## ContainerExpression — Concepts

This document specifies the `ContainerExpression` builder: an immutable, lazily-evaluated expression model representing container instances that hosts can construct programmatically. The surface is intentionally minimal: containers are composed by appending member entities.

## Summary

- `ContainerExpression` is a fluent builder evaluated by the runtime.
- Prefer building members as `EntityExpression` values (use hostApi.entity.create() and hostApi.textMap/numberMap helpers to construct them).
- See [TextMap](./textMap.md) and [NumberMap](./numberMap.md) for their specific definitions.

## Purpose

Provide a compact host API to create container instances and register reusable container templates. Keeping the surface minimal reduces duplication with entity-level semantics and centralizes container constraints in the container rule definitions.

## Evaluation semantics

- `addEntity(entity)`: append the evaluated `EntityExpression` as a member of the container. The runtime evaluates the supplied entity expression when materializing the container.
- Members are not ordered.
- Duplicate entities are allowed.
- `getRule(ruleName)` resolves registered container templates at runtime when present.

## Host API (TypeScript)

```ts
export type HostApi = {
  /* ... rest of declarations ... */
  container: ContainerExpressionApi
}

export type ContainerExpressionApi = {
  /** Create an empty container builder */
  create: () => ContainerExpression,
  /** Register and retrieve named container templates */
  asRule?: (ruleName: string, expr: ContainerExpression) => ContainerExpressionApi,
  getRule?: (ruleName: string) => ContainerExpression,
  type: ContainerExpressionType,
  /** Dimension expression builder factory */
  dimension?: DimensionExpressionApi,
}

export type ContainerExpressionType = {
  // marker for dynamic HostApi typing
}

export type DimensionExpressionApi = {
  create: () => DimensionExpression,
  asRule?: (ruleName: string, expr: DimensionExpression) => DimensionExpressionApi,
  getRule?: (ruleName: string) => DimensionExpression,
}

export type DimensionExpression = {
  withName: (name: string) => DimensionExpression,
  withMapping: (mapping: (entity: EntityExpression) => NumberExpression) => DimensionExpression,
  withSize: (value: NumberExpression, outOfBounds: OutOfBoundsRule) => DimensionExpression,
}

export type ContainerExpression = {
  /** Append an inline member entity built using EntityExpression */
  withEntity: (entity: EntityExpression) => ContainerExpression,
  /** Add a dimension expression to the container builder */
  withDimension: (dimension: DimensionExpression) => ContainerExpression,
  /** Replace the entity's text_map with the supplied TextMapExpression */
  withTextMap: (textMap: TextMapExpression) => EntityExpression,
  /** Replace the entity's number_map with the supplied NumberMapExpression */
  withNumberMap: (numberMap: NumberMapExpression) => EntityExpression,
}
```

### Examples

**Important:** The examples below show two different contexts:
- **DimensionExpression.withMapping()** during **CONSTRUCTION** (builder phase) receives an `EntityExpression` (builder context), which can access `entity.number_map.get()` directly.
- **Container dimensions** shown in **runtime data models** receive **runtime Entity** objects, which must use the accessor API (`entity.getNumber()`, `entity.getText()`) instead of direct map access.

```ts
// Build inline member entity using text/number map helpers
const potionEntity = /* intent: build inline EntityExpression with text 'name'='Health Potion' and number 'hp_restored'=20 */;

const inv = hostApi.container.create()
  .withDimension(hostApi.container.dimension?.create()
    .withName("slot")
    .withMapping((entity) => entity.number_map.get("slotIndex"))
    .withSize(hostApi.number.of(20), "clamp")
  )
  .withEntity(potionEntity);

/* intent: register container template 'basic_inventory' in runtime repository */
hostApi.container.asRule?.("basic_inventory", inv);

/* intent: retrieve registered container 'basic_inventory' and append an inline entity named 'Gem' */
const instantiated = hostApi.container.getRule?.("basic_inventory")
  .withEntity(/* intent: inline EntityExpression with text 'name'='Gem' */);

// Example: 1D container (slots) data model — uses runtime Entity accessor API
const bagContainer: Container = {
  id: "bag-1",
  dimensions: [
    {
      name: "slot",
      // mapping receives runtime Entity and must use accessor API: getNumber()
      mapping: (entity) => entity.getNumber(hostApi.string.of("slotIndex")).orElse(hostApi.number.of(0)),
      size: {
        value: hostApi.number.of(20),
        outOfBounds: "clamp",
      },
    },
  ],
  entities: {
    entity: [ { entityIdReference: "item-1" }, { entityIdReference: "item-2" } ],
  },
};

// Example: 2D container (grid) data model — uses runtime Entity accessor API with wrap behavior
const gridContainer: Container = {
  id: "chest-grid-1",
  dimensions: [
    {
      name: "row",
      // mapping receives runtime Entity and must use accessor API: getNumber()
      mapping: (entity) => entity.getNumber(hostApi.string.of("row")).orElse(hostApi.number.of(0)),
      size: { value: hostApi.number.of(3), outOfBounds: "wrap" },
    },
    {
      name: "col",
      // mapping receives runtime Entity and must use accessor API: getNumber()
      mapping: (entity) => entity.getNumber(hostApi.string.of("col")).orElse(hostApi.number.of(0)),
      size: { value: hostApi.number.of(5), outOfBounds: "wrap" },
    },
  ],
  entities: {
    entity: [ { entityIdReference: "gem-1" } ],
  },
};
```

