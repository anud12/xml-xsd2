
- `module`: every rule/ constant declaration are done in this module's context. When importing another, those informations can be read/modified through a "getContext" api

- modify `conditionExpression` to add ability of `isTrue(() => {})` and `isFalse(() => {})`


- `Architecture`: server is a `number cruncher` with `high memory constraints limitations` which reads data only on load, and uses `SIMD` to process all informations as flat as possible, to then relay changes to client or another service which has the role to persist data.


---
Check `EntityExpression` for correctness

for "entities.md" createa a "EntityExpression" for entity. 
Only functions are "of", "withTextMap", "withNumberMap" and "withContainer".

the `TextMap` anud `NumberMap` are also expressions to add `key`/`value` pairs, check of existence/equality.
adding keyed value replaces the value stored at that key, if it exists.


  When a primitive is needed use defined wrapper expressions. use 
Inspire usage from "conditionExpression","numberExpression" and, "stringExpression".

Use cortana to write to file 


---
Merge `zones` into `containers`

---
Repository specification for entities/containers

---
Entity mutation declaration api,

---
Split entity into
  - `structure`
  - `expression`
  - `filter`

---
Container:
- also for container add a portals from regions.
  - for portal declare position if it is on border, or inside

--- Remove zones/regions

--- change literature to replace server with runtime, where the runtime executes etc.

--- expand actions