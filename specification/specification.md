XML-XSD RPG Game Runtime — Implementation Specification
===============================================

Purpose
-------
This document describes the implemented functionality in the codebase under implementation/ and the behavioral test-suite in specification-test/. It is a high-level specification intended to let a developer rewrite or re-implement the runtime while preserving behavior.

Concepts
-------------------

## Primitives
- [`NumberExpression`](./numberExpression.md): An `immutable`, `lazily-evaluated` expression tree representing a `long` value.

- [`StringExpression`](./stringExpression.md): An `immutable`, `lazily-evaluated` expression tree representing host `String` values.

- [`ListExpression`](./listExpression.md): An `immutable`, `lazily-evaluated` expression tree representing ordered sequences (lists/arrays) of element expressions.

- [`MaybeExpression`](./maybeExpression.md): An `immutable`, `lazily-evaluated` expression representing optional values (present/absent).

- [`ConditionExpression`](./conditionExpression.md): An `immutable`, `lazily-evaluated` expression tree representing boolean values.

- [`TemporalExpression`](./temporalExpression.md): An `immutable`, `lazily-evaluated` expression representing a duration of in-game time, expressed in module-defined named units (e.g. `"round"`, `"day"`) that map to an internal Game Time Unit (GTU) counter advancing at a configurable rate per tick.


## [Modules](./modules.md)
Modules are ZIP archives containing sandboxed JavaScript (ESM) and related 
assets. 
Modules are loaded by executing its start point script in a javascript enviroment.
The enviroment has the following constraints:
    - is **sandboxed** so that no host changes are allowed.
    - no build in api, that means no `nodejs`/`browser`/etc api is available for the script to use.
    - the api is ran at load time to load all logic, then discarted, keeping in memory the execution plan defined in the module.

## [Entities](./entities.md)
Atomic unit of the engine.
## [Containers](./containers.md)
Holder of entities

## [Randomness](./randomness.md)
All randomness is deterministic and stateless — each draw is derived from the `ExecutionContext` (World Seed, Tick, Source, Action, Call Index) using a 64-bit SplitMix64-based PRNG. This guarantees runtime-client parity, parallel safety, and no global RNG state.

## [Actions](./actions.md)
The sole external entrypoint into the runtime. Clients send named Actions over WebSocket targeting an entity, container, or point-in-container. Modules register Actions with a guard, cooldown, and an Effect pipeline (DAG).

## Classifications
Used for better query and grouping.

## Module
Module is a collection of rules packaged into a .zip file.
