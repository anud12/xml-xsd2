# Entities — Concepts

This document describes the core `Entity` concept and provides an `EntityExpression` builder used by hosts to declaratively construct entities using expression primitives.

## Summary
An Entity is a discrete object in the world with identity, attributes, optional container relationships and lifecycle events. The `EntityExpression` is a builder-style immutable expression used to describe how an Entity should be constructed at runtime. Where primitive values are required, use the corresponding wrapper expressions (`StringExpression`, `NumberExpression`, `ConditionExpression`) so they participate in lazy evaluation and deterministic semantics.

## EntityExpression

`EntityExpression` is a declarative, immutable builder describing an Entity's structure. It intentionally accepts expression wrappers for primitive fields so a single declarative description can be reused, composed, and evaluated deterministically by the runtime.

### Host API (TypeScript)

```ts
export type EntityExpression = {
  /** Builder-style methods. The builder accepts generic keyed-setters where primitive values must be expression wrappers. */
  setField: (name: string, value: StringExpression | NumberExpression | ConditionExpression) => EntityExpression;
  setContainer: (name: string, containerExpr: ConditionExpression /* or ContainerExpression when defined */) => EntityExpression;

  /** Convenience: set multiple fields in one call */
  setFields: (map: Record<string, StringExpression | NumberExpression | ConditionExpression>) => EntityExpression;

  /** Finalize/build: evaluated by the runtime to produce a concrete Entity instance */
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

### Notes
- Always use wrapper expressions for primitive fields. Example: `withText('displayName', hostApi.string.of('Alice'))` or `withNumber('level', hostApi.number.of(5))`.
- `EntityExpression` is side-effect free. `build()` is invoked by the runtime to materialize a concrete Entity instance (for example during rule-driven creation). Keep callbacks or other side-effectful constructs out of the description unless explicitly intended.
- `asRule` / `getRule` mirror other expression APIs: register reusable entity blueprints with `asRule`, retrieve a rule-backed `EntityExpression` with `getRule`.

## Examples

```ts
// Build an entity expression and register as a rule
const entityExpr = hostApi.entity.create()
  .setField('id', hostApi.string.of('player-1'))
  .setField('entityRuleRef', hostApi.string.of('character'))
  .setField('displayName', hostApi.string.of('Alice'))
  .setField('level', hostApi.number.of(1));

hostApi.entity.asRule('playerTemplate', entityExpr);

// Retrieve and use
const playerTemplate = hostApi.entity.getRule('playerTemplate');
// runtime evaluates playerTemplate.build() when materializing the entity
```

## Integration
- Ensure `EntityExpression.build()` follows the same evaluation and repository resolution patterns as other expressions (lazy evaluation, deterministic `randomFrom`, and `ref` resolution via rule repositories).
- When container or nested entity expressions are needed, prefer composing via expressions rather than raw objects.

