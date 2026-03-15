XML-XSD RPG Game Server — Implementation Specification
===============================================

Purpose
-------
This document describes the implemented functionality in the codebase under implementation/ and the behavioral test-suite in specification-test/. It is a high-level specification intended to let a developer rewrite or re-implement the server while preserving behavior.

Concepts
-------------------

## Primitives
- [`NumberExpression`](./numberExpression.md): An `immutable`, `lazily-evaluated` expression tree representing a `long` value. Construction via `of()` (eager: converts & caches a `long` from a JS `Number`), and combinators `sum`, `subtract`, `multiply`, `divide`, `group`, `random`. Expose an explicit evaluation API such as `evaluate(context): long`. From JS a `NumberExpression` is truthy; implicit numeric coercion MUST throw (use explicit conversion helpers).
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