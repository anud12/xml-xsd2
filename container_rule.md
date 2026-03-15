# Container Rules — Concepts

This document describes the core `Container Rule` concept.

## Summary
An `Container Rule` is a `factory` concept used to create [`Containers`](/./container.md) within described requirements.


## Structure
- Fields:
  - `id`: type `string` unique id within global `Container Rules` records.
  - `entities`: list of `Entity rules` and a [`Number Expression`](./numberExpression.md) to denote how many must be created.
  

## HostApi

```typescript
hostApi.createContainerRule()
    .id("containerRule")
    .addEntities(hostApi.getEntityRule("entityRule"), hostApi.number.of(2))
```