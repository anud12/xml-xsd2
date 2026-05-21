/// <reference path="conditionExpression.d.ts" />
/// <reference path="expression.d.ts" />
/// <reference path="ListExpression.ts" />
/// <reference path="maybeExpression.d.ts" />
/// <reference path="numberExpression.d.ts" />
/// <reference path="stringExpression.d.ts" />
/// <reference path="temporalExpression.d.ts" />

/**
 * Barrel module for all primitive expression types.
 *
 * These types form the expression algebra that the runtime evaluates lazily.
 * Every expression is **immutable** and **lazy** (with the exception of `of()`,
 * which is eager and cached). The runtime builds expression trees at module
 * load time and evaluates them at commit time each tick.
 *
 * ## Primitives
 *
 * | Type                   | Purpose                              |
 * |------------------------|--------------------------------------|
 * | `NumberExpression`     | 64-bit signed integer arithmetic     |
 * | `StringExpression`     | String concatenation and composition |
 * | `ConditionExpression`  | Lazy boolean logic                   |
 * | `MaybeExpression<T>`   | Optional / nullable values           |
 * | `ListExpression<T>`    | Ordered sequences                    |
 * | `TemporalExpression`   | In-game duration (GTU-based)         |
 *
 * ## Shared design principles
 *
 * - **Immutability**: every operation returns a new expression; the receiver
 *   is never mutated.
 * - **Lazy evaluation**: nodes are constructed at load time and evaluated by
 *   the runtime at commit time. `of()` is the only eager factory.
 * - **Rule repository**: each API surface provides `asRule()` / `getRule()`
 *   for registering and looking up named expression fragments.
 * - **Type markers**: each API exposes a `type` field used by the HostApi
 *   to declare expected argument types for events and effects.
 * - **Determinism**: expressions that involve randomness use the runtime's
 *   deterministic instance RNG (SplitMix64) for reproducibility.
 *
 * @see specification/expressions/
 * @see specification/runtime/randomness.md
 */
export {};
