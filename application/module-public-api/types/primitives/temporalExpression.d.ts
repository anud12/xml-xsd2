/**
 * A lazy in-game duration expression.
 *
 * TemporalExpressions represent durations in module-defined units
 * (e.g., "round", "day") mapped to an internal **Game Time Unit (GTU)**
 * clock. Used for cooldowns and effect reoccurrence scheduling.
 *
 * ## GTU Clock model
 *
 * The runtime maintains a monotonically increasing GTU counter. Each tick
 * the counter advances by `tickAdvancesBy` GTU (default `0` — time is frozen).
 * Modules opt in to time-based mechanics by declaring `tickAdvancesBy`
 * exactly once across all loaded modules.
 *
 * Named units are integer multiples of 1 GTU registered via `defineUnit`.
 * Unit names must be globally unique; duplicate names are load-time errors.
 *
 * ## Evaluation semantics
 *
 * - `temporal.of(n, unitName)` -> evaluates to `n x unit.magnitudeInGTU`
 *   GTU (integer, lazily computed).
 * - `multiply(factor)` -> multiplies the GTU total by `factor`, then
 *   `floor`s to the nearest integer GTU. Values <= 0 are treated as 0 GTU
 *   (schedules for next tick).
 * - `max(other)` / `min(other)` -> standard comparison over resolved GTU
 *   values.
 * - All arithmetic uses 64-bit signed integer semantics (same as
 *   NumberExpression — overflow wraps).
 *
 * ## Failure modes
 *
 * | Scenario                              | Mitigation                                    |
 * |---------------------------------------|-----------------------------------------------|
 * | Two modules declare `tickAdvancesBy`   | Load-time error `E_TEMPORAL_SCALE_CONFLICT`   |
 * | Two modules declare same unit name     | Load-time error `E_TEMPORAL_UNIT_CONFLICT`    |
 * | `magnitudeInGTU` evaluates to <= 0     | Load-time error `E_TEMPORAL_UNIT_INVALID`     |
 * | Unknown `unitName` in `temporal.of`    | Load-time error (static); runtime: 0 GTU + log|
 * | `multiply` produces <= 0 GTU           | Treated as 0 GTU (next tick); runtime logs    |
 * | `tickAdvancesBy = 0` with temporal use | Runtime warning at load time; delays infinite |
 *
 * ## Frozen time (`tickAdvancesBy = 0`)
 *
 * When the default of `0` is left unchanged:
 * - The GTU counter never advances.
 * - All TemporalExpression-based cooldowns and delays are permanently
 *   unelapsed.
 * - `reoccurAfterMs` effects using TemporalExpression never re-trigger.
 * - This is intentional: time-based mechanics are **opt-in**.
 *
 * @see specification/expressions/temporalExpression.md
 * @see TemporalExpressionApi
 */
export type TemporalExpression = {
  /**
   * Scale this duration by a numeric factor.
   *
   * Useful for stat-based cooldowns (e.g., cooldown halved by actor speed).
   * The result is **floor'd** to the nearest integer GTU. Values that resolve
   * to <= 0 GTU are treated as 0 GTU (schedules for the next available tick).
   *
   * @param factor - A NumberExpression scaling factor.
   */
  multiply: (factor: import('./numberExpression').NumberExpression) => TemporalExpression;

  /**
   * Return the longer of this duration and `other`.
   *
   * Compares resolved GTU values and returns the greater one.
   */
  max: (other: TemporalExpression) => TemporalExpression;

  /**
   * Return the shorter of this duration and `other`.
   *
   * Compares resolved GTU values and returns the lesser one.
   */
  min: (other: TemporalExpression) => TemporalExpression;
};

/**
 * HostApi surface for configuring the game clock and constructing
 * {@link TemporalExpression} values.
 *
 * Exposed as `hostApi.temporal` inside module scripts.
 *
 * ## World configuration
 *
 * `tickAdvancesBy` must be declared **exactly once** across all loaded modules.
 * If no module declares it, the default is `0` (time is frozen — opt-in
 * behavior). Declaring from two different modules is a load-time error.
 *
 * ## Unit registration
 *
 * Units are registered via `defineUnit`. Unit names must be unique across
 * all loaded modules. The optional `displayName` field provides a
 * human-readable label for UI rendering (does not need to be unique).
 *
 * @example
 * ```ts
 * // World setup (one module only)
 * hostApi.temporal.tickAdvancesBy(hostApi.number.of(5)); // 1 tick = 5 GTU
 * hostApi.temporal.defineUnit("round", hostApi.number.of(6),  { displayName: "Round" });
 * hostApi.temporal.defineUnit("day",   hostApi.number.of(8640), { displayName: "Day" });
 *
 * // Usage: 2 rounds = 12 GTU -> expires after ceil(12 / 5) = 3 ticks
 * const twoCooldown = hostApi.temporal.of(hostApi.number.of(2), "round");
 * ```
 *
 * @see TemporalExpression
 * @see specification/expressions/temporalExpression.md
 */
export type TemporalExpressionApi = {
  /**
   * Declare how many GTU each runtime tick advances the game clock.
   *
   * Must be called **exactly once** across all loaded modules.
   * Default: `0` (game time does not advance — all time-based mechanics
   * are effectively disabled).
   *
   * @param gtu - GTU advancement per tick; must evaluate to a non-negative integer.
   * @throws E_TEMPORAL_SCALE_CONFLICT at load time if called more than once.
   */
  tickAdvancesBy: (gtu: import('./numberExpression').NumberExpression) => TemporalExpressionApi;

  /**
   * Register a named time unit defined as `magnitudeInGTU` base GTU.
   *
   * Unit names must be globally unique across all loaded modules. The optional
   * `displayName` provides a human-readable label for UI rendering (does not
   * need to be unique).
   *
   * @param unitName        - Globally unique unit identifier (e.g., "round").
   * @param magnitudeInGTU  - How many GTU one unit equals; must evaluate to > 0.
   * @param options.displayName - UI label for this unit (e.g., "Round").
   * @throws E_TEMPORAL_UNIT_CONFLICT  at load time if `unitName` already exists.
   * @throws E_TEMPORAL_UNIT_INVALID   at load time if `magnitudeInGTU` <= 0.
   */
  defineUnit: (
    unitName: string,
    magnitudeInGTU: import('./numberExpression').NumberExpression,
    options?: { displayName?: string }
  ) => TemporalExpressionApi;

  /**
   * Create a duration: `n x <named unit>` GTU.
   *
   * Evaluates lazily to `n x unit.magnitudeInGTU` GTU (integer). Unknown
   * `unitName` is a load-time error when statically known, or a runtime
   * warning (treated as 0 GTU) when dynamic.
   *
   * @param n        - Number of units (a NumberExpression).
   * @param unitName - Registered unit name (e.g., "round").
   */
  of: (n: import('./numberExpression').NumberExpression, unitName: string) => TemporalExpression;

  /**
   * Register or replace a named TemporalExpression rule in the rule repository.
   *
   * Returns the API surface for fluent chaining.
   *
   * @param ruleName - Unique rule identifier.
   * @param expr     - The TemporalExpression to register.
   */
  asRule: (ruleName: string, expr: TemporalExpression) => TemporalExpressionApi;

  /**
   * Return a TemporalExpression that resolves the named rule at evaluation time.
   *
   * @param ruleName - Rule identifier to look up.
   */
  getRule: (ruleName: string) => TemporalExpression;

  /**
   * Type marker for HostApi surfaces.
   *
   * Pass `hostApi.temporal.type` as the `type` field when declaring event or
   * effect arguments dynamically. No runtime behavior.
   */
  type: unknown;
};
