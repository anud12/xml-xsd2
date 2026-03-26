import type { ConditionExpression } from "./conditionExpression";

/**
 * NumberExpression — Per-field documentation (derived from numberExpression.md)
 *
 * Top-level summary: immutable, lazily-evaluated expression tree representing host 64-bit signed integers (`long`).
 * `of()` is eager and caches; all other nodes are lazy. Conversions, overflow, and randomness follow the rules documented per-field below.
 */
export type NumberExpression = {
  /**
   * sum(other)
   * - Purpose: arithmetic addition.
   * - Evaluation: lazy. When the expression tree is evaluated, the left operand (receiver) is evaluated first, then `other`.
   *   Both are converted to host-long semantics (see `of` for conversion rules). Addition is performed with unbounded precision and then
   *   reduced modulo 2^64. The 64-bit pattern is interpreted as a signed two's-complement integer.
   * - Edge cases: overflow wraps around (Java-style). For checked or saturating behavior rely on specific runtime variants.
   * - Examples: sum(of(3), of(5)) -> 8; sum(of(2**63 - 1), of(1)) -> -2**63 (wrap).
   */
  sum: (other: NumberExpression) => NumberExpression;

  /**
   * subtract(other)
   * - Purpose: arithmetic subtraction.
   * - Evaluation: lazy. Evaluates operands, computes difference using unbounded precision, reduces modulo 2^64, interprets as signed long.
   * - Notes: may be implemented by evaluators as addition of the negated right-hand side for efficiency.
   */
  subtract: (other: NumberExpression) => NumberExpression;

  /**
   * multiply(other)
   * - Purpose: arithmetic multiplication.
   * - Evaluation: lazy. Multiply with unbounded precision then reduce modulo 2^64.
   * - Edge cases: large products wrap according to modulo semantics.
   */
  multiply: (other: NumberExpression) => NumberExpression;

  /**
   * divide(other)
   * - Purpose: integer division.
   * - Evaluation: lazy. Evaluate operands and perform integer division. Recommended rounding behaviour: truncate toward zero.
   * - Division by zero: runtime-defined. Recommended: fail-fast with a descriptive error, but implementations may choose another strategy.
   */
  divide: (other: NumberExpression) => NumberExpression;

  /**
   * random(fromInclusive, toInclusive)
   * - Purpose: deterministic random selection within inclusive bounds using the runtime's RandomizationTable.
   * - Evaluation: evaluate bounds first to host-long values, then select uniformly from the inclusive range. Selection must be deterministic
   *   relative to the runtime's randomness state.
   * - Edge cases: if fromInclusive > toInclusive the runtime must decide (recommended: swap or throw). If equal, return that value.
   */
  random: (fromInclusive: NumberExpression, toInclusive: NumberExpression) => NumberExpression;

  /**
   * Comparison helpers — produce lazily-evaluated ConditionExpression nodes.
   * - Purpose: compare two NumberExpression values using signed host-long semantics.
   * - Evaluation: both operands are evaluated to host-long values and compared. Returns a ConditionExpression for composition.
   */
  isGreaterThan: (other: NumberExpression) => ConditionExpression;
  isLessThan: (other: NumberExpression) => ConditionExpression;
  isGreaterOrEqualTo: (other: NumberExpression) => ConditionExpression;
  isLessOrEqualTo: (other: NumberExpression) => ConditionExpression;
  isEqualTo: (other: NumberExpression) => ConditionExpression;
  isNotEqualTo: (other: NumberExpression) => ConditionExpression;
};

/**
 * NumberExpressionApi — Host API surface (per-field notes)
 */
export type NumberExpressionApi = {
  /**
   * of(value)
   * - Purpose: eager literal constructor that converts a JS Number to a host-long literal node and caches the converted value.
   * - Conversion rules (JS Number -> host long):
   *   - NaN -> error (throw at call time).
   *   - ±Infinity -> error (throw at call time).
   *   - Finite non-integers -> truncated toward zero before conversion (3.9 -> 3, -2.9 -> -2).
   *   - After truncation, values are reduced into the 64-bit two's-complement representation as needed.
   * - Notes: callers must not rely on implicit numeric coercion of NumberExpression nodes; they are opaque wrappers in JS.   
   */
  of: (value: number) => NumberExpression;

  /**
   * asRule(ruleName, expr)
   * - Purpose: register or replace a named NumberExpression in the runtime repository.
   * - Semantics: overwrites any existing rule with the same name. Rule names must be unique within the repository's namespace.
   * - Returns: the NumberExpressionApi for fluent chainable host usage.
   */
  asRule: (ruleName: string, expr: NumberExpression) => NumberExpressionApi;

  /**
   * getRule(ruleName)
   * - Purpose: return a NumberExpression node that resolves the named rule at evaluation time.
   * - Semantics: resolution of the named rule is deferred until evaluation. If the rule is missing at evaluation time the runtime should
   *   fail-fast with a descriptive error (recommended), but implementations may choose fail-soft behavior if documented.
   */
  getRule: (ruleName: string) => NumberExpression;

  /**
   * type
   * - Marker for HostApi surfaces. No runtime behavior; for branding and dynamic type checks in host code.
   */
  type: unknown; // Placeholder for NumberExpressionType
};
