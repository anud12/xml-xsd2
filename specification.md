XML-XSD RPG Game Server — Implementation Specification
===============================================

Purpose
-------
This document describes the implemented functionality in the codebase under implementation/ and the behavioral test-suite in specification-test/. It is a high-level specification intended to let a developer rewrite or re-implement the server while preserving behavior.

Concepts
-------------------

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