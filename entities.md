# Entities — Concepts

This document describes the core "Entity" concept.

## Summary
An Entity is a discrete object in the world with identity, attributes, optional container relationships, a location (zone/region/location graph node), classifications, and lifecycle events.

## Identity
- id (unique within a world_step)

## Structure

## text_map
- nodeName: `text_map`
- Purpose: Holds string attributes as explicit key/value pairs.
- Children: `text[]` (zero or more)
  - `text` — Attributes:
    - `name` (string) — attribute key
    - `value` (string) — attribute value
- Notes: `text_map` values parse as `string`. Used by validators, [Effects](./effects.md) and [Actions](./actions.md) to read string attributes.

## number_map
- nodeName: `number_map`
- Purpose: Holds numeric attributes as explicit key/value pairs.
- Children: `number[]` (zero or more)
  - `number` — Attributes:
    - `name` (string) — attribute key
    - `value` (number) — attribute value
- Notes: `number_map` values parse as `long`. Used by validators, [Effects](./effects.md) and [Actions](./actions.md) to read numeric attributes.

## entity
- nodeName: `entity`
- Attributes:
  - `entity_rule_ref` (string) — reference to the entity rule
  - `id` (string) — unique id within the `world_step`
- Children (optional):
  - `text_map` — contains `text[]` elements for string attributes
  - `number_map` — contains `number[]` elements for numeric attributes
  - `containers` — contains `container[]` elements representing container membership
- Behavior: Maps make attributes explicit and typed, simplifying validation and indexing. Attribute validators check values according to the map type (`number_map` → `long`, `text_map` → `string`).

Example:

```xml
<entity entity_rule_ref="entity_rule" id="0.0">
  <text_map>
    <text name="displayName" value="Alice"/>
  </text_map>
  <number_map>
    <number name="hp" value="100"/>
    <number name="level" value="5"/>
  </number_map>
  <containers>
    <container container_rule_ref="container_rule" id="0.2">
      <entities>
        <entity entity_id_ref="0.1"/>
      </entities>
    </container>
  </containers>
</entity>
```

## Reference entity (container-only)
- Use-case: Minimal wrapper to express container membership by reference without re-defining attributes.
- Pattern: The inner `<entity>` uses `entity_id_ref` to reference an entity defined elsewhere in the same `world_step`.

Example:

```xml
<entity entity_rule_ref="entity_rule" id="0.0">
  <containers>
    <container container_rule_ref="container_rule" id="0.2">
      <entities>
        <entity entity_id_ref="0.1"/>
      </entities>
    </container>
  </containers>
</entity>
```

## Notes
- When `entity_id_ref` is present the parser treats the element as a reference and does not re-define attributes.
- `container` elements include `container_rule_ref` and `id`. Container ids are unique within the `world_step` and can be referenced by other entities.
## Classification & Rules
- Classification tags for grouping & indexing

## Lifecycle & Actions
- Creation (EntityCreate middleware)
- Movement/Teleport (ZoneTeleportEntity middleware)
- Container operations (ContainerCreate, append)
- Destruction / removal


Container-only / reference use-case

Entities may also be serialized as a minimal wrapper containing only container membership. In these cases the inner <entity> element uses `entity_id_ref` to reference an existing entity defined elsewhere in the same `world_step`. This pattern avoids duplicating full entity definitions when expressing container membership by reference.

Example:

```xml
<entity entity_rule_ref="entity_rule" id="0.0">
  <containers>
    <container container_rule_ref="container_rule" id="0.2">
      <entities>
        <entity entity_id_ref="0.1"/>
      </entities>
    </container>
  </containers>
</entity>
```

Notes:
- When `entity_id_ref` is present the parser treats the element as a reference and does not re-define attributes.
- Container elements include `container_rule_ref` and `id`. Container ids are unique within the `world_step` and can be referenced by other entities.

## Indexing & Repositories
- Entities are indexed in EntityRepository for lookups and tests
- Use name rules and classification for search and resolution


