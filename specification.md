XML-XSD RPG Game Server — Implementation Specification
===============================================

Purpose
-------
This document describes the implemented functionality in the codebase under implementation/ and the behavioral test-suite in specification-test/. It is a high-level specification intended to let a developer rewrite or re-implement the server while preserving behavior.

Concepts
-------------------

## Primitives
- [`NumberExpression`](./numberExpression.md): An `immutable`, `lazily-evaluated` expression tree representing a `long` value. Construct with `of(number)` (eager — truncates/validates a JS Number) and combinators `sum`, `subtract`, `multiply`, `divide`, `group`, `random`. Provide an evaluation API such as `evaluate(context): long`. From JS a `NumberExpression` is truthy; implicit numeric coercion MUST throw.

- [`StringExpression`](./stringExpression.md): An `immutable`, `lazily-evaluated` expression tree representing host `String` values. Construct with `of(string)` (eager) and compositors `concat`, `join`, `group`, `ref`, and `oneOf`. Evaluation is lazy; `oneOf` uses the deterministic instance random table and `ref` resolution follows the repository (`asRule`/`getRule`). Includes set-aware matching primitives (`containsExpression`, `indexOfExpression`) that reason about possible expansions caused by `oneOf` and refs.

- [`ConditionExpression`](./conditionExpression.md): An `immutable`, `lazily-evaluated` expression tree representing boolean values. Construct with `of(boolean)` and compose with `and`, `or`, `negate`, `ifTrue(cb)`, `ifFalse(cb)`. Provides `asRule`/`getRule` for repository-backed named conditions. Evaluation is lazy and uses short-circuiting; callbacks passed to `ifTrue`/`ifFalse` are invoked only when the receiver's truth value dictates.


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
## Zones

## Regions

## Classifications
Used for better querry and grouping.
## Containers


## Module
Module is a collection of rules packaged into a .zip file.
