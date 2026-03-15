XML-XSD RPG Game Server — Implementation Specification
===============================================

Purpose
-------
This document describes the implemented functionality in the codebase under implementation/ and the behavioral test-suite in specification-test/. It is a high-level specification intended to let a developer rewrite or re-implement the server while preserving behavior.

Concepts
-------------------

## Primitives\n- [NumberExpression](./numberExpression.md): An immutable, lazily-evaluated expression tree representing a long value. Construction via of() (eager: converts & caches a long from a JS Number), and combinators sum, subtract, multiply, divide, group, andom. Expose an explicit evaluation API such as valuate(context): long. From JS a NumberExpression is truthy; implicit numeric coercion MUST throw (use explicit conversion helpers).\n- [StringExpression](./stringExpression.md): An immutable, lazily-evaluated expression tree representing host String values. Construction via of() (eager: converts & caches a JS string), and compositors concat, join, group, ef, and oneOf. Evaluation is lazy; oneOf uses the deterministic instance random table and ef resolution follows the repository (sRule/getRule). Includes set-aware matching primitives (containsExpression, indexOfExpression) which reason about possible expansions caused by oneOf and refs.\n- [ConditionExpression](./booleanExpression.md): An immutable, lazily-evaluated expression tree representing boolean values. Built via of(), logical combinators nd, or, 
ot, xor, implies, group, ef, oneOf and andom(probability). Operators use short-circuit semantics where applicable. ef resolution uses the repository pattern and defaults to fail-soft (false) unless strict validation is enabled.\n\n## Server
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
