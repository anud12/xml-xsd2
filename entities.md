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
