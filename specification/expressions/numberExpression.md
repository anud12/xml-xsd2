
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

For `random` use notions defined in [randomness.md](../runtime/randomness.md)


## Overflow

All NumberExpression arithmetic is done unbounded precision, reduce result modulo 2^64, then interpret the 64‑bit pattern as a signed two’s‑complement integer (range −2^63 .. 2^63−1). Results wrap; no exception by default (same behaviour as Java long).

References:
- [Java Language Specification — Integral Types and Values](https://docs.oracle.com/javase/specs/jls/se17/html/jls-4.html#jls-4.2)
- [C# —  checked (overflow checking)](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/checked),
- [C# — unchecked](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/unchecked),
- [Rust — Integer overflow (The Rust Book)](https://doc.rust-lang.org/book/ch03-02-data-types.html#integer-overflow)
- [Rust — std::num::Wrapping](https://doc.rust-lang.org/std/num/struct.Wrapping.html)

## Host Api 

The runtime exposes the following TypeScript declaration file (.d.ts) which enhances the HostApi.

### API Structure

**NumberOperations** is the factory and operation builder:
```typescript
export type NumberOperations = {
    /** Create a constant value (returns NumberExpression immediately) */
    of: (number: number) => NumberExpression,
    
    /** Create a deterministically random value (returns NumberExpression immediately) */
    random: (fromInclusive: NumberExpression, toInclusive: NumberExpression) => NumberExpression,
    
    /** Build an operation: add */
    add: (value: NumberExpression) => NumberOperations,
    
    /** Build an operation: subtract */
    subtract: (value: NumberExpression) => NumberOperations,
    
    /** Build an operation: multiply */
    multiply: (value: NumberExpression) => NumberOperations,
    
    /** Build an operation: divide (throws if value == 0 at build time) */
    divide: (value: NumberExpression) => NumberOperations,
    
    /** Evaluate this operation sequence against a given value */
    evaluate: (value: NumberExpression) => NumberExpression,
}
```

**NumberExpression** is the lazy expression tree (composition only):
```typescript
export type NumberExpression = {
    /** Apply an operation to transform the current value. Returns self for chaining. */
    apply: (operation: NumberOperations) => NumberExpression,
    
    /** Replace the current value entirely (reset point). Returns self for chaining. */
    set: (value: NumberExpression) => NumberExpression,
    
    /** Comparison operations returning a lazy ConditionExpression. Prefix 'is' required. */
    isGreaterThan: (other: NumberExpression) => ConditionExpression,
    isLessThan: (other: NumberExpression) => ConditionExpression,
    isGreaterOrEqualTo: (other: NumberExpression) => ConditionExpression,
    isLessOrEqualTo: (other: NumberExpression) => ConditionExpression,
    isEqualTo: (other: NumberExpression) => ConditionExpression,
    isNotEqualTo: (other: NumberExpression) => ConditionExpression,
}

export type HostApi = {
    /* ... rest of declarations ... */
    number: NumberOperations
}
```

### Implementation Notes

- **`NumberExpression` is a constant value** with an operation queue. The underlying numeric value never changes; only the queued operations grow.
- **`.apply(operation)`** appends the operation to the queue and returns `this` for chaining.
- **`.set(value)`** discards the current queue and replaces the value with a new one. Returns `this` for chaining. Useful for control flow (e.g., reset to a default).
- **`NumberOperations.divide(0)`** throws immediately at build time (fail-fast).
- **Sequential execution**: operations in the queue execute in declaration order when the expression is evaluated.