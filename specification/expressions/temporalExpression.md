# Temporal Expression — Concepts

This document describes the `TemporalExpression` primitive.

## Summary

`TemporalExpression` is an `immutable`, `lazily-evaluated` expression model representing a **duration of in-game time**. It is intentionally decoupled from real-world wall-clock time and from the runtime tick rate. Durations are expressed in module-defined named units (e.g. `"round"`, `"day"`, `"season"`) which resolve to an internal **Game Time Unit (GTU)** counter that advances at a configurable rate per tick.

This allows game time to run at any speed independently of frame rate — a "day" can take real-world minutes or hours, a "round" can last a handful of ticks, and a "banana" can be whatever the module author decides.

---

## Purpose

- Express cooldowns, reoccurrence delays, and any duration-based mechanic in **game-meaningful units** rather than raw tick counts or milliseconds.
- Allow world designers to control the speed of in-game time independently of the runtime frame rate.
- Provide a display-friendly label for UI rendering (separate from the unit's technical identity).
- Unify duration expressions across the codebase: replaces raw `NumberExpression` (ms) in `effects.md`'s `reoccurAfterMs` and `actions.md`'s `cooldown`.

---

## Core Concepts

### Game Time Unit (GTU)

The **GTU** is the runtime's internal integer clock unit. It is an implementation detail — module authors never reference GTU directly. The runtime maintains a monotonically increasing GTU counter.

Every tick, the runtime advances the GTU counter by `tickAdvancesBy` (an integer declared once per world). If `tickAdvancesBy = 0` (the default), the GTU counter never advances and all time-based mechanics are effectively disabled.

### Named Units

Modules register named time units as integer multiples of 1 GTU. For example:

```
"second"  = 1 GTU
"minute"  = 60 GTU
"round"   = 6 GTU
"day"     = 8640 GTU
"banana"  = 3 GTU
```

With `tickAdvancesBy = 5` (each tick advances 5 GTU):

```
1 "round" = 6 GTU = ceil(6 / 5) = 2 ticks
3 "rounds" = 18 GTU = ceil(18 / 5) = 4 ticks
```

### Scheduling precision

When the runtime schedules a delay (cooldown expiry, reoccurrence), it stores the **target GTU** (`currentGTU + durationGTU`). It checks expiry each tick after advancing the counter. This means the actual real-time precision is bounded by tick duration, but the game-time precision is exact to 1 GTU.

---

## World Configuration

`tickAdvancesBy` must be declared **exactly once** across all loaded modules. If no module declares it, the default is `0` (time is frozen — opt-in behavior).

```ts
// In a world-setup module:
hostApi.temporal.tickAdvancesBy(hostApi.number.of(5)); // each tick = 5 GTU
```

Declaring `tickAdvancesBy` from two different modules is a load-time error: `E_TEMPORAL_SCALE_CONFLICT`.

---

## Unit Registration

Units are registered via `defineUnit`. Unit names must be unique across all loaded modules — a duplicate name is a load-time error: `E_TEMPORAL_UNIT_CONFLICT`.

The optional `displayName` field provides a human-readable label for UI rendering (e.g. showing `"3 rounds"` to the player). `displayName` does not need to be unique.

```ts
hostApi.temporal.defineUnit("round", hostApi.number.of(6), { displayName: "Round" });
hostApi.temporal.defineUnit("day",   hostApi.number.of(8640), { displayName: "Day" });
```

---

## Host API (TypeScript)

## Host API (TypeScript)

### API Structure

**TemporalApi** is the factory and operation builder:
```ts
export type TemporalApi = {
  /**
   * Declare how many GTU each runtime tick advances the game clock.
   * Must be called exactly once across all loaded modules.
   * Default: 0 (game time does not advance — all time-based mechanics disabled).
   * Load-time error if called more than once: E_TEMPORAL_SCALE_CONFLICT.
   */
  tickAdvancesBy: (gtu: NumberExpression) => TemporalApi;

  /**
   * Register a named time unit defined as `magnitudeInGTU` base game-time units.
   * Unit names must be globally unique across all modules.
   * Load-time error on duplicate name: E_TEMPORAL_UNIT_CONFLICT.
   * Load-time error if magnitudeInGTU evaluates to <= 0: E_TEMPORAL_UNIT_INVALID.
   *
   * @param unitName     Unique identifier used in temporal.of(n, unitName)
   * @param magnitudeInGTU  How many GTU one unit of this type equals
   * @param options.displayName  Human-readable label for UI (not required to be unique)
   */
  defineUnit: (
    unitName: string,
    magnitudeInGTU: NumberExpression,
    options?: { displayName?: string }
  ) => TemporalApi;

  /**
   * Create a duration: n × the named unit.
   * Unknown unitName is a load-time error when statically known,
   * or a runtime error (treated as 0 GTU with a log warning) when dynamic.
   */
  of: (n: NumberExpression, unitName: string) => TemporalExpression;
  
  /** Build an operation: multiply duration by factor */
  multiply: (factor: NumberExpression) => TemporalApi,
  
  /** Build an operation: max of two durations */
  max: (other: TemporalExpression) => TemporalApi,
  
  /** Build an operation: min of two durations */
  min: (other: TemporalExpression) => TemporalApi,
  
  /** Evaluate this operation sequence against a given value */
  evaluate: (value: TemporalExpression) => TemporalExpression,

  /** Register and retrieve named temporal duration rules */
  asRule: (ruleName: string, expr: TemporalExpression) => TemporalApi;
  getRule: (ruleName: string) => TemporalExpression;

  /** Marker for HostApi surfaces */
  type: TemporalExpressionType;
}

export type TemporalExpressionType = {
  // marker for dynamic HostApi typing
}
```

**TemporalExpression** is the lazy expression tree (composition only):
```ts
export type TemporalExpression = {
  /** Apply an operation to transform the current value. Returns self for chaining. */
  apply: (operation: TemporalApi) => TemporalExpression;
  
  /** Replace the current value entirely (reset point). Returns self for chaining. */
  set: (value: TemporalExpression) => TemporalExpression;

  /**
   * Scale this duration by a NumberExpression factor.
   * Useful for stat-based cooldowns (e.g. cooldown halved by actor speed).
   * Result is floor'd to the nearest GTU. Values <= 0 are treated as 0 GTU (fires next tick).
   */
  multiply: (factor: NumberExpression) => TemporalExpression;

  /** Return the longer of this duration and `other` */
  max: (other: TemporalExpression) => TemporalExpression;

  /** Return the shorter of this duration and `other` */
  min: (other: TemporalExpression) => TemporalExpression;
}
```

### Implementation Notes

- **`TemporalExpression` is immutable** with an operation queue. The underlying GTU value never changes; only the queued operations grow.
- **`.apply(operation)`** appends the operation to the queue and returns `this` for chaining.
- **`.set(value)`** discards the current queue and replaces the value with a new one. Returns `this` for chaining.
- **Sequential execution**: operations in the queue apply in declaration order when the expression is evaluated.

---

## Evaluation Semantics

- `temporal.of(n, unitName)` → evaluates to `n × unit.magnitudeInGTU` GTU (integer, lazily computed).
- `multiply(factor)` → multiplies the GTU total by `factor`, then `floor`s to the nearest integer GTU.
- `max(other)` / `min(other)` → standard comparison over resolved GTU values.
- All arithmetic uses 64-bit signed integer semantics (same as `NumberExpression` — see `numberExpression.md`).
- The resolved GTU value is used by the runtime to compute `expiresAtGTU = currentGTU + resolvedGTU`.

---

## Examples

### World setup

```ts
// One module configures the clock and registers units
hostApi.temporal.tickAdvancesBy(hostApi.number.of(5));  // 1 tick = 5 GTU

hostApi.temporal.defineUnit("second", hostApi.number.of(1),    { displayName: "Second" });
hostApi.temporal.defineUnit("minute", hostApi.number.of(60),   { displayName: "Minute" });
hostApi.temporal.defineUnit("round",  hostApi.number.of(6),    { displayName: "Round"  });
hostApi.temporal.defineUnit("day",    hostApi.number.of(8640), { displayName: "Day"    });
```

### Action cooldown (fixed)

```ts
cooldown: (_ctx) => hostApi.temporal.of(hostApi.number.of(2), "round")
// 2 rounds × 6 GTU = 12 GTU → expires after ceil(12 / 5) = 3 ticks
```

### Action cooldown (stat-scaled)

```ts
// Cooldown shrinks as actor's "speed" stat increases
cooldown: (ctx) => hostApi.temporal
  .of(hostApi.number.of(10), "round")
  .multiply(
    hostApi.number.of(100)
      .divide(ctx.actor.numberMap.get("speed"))  // e.g. speed=200 → factor=0.5 → 5 rounds
  )
```

### Clamped cooldown (never less than 1 round)

```ts
cooldown: (ctx) => hostApi.temporal
  .of(hostApi.number.of(10), "round")
  .multiply(/* some scaling factor */)
  .max(hostApi.temporal.of(hostApi.number.of(1), "round"))
```

### Effect reoccurrence (replaces raw NumberExpression ms)

```ts
reoccurAfterMs: (_ctx, _count, _input, _output) =>
  hostApi.maybe.of(hostApi.temporal.of(hostApi.number.of(1), "round"))
```

---

## Relationship to `tickAdvancesBy = 0` (frozen time)

When `tickAdvancesBy = 0` (the default):

- The GTU counter never advances.
- All `TemporalExpression`-based cooldowns and delays are permanently unelapsed.
- `reoccurAfterMs` effects using `TemporalExpression` never re-trigger.
- This is intentional: time-based mechanics are **opt-in**. A world without a clock declaration is a timeless world.
- The runtime SHOULD emit a warning at load time when a `TemporalExpression` is used but `tickAdvancesBy` is `0`.

---

## Failure Modes & Edge Cases

| Scenario | Mitigation |
|---|---|
| Two modules declare `tickAdvancesBy` | Load-time error `E_TEMPORAL_SCALE_CONFLICT` |
| Two modules declare same unit name | Load-time error `E_TEMPORAL_UNIT_CONFLICT` |
| `magnitudeInGTU` evaluates to ≤ 0 | Load-time error `E_TEMPORAL_UNIT_INVALID` |
| Unknown `unitName` in `temporal.of(...)` | Load-time error (static); runtime: treat as 0 GTU + log warning |
| `multiply` produces ≤ 0 GTU | Treated as 0 GTU — schedules for next tick; runtime logs warning |
| GTU counter overflow | 64-bit signed integer; at 30fps × 5 GTU/tick the counter overflows after ~195 million years. Acceptable. |
| `tickAdvancesBy = 0` with temporal usage | Runtime warning at load time; all temporal delays behave as infinite |
| Module hot-reload changes `tickAdvancesBy` | Handled by full resync (see `runtime.md`); GTU counter resets or is preserved per resync policy |

---

## Tradeoffs

| + | - |
|---|---|
| Game time fully decoupled from real time and frame rate | `tickAdvancesBy = 0` default may silently break time-based mechanics if forgotten |
| Integer arithmetic throughout — deterministic, no float drift | Floor rounding on `multiply` means very small durations may collapse to 0 GTU |
| `displayName` cleanly separates identity from UI presentation | Unit ID conflicts require cross-module coordination in large module ecosystems |
| Unifies all duration expressions (`cooldown`, `reoccurAfterMs`) | Replaces existing raw `NumberExpression` (ms) — migration step required for `effects.md` |

---

## Cross-References

- [`actions.md`](../interaction/actions.md) — `cooldown` field uses `TemporalExpression`
- [`effects.md`](../interaction/effects.md) — `reoccurAfterMs` should migrate from `NumberExpression` (ms) to `TemporalExpression`
- [`numberExpression.md`](./numberExpression.md) — integer arithmetic semantics (overflow, floor)
- [`runtime.md`](../runtime/runtime.md) — tick loop, ExecutionContext, module hot-reload

---

## Implementation Checklist

- [ ] Maintain GTU counter in runtime tick loop; advance by `tickAdvancesBy` each tick
- [ ] Enforce single `tickAdvancesBy` declaration; default to `0` if absent
- [ ] Store unit registry (`unitName → magnitudeInGTU`, `displayName`) at load time; reject duplicates
- [ ] Evaluate `temporal.of(n, unitName)` to GTU at commit time (lazy)
- [ ] Schedule cooldowns/delays as `expiresAtGTU = currentGTU + resolvedGTU`
- [ ] Check expiry each tick after GTU advance
- [ ] Emit load-time warning when temporal is used with `tickAdvancesBy = 0`
- [x] Migrate `effects.md` `reoccurAfterMs` from `MaybeExpression<NumberExpression>` to `MaybeExpression<TemporalExpression>`

---

Architecture Review Result: Implementation Ready
