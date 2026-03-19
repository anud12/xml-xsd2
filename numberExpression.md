
# Number expression — Concepts

This document describes the core `Number expression` concept.

## Summary
`NumberExpression` is an `immutable`, `lazily-evaluated` expression model representing game numbers as `64-bit signed integers`. The host API builds expression trees via `of`, `sum`, `subtract`, `multiply`, `divide`, `group`, and `random`. `of()` eagerly converts a JS `Number` to the host `long` (truncate toward zero) and caches it; other ops produce nodes evaluated on extraction.

From JS, a `NumberExpression` is a truthy wrapper — implicit numeric coercion should throw. Arithmetic defaults to `Java-style wrap-around` (`mod 2^64`, range `-2^63 .. 2^63-1`); optional checked/saturating variants are recommended.

## Purpose

The game numbers all must be stored in `64-bit signed integer (long)` format. Due to javascript `number` representation is of type `IEEE-754 doubles`, the host must export an `number expression` api on which number constants and math operations can be declared.
The resulting `NumberExpression` object hides the underlying value from javascript, only allowing a set of operations to be executed against it .


## Conversions (host doubles ↔ long)

- Input (double -> long): 
    - NaN→error,
    - ±Inf→error,\
    - truncate, then apply modulo 2^64 or truncate toward zero. Example: `3.9` -> `3`, `-2.9` -> `-2`.
    
- Output (long -> NumberExpression): Object viewed from javascript perspective it hides the underlying number value, is always truthy and when errorly used as a number it should throw exception.

## Grouping, operator evaluation

When the underlying expression is extracted, the operations are executed in the order they are defined due to having a grouping operation if a specific one is desired.

When operations are added, they are lazily evaluated, exception on this rule is the `of` operation, which is eagerly computed and cached.

For `random` use notions defined in [randomness.md](./randomness.md)


## Overflow

All NumberExpression arithmetic is done unbounded precision, reduce result modulo 2^64, then interpret the 64‑bit pattern as a signed two’s‑complement integer (range −2^63 .. 2^63−1). Results wrap; no exception by default (same behaviour as Java long).

References:
- [Java Language Specification — Integral Types and Values](https://docs.oracle.com/javase/specs/jls/se17/html/jls-4.html#jls-4.2)
- [C# —  checked (overflow checking)](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/checked),
- [C# — unchecked](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/unchecked),
- [Rust — Integer overflow (The Rust Book)](https://doc.rust-lang.org/book/ch03-02-data-types.html#integer-overflow)
- [Rust — std::num::Wrapping](https://doc.rust-lang.org/std/num/struct.Wrapping.html)

## Host Api 

The server exposes the following TypeScript declaration file (.d.ts) which enhances the HostApi.
```typescript
export type HostApi = {
/*... rest of declarations */
number: NumberExpressionApi
}


export type NumberExpressionApi = {
    of: (number:number) => NumberExpression,
    asRule:(ruleName: string, numberExpression: NumberExpression) => NumberExpressionApi
    getRule(ruleName: string) => NumberExpressionApi
    type: NumberExpressionType,
}

export type NumberExpressionType {
    //** used when declaring type of arguments dynamically.
}

export type NumberExpression = {
    of: (number:number) => NumberExpression,
    sum: (numberExpression:NumberExpression) => NumberExpression,
    subtract: (numberExpression:NumberExpression) => NumberExpression,
    multiply: (numberExpression:NumberExpression) => NumberExpression,
    divide: (numberExpression:NumberExpression) => NumberExpression,
    random: (fromInclusive:NumberExpression, toInclusive: NumberExpression) => NumberExpression,
}
```