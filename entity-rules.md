# Entity Rules — Extracted Summary
This document describes the core `Entity Rule` concept.

## Overview
An `Entity Rule` is a `factory` concept used to create [`Entities`](./entities.md) within described requirements.

## Structure

- Fields:
  - `id`: type `string` unique id within global `Entity Rule` records.
  - `text_map`: colection of required `string` values accesible by `name`.
    - `name`: attribute key
    - `value`: attribute `string` value
  - `number_map` — colection of required `long` values accesible by `name` 
    - `name`: attribute key
    - `value`: attribute `long` value
  - `containers` — is a list of [`Container Rules`](./container_rule.md) represents needed required `container[]` elements.

```xml
<entity_rule>
    <entry name="complete_entity">
      <text_map>
        <text name="displayName" value="Alice the Adventurer"/>
        <text name="shortName" value="Al"/>      
        <text name="title" value="Veteran"/>     
        <text name="description" value="Template with multiple text attributes and container refs."/>
      </text_map>
      <containers>
        <container container_rule_ref="inventory"/>
        <container container_rule_ref="equipment"/>
        <container container_rule_ref="stash"/>  
      </containers>
    </entry>
  </entity_rule>
```

## Notes
- The summary reflects the implementation model shape (attributes and child elements) — it is intended for documentation and quick reference.
