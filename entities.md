# Entities — Concepts

This document describes the core "Entity" concept.

## Summary
An Entity is a discrete object in the world with identity, attributes, optional container relationships, a location (zone/region/location graph node), classifications, and lifecycle events.

## Identity
- id (unique within a world_step)

## Structure

Attributes are serialized as either `text_map` or `number_map`. Each map contains simple key/value elements where "name" is the attribute key and "value" holds the typed value.

- text_map — string attributes: `<text name="..." value="..."/>`
- number_map — numeric attributes: `<number name="..." value="..."/>`

Maps make attributes explicit and typed, simplifying validation and indexing. Attribute validators check values according to the map type(`number_map` values parse as `longs`/ `text_map` values parse as `string`). [Effects](./effects.md) and [Actions](./actions.md) access attributes via these maps.

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


## Location & Containment
- zone / region / locationGraph node references
- parent container (inventory, container entity)
- Teleportation updates location attributes

## Classification & Rules
- Classification tags for grouping & indexing
- Name rules used to resolve human-readable names

## Lifecycle & Actions
- Creation (EntityCreate middleware)
- Movement/Teleport (ZoneTeleportEntity middleware)
- Container operations (ContainerCreate, append)
- Destruction / removal


## Scripts & Hooks
- Entities can be targeted by JS rule modules (HostJSApi)
- onServerTick and event handlers may modify attributes or trigger actions

## Serialization (XML example)
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

