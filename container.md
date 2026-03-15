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

This means an Entity may both exist in a Zone/Region location graph and also participate in container relationships.

## Structure

### Purpose

- Express an owned collection of Entities with identity and rule-driven semantics.
- Represent inventories, bags, equipment slots, chests, cargo holds, or similar owned groupings.

### Placement

- Containers appear under an owning `Entity` inside a `<containers>` collection.
- They are nested model elements (not top-level entities) and belong to their owner in the same `world_step`.

### XML representation

- Element names and attributes (skeleton):

```xml
<containers>            <!-- 0..* under <entity> -->
  <container            <!-- 1 per container instance -->
    id="string"       <!-- unique within world_step -->
    container_rule_ref="string"  <!-- ref to container rule -->
  >
    <entities>          <!-- optional wrapper for members -->
      <entity           <!-- 0..* member entries -->
        entity_id_ref="string"   <!-- reference to an entity defined elsewhere -->
      />
    </entities>
  </container>
</containers>
```

### Content model

- `<containers>`: 0..* under an `Entity`.
- `<container>`: contains attributes `id` (required) and `container_rule_ref` (recommended/used to resolve rules).
- Optional `<entities>` child which holds 0..* `<entity>` entries.
- Each member `<entity>` is typically a reference (`entity_id_ref`) to an `Entity` elsewhere in the same `world_step`; inline full entity definitions are allowed but less common.

### Example XML snippet

```xml
<entity entity_rule_ref="character" id="player-1">
  <containers>
    <container container_rule_ref="inventory" id="player-1.inventory">
      <entities>
        <entity entity_id_ref="sword-1"/>
        <entity entity_id_ref="potion-3"/>
      </entities>
    </container>
  </containers>
</entity>
```

### Notes & constraints

- `id` is unique within a `world_step` and makes the container addressable for indexing and runtime lookups.
- `container_rule_ref` links to the reusable container rule that defines allowed contents and behavior.
- Containers model membership, not physical placement: an Entity may be both located in a `Zone/Region` and also be contained by another Entity.
- Member entries usually use `entity_id_ref` to avoid duplicating full Entity payloads; when present the parser treats the element as a reference.
- Cardinality summary:
  - `Entity` → `containers`: 0..*
  - `container` → `entities`: 0..1 (wrapper)
  - `entities` → `entity`: 0..*
- See `entities.md` for matching reference semantics and overall serialization conventions.

## Identity

- `id` uniquely identifies the container within a `world_step`.
- `container_rule_ref` links the container instance to its rule definition.

Container identity is separate from Entity identity. A container is not just an attribute on an Entity; it is a first-class nested model element with its own referenceable identifier.

## Ownership and membership

The core relationship is:

- an owning Entity has one or more Containers;
- each Container contains zero or more member Entities.

This lets the model distinguish between:

- the owner of a container, and
- the contents of that container.

In practical terms, the owner could be a character, chest, vehicle, or structure, while the contained Entities could be items, resources, equipment, or even other game objects, depending on rule constraints.

## Reference semantics

Container membership commonly uses Entity references instead of re-defining full Entity payloads inline.

Example:

```xml
<entities>
  <entity entity_id_ref="0.1"/>
</entities>
```

This means:

- the member Entity is defined elsewhere in the same `world_step`;
- the container records membership by reference;
- serialization avoids duplicating the full Entity definition.

This matches the broader Entity serialization pattern documented in `entities.md`.

## Relationship to location

A container relationship is different from world placement.

- Zone/Region/location describes where an Entity is in the world.
- Container membership describes what an Entity is inside of.

These concepts may coexist. For example, a player Entity may be located in a Region while also owning an inventory container that references item Entities.

## Relationship to rules

Containers are rule-backed via `container_rule_ref`.

At the specification level, this implies:

- container instances are created from reusable rule definitions;
- rule definitions are the place to encode container-specific constraints or defaults;
- runtime systems can resolve container behavior by first resolving the referenced rule entry.

The exact fields of a container rule are not yet documented in this workspace, but the reference pattern follows the same rule/entry approach used elsewhere in the model.

## Lifecycle

The Entity document already identifies container operations as part of entity lifecycle behavior:

- creation;
- append / membership changes;
- destruction / removal.

So the intended runtime model is that containers are not static markup only; they participate in gameplay operations that add or remove contained Entities over time.

## Indexing and lookup

Because containers have stable ids and rule references, they are suitable for repository-style indexing in the same way as other major model elements.

At minimum, implementations should support lookups by:

- container id;
- owning entity;
- referenced container rule;
- contained entity membership.

This keeps container operations efficient and makes tests easier to express.

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

## Example interpretation

Given:

```xml
<entity entity_rule_ref="character" id="player-1">
  <containers>
    <container container_rule_ref="inventory" id="player-1.inventory">
      <entities>
        <entity entity_id_ref="sword-1"/>
        <entity entity_id_ref="potion-3"/>
      </entities>
    </container>
  </containers>
</entity>
```

The interpretation is:

- `player-1` owns a container;
- that container is an `inventory`;
- the inventory currently contains `sword-1` and `potion-3`;
- the contained entities are referenced, not duplicated inline.

## Design intent

The important conceptual boundary is that a Container is not merely a list field. It is a modeled relationship node that:

- belongs to an Entity;
- has its own identity;
- is bound to a container rule;
- groups contained Entities explicitly.

That explicitness is what makes containers useful for validation, runtime operations, indexing, and behavior-specific rules.
