import type { ConditionExpression } from './conditionExpression';

/**
 * Marker type for NumberExpression values on HostApi surfaces.
 *
 * Pass this as the `type` field in event/effect argument declarations to
 * signal that the argument carries a NumberExpression.
 *
 * @see NumberExpressionApi.type
 */
export type NumberExpressionType = {
  // used when declaring type of arguments dynamically
};

/**
 * An immutable, lazily-evaluated 64-bit signed integer expression tree.
 *
 * Represents game numbers as host `long` values using Java-style two's-
 * complement arithmetic. All arithmetic is performed with unbounded precision
 * and then reduced modulo 2^64 (range: −2^63 .. 2^63−1). Overflow wraps —
 * no exception by default.
 *
 * Comparison methods produce lazy {@link ConditionExpression} nodes for
 * composing with boolean logic.
 *
 * Evaluation is deferred and performed by the runtime at commit time.
 * Exception: {@link NumberExpressionApi.of} is **eager** (computed and cached
 * at construction time).
 *
 * @see NumberExpressionApi
 * @see numberExpression.md
 */
export type NumberExpression = {
  /**
   * Arithmetic addition.
   *
   * Lazy: evaluates the receiver, then `other`. Computes the sum with unbounded
   * precision, reduces modulo 2^64, and interprets the result as a signed
   * two's-complement long. Overflow wraps (Java-style).
   *
   * @example `of(MAX_LONG).sum(of(1))` → `MIN_LONG` (wrap-around)
   */
  sum: (other: NumberExpression) => NumberExpression;

  /**
   * Arithmetic subtraction.
   *
   * Lazy: evaluates receiver − other with unbounded precision, reduces modulo
   * 2^64, interprets as signed long. May be implemented as addition of the
   * negated right-hand side.
   */
  subtract: (other: NumberExpression) => NumberExpression;

  /**
   * Arithmetic multiplication.
   *
   * Lazy: multiplies with unbounded precision, reduces modulo 2^64. Large
   * products wrap according to modulo semantics.
   */
  multiply: (other: NumberExpression) => NumberExpression;

  /**
   * Integer division. Truncates toward zero.
   *
   * Lazy: evaluates both operands and performs integer division. Recommended
   * rounding: truncate toward zero (3 / 2 = 1, −3 / 2 = −1).
   *
   * @note Division-by-zero behavior is runtime-defined. Recommendation:
   *       fail-fast with a descriptive error.
   */
  divide: (other: NumberExpression) => NumberExpression;

  /**
   * Deterministic random selection within an inclusive range.
   *
   * Selects uniformly from [fromInclusive, toInclusive] using the runtime's
   * randomness context (ExecutionContext). Results are deterministic and
   * reproducible for a given context.
   *
   * @param fromInclusive - Lower bound (inclusive).
   * @param toInclusive   - Upper bound (inclusive).
   * @note If fromInclusive > toInclusive, behavior is runtime-defined
   *       (recommendation: swap bounds or throw). Equal bounds return that value.
   * @see randomness.md
   */
  random: (fromInclusive: NumberExpression, toInclusive: NumberExpression) => NumberExpression;

  // ── Comparison operations ─────────────────────────────────────────────────
  // All comparisons evaluate both operands to host-long values and compare
  // using signed semantics. Each returns a lazy ConditionExpression.

  /** Returns a ConditionExpression that is true when `this > other`. */
  isGreaterThan: (other: NumberExpression) => ConditionExpression;

  /** Returns a ConditionExpression that is true when `this < other`. */
  isLessThan: (other: NumberExpression) => ConditionExpression;

  /** Returns a ConditionExpression that is true when `this >= other`. */
  isGreaterOrEqualTo: (other: NumberExpression) => ConditionExpression;

  /** Returns a ConditionExpression that is true when `this <= other`. */
  isLessOrEqualTo: (other: NumberExpression) => ConditionExpression;

  /** Returns a ConditionExpression that is true when `this == other`. */
  isEqualTo: (other: NumberExpression) => ConditionExpression;

  /** Returns a ConditionExpression that is true when `this != other`. */
  isNotEqualTo: (other: NumberExpression) => ConditionExpression;
};

/**
 * HostApi surface for constructing and registering {@link NumberExpression}
 * values.
 *
 * Exposed as `hostApi.number` inside module scripts.
 *
 * @example
 * ```ts
 * const ten = hostApi.number.of(10);
 * const doubled = ten.multiply(hostApi.number.of(2));
 * const isPositive = ten.isGreaterThan(hostApi.number.of(0));
 * ```
 *
 * @see NumberExpression
 * @see numberExpression.md
 */
export type NumberExpressionApi = {
  /**
   * Eagerly convert a JS number to a host-long literal node.
   *
   * Conversion rules (JS Number → host long):
   * - `NaN` → throws at call time
   * - `±Infinity` → throws at call time
   * - Non-integers → truncated toward zero before conversion (3.9 → 3, −2.9 → −2)
   * - After truncation, value is reduced into the 64-bit two's-complement range
   *
   * The result is cached. NumberExpression nodes are opaque wrappers in JS —
   * callers must not rely on implicit numeric coercion.
   *
   * @param value - A finite JS number to convert.
   */
  of: (value: number) => NumberExpression;

  /**
   * Register or replace a named NumberExpression rule in the rule repository.
   *
   * Overwrites any existing rule with the same name. Returns the API surface
   * for fluent chaining.
   *
   * @param ruleName - Unique identifier for the rule.
   * @param expr     - The NumberExpression to register.
   */
  asRule: (ruleName: string, expr: NumberExpression) => NumberExpressionApi;

  /**
   * Return the API surface scoped to the named rule, resolved at evaluation
   * time.
   *
   * If the rule is missing at evaluation time, the runtime should fail-fast
   * with a descriptive error (recommended) or apply its configured fail-soft
   * policy.
   *
   * @param ruleName - Rule identifier to look up.
   */
  getRule: (ruleName: string) => NumberExpressionApi;

  /**
   * Type marker for HostApi surfaces.
   *
   * Pass `hostApi.number.type` as the `type` field when declaring event or
   * effect arguments dynamically. No runtime behavior.
   */
  type: NumberExpressionType;
};
