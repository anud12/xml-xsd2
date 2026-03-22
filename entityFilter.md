# Entity Filter — Concepts

## Summary

`EntityFilter` is an immutable, lazily-evaluated expression that extracts a subset of entities from either a caller-supplied input list (ListExpression) or the global entity repository. It understands the engine's entity shape (`id`, `text_map`, `number_map`, `containers`, classifications) and is implemented by composing existing expression primitives (`ConditionExpression`, `StringExpression`, `NumberExpression`, `ListExpression`, and TextMap/NumberMap helpers).

## Purpose

EntityFilter is a declarative, composable, and side-effect-free mechanism to locate entities for rules, effects, UI queries, and runtime logic. Filters must be pure (no writes), evaluated against the reading-buffer snapshot, and deterministic given the same ExecutionContext (including deterministic randomness).

## HostApi

```ts
type EntityFilter = {
  byId: (fn: (id: StringExpression) => ConditionExpression) => EntityFilter
  hasTextValue: (stringExpression:StringExpression, fn: (value: StringExpression) => ConditionExpression) => EntityFilter
  hasNumberValue: (stringExpression:StringExpression, fn: (number: NumberExpression) => ConditionExpression) => EntityFilter,
  hasContainer:(containerFilter:ContainerFilter) => EntityFilter
  not: (entityFilter:EntityFilter) => EntityFilter,
}
```