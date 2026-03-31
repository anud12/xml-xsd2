# TextMap

## Summary
Use `StringExpression` for text map values. The `TextMapExpression` builder is provided by the `HostApi.textMap` surface.

## Purpose
`TextMap` holds a keyed collection of `StringExpression` values attached to an Entity or Container.

## Evaluation semantics
- `TextMapExpression.put(key, value)` replaces any existing value at `key` with the provided `StringExpression`.
- Existence/equality checks (`has`, `equals`) return `ConditionExpression` values and are evaluated lazily.
- Missing keys may produce an empty `StringExpression` on `get`.

## Structure
```ts
type TextMap = {
  [name: string]: StringExpression; // collection of StringExpression values accessible by name
}
```

## Expression API

```ts
export type TextMapExpressionApi = {
  create: () => TextMapExpression;
}

export type TextMapExpression = {
  /** Insert or replace a key's value with a StringExpression */
  put: (key: string, value: StringExpression) => TextMapExpression;
  /** Remove a key */
  remove?: (key: string) => TextMapExpression;
  /** Retrieve the value expression for a key (missing keys may produce an empty StringExpression) */
  get: (key: string) => StringExpression;
  /** Existence check: returns a ConditionExpression */
  has: (key: string) => ConditionExpression;
  /** Equality check: compare stored value to provided StringExpression */
  equals: (key: string, value: StringExpression) => ConditionExpression;
}
```

## Notes
- `put` replaces any existing value at the given key.
- Always wrap primitive literals using `hostApi.string.of(...)`.
- See [`numberMap.md`](./numberMap.md) for the numeric counterpart.
- See [`entities.md`](./entities.md) and [`containers.md`](./containers.md) for how TextMap is attached to world objects.
