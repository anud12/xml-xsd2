# NumberMap

## Summary
Use `NumberExpression` for number map values. The `NumberMapExpression` builder is provided by the `HostApi.numberMap` surface.

## Purpose
`NumberMap` holds a keyed collection of `NumberExpression` values attached to an Entity or Container.

## Evaluation semantics
- `NumberMapExpression.put(key, value)` replaces any existing value at `key` with the provided `NumberExpression`.
- Existence/equality checks (`has`, `equals`) return `ConditionExpression` values and are evaluated lazily.
- Missing keys may produce a zero `NumberExpression` on `get`.

## Structure
```ts
type NumberMap = {
  [name: string]: NumberExpression; // collection of NumberExpression values accessible by name
}
```

## Expression API

```ts
export type NumberMapExpressionApi = {
  create: () => NumberMapExpression;
}

export type NumberMapExpression = {
  /** Insert or replace a key's value with a NumberExpression */
  put: (key: string, value: NumberExpression) => NumberMapExpression;
  /** Remove a key */
  remove?: (key: string) => NumberMapExpression;
  /** Retrieve the value expression for a key (missing keys may produce a zero NumberExpression) */
  get: (key: string) => NumberExpression;
  /** Existence check: returns a ConditionExpression */
  has: (key: string) => ConditionExpression;
  /** Equality check: compare stored value to provided NumberExpression */
  equals: (key: string, value: NumberExpression) => ConditionExpression;
}
```

## Notes
- `put` replaces any existing value at the given key.
- Always wrap primitive literals using `hostApi.number.of(...)`.
- See [`textMap.md`](./textMap.md) for the string counterpart.
- See [`entities.md`](./entities.md) and [`containers.md`](./containers.md) for how NumberMap is attached to world objects.
