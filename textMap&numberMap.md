### Summary
- Use `StringExpression` for text_map values and `NumberExpression` for number_map values; these wrappers are provided by the string/number expression surfaces.

## Purpose
`TextMap` exists to hold `StringExpression` values.

`NumberMap` exists to hold `NumberExpression` values.

## Evaluation semantics
- `TextMapExpression.put(key, value)` replaces any existing value at `key` with the provided `StringExpression`.
- `NumberMapExpression.put(key, value)` behaves analogously for numbers.
- Existence/equality checks (`has`, `equals`) return `ConditionExpression` values and are evaluated lazily.

## Structure
```ts
type TextMap = {
  [name:string]: StringExpression, //colection of `StringExpression` values accesible by `name`.
}
type NumberMap = {
  [name:string]: NumberExpression, //colection of `NumberExpression` values accesible by `name`.
}
```

### TextMap / NumberMap expressions

```ts
export type TextMapExpressionApi = {
  create: () => TextMapExpression,
}

export type TextMapExpression = {
  /** Insert or replace a key's value with a StringExpression */
  put: (key: string, value: StringExpression) => TextMapExpression,
  /** Remove a key (optional) */
  remove?: (key: string) => TextMapExpression,
  /** Retrieve the value expression for a key (missing keys may produce an empty StringExpression) */
  get: (key: string) => StringExpression,
  /** Existence check: returns a ConditionExpression */
  has: (key: string) => ConditionExpression,
  /** Equality check: compare stored value to provided StringExpression */
  equals: (key: string, value: StringExpression) => ConditionExpression,
}

export type NumberMapExpressionApi = {
  create: () => NumberMapExpression,
}

export type NumberMapExpression = {
  put: (key: string, value: NumberExpression) => NumberMapExpression,
  remove?: (key: string) => NumberMapExpression,
  get: (key: string) => NumberExpression,
  has: (key: string) => ConditionExpression,
  equals: (key: string, value: NumberExpression) => ConditionExpression,
}
```

### Notes

- `put` replaces an existing keyed value for that map key.
- Always wrap primitive literals using `hostApi.string.of(...)` and `hostApi.number.of(...)`.
- Follow the repository/indexing pattern for optional `asRule`/`getRule` support