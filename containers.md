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
  id: UniqueGlobalContainerId
  textMap: TextMap,
  numberMap: NumberMap,
  entities: EntityReference,
}
type TextMap = {
  [name:string]: StringExpression, //colection of `StringExpression` values accesible by `name`.
}
type NumberMap = {
  [name:string]: NumberExpression, //colection of `NumberExpression` values accesible by `name`.
}
type EntityReference = {
  entityIdReference: UniqueGlobalEntityId, 
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

---

## ContainerExpression — Concepts

This document specifies the `ContainerExpression` builder: an immutable, lazily-evaluated expression model representing container instances that hosts can construct programmatically. The surface is intentionally minimal: containers are composed by appending member entities.

## Summary

- `ContainerExpression` is a fluent builder evaluated by the runtime.
- Prefer building members as `EntityExpression` values (use hostApi.entity.create() and hostApi.textMap/numberMap helpers to construct them).
- See [TextMap and NumberMap](./textMap&numberMap.md) for their specific definitions.

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
}

export type ContainerExpressionType = {
  // marker for dynamic HostApi typing
}

export type ContainerExpression = {
  /** Append an inline member entity built using EntityExpression */
  withEntity: (entity: EntityExpression) => ContainerExpression,
  /** Replace the entity's text_map with the supplied TextMapExpression */
  withTextMap: (textMap: TextMapExpression) => EntityExpression,
  /** Replace the entity's number_map with the supplied NumberMapExpression */
  withNumberMap: (numberMap: NumberMapExpression) => EntityExpression,
}
```

### Examples

```ts
// Build inline member entity using text/number map helpers
const potionEntity = /* intent: build inline EntityExpression with text 'name'='Health Potion' and number 'hp_restored'=20 */;

const inv = hostApi.container.create()
  .withEntity(potionEntity);

/* intent: register container template 'basic_inventory' in runtime repository */
hostApi.container.asRule?.("basic_inventory", inv);

/* intent: retrieve registered container 'basic_inventory' and append an inline entity named 'Gem' */
const instantiated = hostApi.container.getRule?.("basic_inventory")
  .withEntity(/* intent: inline EntityExpression with text 'name'='Gem' */);
```

