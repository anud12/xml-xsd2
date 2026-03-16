# Entities — Concepts

This document describes the core `Entity` concept.

## Summary
An Entity is a discrete object in the world with identity, attributes, optional container relationships and lifecycle events.


## Identity
- id (unique within a world_step)

## Structure

- Fields:
  - `id`: type `string` unique id within global `entity` records.
  - `entity_rule_ref`: `string` reference to the entity rule
  - `text_map`: colection of `string` values accesible by `name`.
    - `name`: attribute key
    - `value`: attribute `string` value
  - `number_map` — colection of `long` values accesible by `name`
    - `name`: attribute key
    - `value`: attribute `long` value
  - `containers` — contains `container[]` elements representing container membership

- Reference entity (container-only)
  - Use-case: Minimal wrapper to express container membership by reference without re-defining attributes.
  - Pattern: The inner `<entity>` uses `entity_id_ref` to reference an entity defined elsewhere in the same `world_step`.

- Notes
  - When `entity_id_ref` is present the parser treats the element as a reference and does not re-define attributes.
  - `container` elements include `container_rule_ref` and `id`. Container ids are unique within the `world_step` and can be referenced by other entities.

## EntityExpression

This workspace provides an EntityExpression builder for host code to construct entity instances using the expression primitives (StringExpression, NumberExpression, ConditionExpression) where appropriate.

EntityExpression is an immutable builder-style expression that describes how to construct an Entity at evaluation time. Primitive/leaf values should use the corresponding wrapper expressions rather than raw JS values so they participate in lazy evaluation and deterministic semantics.

Host API (TypeScript)

```ts
export type EntityExpression = {
  /** Builder-style setters that accept expression wrappers for primitives. Each returns a new EntityExpression. */
  withId: (idExpr: StringExpression) => EntityExpression;
  withText: (name: string, value: StringExpression) => EntityExpression;
  withNumber: (name: string, value: NumberExpression) => EntityExpression;
  withContainer: (containerExpr: ConditionExpression /* or a ContainerExpression when defined */) => EntityExpression;

  /** Convenience: set multiple text/number fields in one call */
  withTexts: (map: Record<string, StringExpression>) => EntityExpression;
  withNumbers: (map: Record<string, NumberExpression>) => EntityExpression;

  /** Finalize/build: returns an evaluated Entity instance in the runtime context (evaluation is runtime responsibility). */
  build: () => any; // runtime-defined entity instance type
};

export type EntityExpressionApi = {
  /** Create an empty builder */
  create: () => EntityExpression;
  /** register as rule/get rule (mirrors other primitive APIs) */
  asRule: (ruleName: string, expr: EntityExpression) => EntityExpressionApi;
  getRule: (ruleName: string) => EntityExpression;
  type: EntityExpressionType;
};
```

Notes:
- Use `hostApi.string.of("Alice")` or `hostApi.string.of("Alice")` for id/text primitives and `hostApi.number.of(42)` for numeric fields so all parts participate in lazy evaluation and deterministic randomness.
- The `build()` operation is evaluated by the runtime when an Entity must be materialized (for example when creating entities during rule processing). The EntityExpression itself is a declarative description and should remain side-effect free.

## HostApi
