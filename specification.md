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

- [`ConditionExpression`](./conditionExpression.md): An `immutable`, `lazily-evaluated` expression tree representing boolean values. Built with `of()`, logical combinators `and`, `or`, `not`, `xor`, `implies`, `group`, `ref`, `oneOf`, and `random(probability)`. Operators use short-circuit semantics where applicable. `ref` resolution uses the repository pattern and defaults to fail‑soft (`false`) unless strict validation is enabled.
## Server
Server loads modules which are packaged in a zip archive.

## [Modules](./modules.md)
Modules are ZIP archives containing sandboxed JavaScript (ESM) and related 
assets. 

## [Entities](./entities.md)
Atomic unit of the engine.
## Zones

## Regions

## Classifications
Used for better querry and grouping.
## Containers


## Module
Module is a collection of rules packaged into a .zip file.
