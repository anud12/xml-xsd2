# Randomness — Concepts

This document describes the deterministic randomness system used throughout the engine.

## Summary

All randomness in the engine is **deterministic** and **reproducible**. Given the same world seed and the same sequence of events, every random draw will produce identical results across runs. This is a hard requirement: replayability and testability depend on it.

Random draws are powered by a **randomization table** stored in `WorldMetadata` and an **internal counter** that increments monotonically per world-step instance. A draw consumes one counter position; the result is always derived from `table[counter % table.length]`, never from a live RNG.

---

## Randomization Table

The `WorldMetadata.RandomizationTable` is a pre-generated, fixed-length array of values in the range `[0.0, 1.0)` (exclusive upper bound). The table is part of the world definition and is serialized alongside it.

- **Source**: generated once per world from a seeded PRNG and stored. It is NOT regenerated at runtime.
- **Length**: configurable; must be a positive integer. A minimum length of 256 is recommended; power-of-two sizes are preferred.
- **Distribution**: values should be uniformly distributed in `[0.0, 1.0)`.
- **Immutability**: the table MUST NOT be mutated after the world is loaded.

---

## Internal Counter

Each `WorldStepInstance` holds a single `long` counter, initialized to `0` when the instance is created. Every call to `random()` or `randomFrom()` increments the counter by exactly `1`.

- The counter is **not reset** between rule evaluations within a step; it advances monotonically for the lifetime of the instance.
- Counter overflow wraps (Java `long` semantics); the modulo against `table.length` always produces a valid index.
- The counter state is considered **part of the deterministic execution context**: identical event sequences must produce identical counter progression.

---

## Core API

The engine exposes two primitive draw operations on `WorldStepInstance`:

### `random() → double`

Returns the next value from the randomization table and increments the counter.

```
index = counter % table.length
value = table[index]
counter += 1
return value   // in [0.0, 1.0)
```

### `randomFrom(size: long) → long`

Returns a deterministic index in `[0, size)` (exclusive upper bound). `size` must be ≥ 1.

```
index = (long) Math.floor(random() * size)
return index   // in [0, size)
```

> `size = 0` is an error (throws).

---

## Usage in `NumberExpression`

`NumberExpression.random(fromInclusive, toInclusive)` draws a value in the **closed** integer range `[from, to]`.

Evaluation algorithm:
1. Evaluate `fromInclusive` → `long f`
2. Evaluate `toInclusive` → `long t`
3. If `f > t` → error.
4. `span = t - f + 1`
5. `result = f + worldStepInstance.randomFrom(span)`

The result is a `NumberExpression` node; the draw happens **at evaluation time** (lazy), not at construction time.

---

## Usage in `StringExpression`

`StringExpression.oneOf(choices)` selects exactly one entry from the `choices` list using the deterministic draw.

Evaluation algorithm:
1. `index = worldStepInstance.randomFrom(choices.length)`
2. Evaluate `choices[index]` and return the result.

The draw is consumed at evaluation time. Nested `oneOf` nodes each consume one counter position in declaration/evaluation order.

---

## Reproducibility Contract

Given:
- the same `WorldMetadata.RandomizationTable`,
- the same initial counter value,
- the same sequence of evaluations,

the engine MUST produce identical results on every run and on every conforming implementation.

Implementations MUST NOT:
- Use `Math.random()`, `java.util.Random`, OS entropy, or any live RNG source for game logic.
- Cache or pre-draw results speculatively (this would shift counter positions and break reproducibility).
- Skip incrementing the counter on a draw (e.g., for "optimized" short-circuits).

---

## Failure modes

| Condition | Expected behaviour |
|---|---|
| `randomFrom(0)` | Error / exception |
| `random(from, to)` where `from > to` | Error / exception |
| Table length `0` | Error at world load time |
| Counter overflow | Wraps silently (modulo semantics) |

---

## References

- `WorldMetadata.RandomizationTable` — stores the pre-generated table.
- `WorldStepInstance.random()` / `randomFrom()` — the two primitive draw operations.
- [`NumberExpression`](./numberExpression.md) — uses `random(fromInclusive, toInclusive)`.
- [`StringExpression`](./stringExpression.md) — uses `oneOf` which calls `randomFrom`.
