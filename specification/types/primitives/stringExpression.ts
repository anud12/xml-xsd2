import type { ConditionExpression } from './conditionExpression';

/**
 * Marker type for StringExpression values on HostApi surfaces.
 *
 * Pass this as the `type` field in event/effect argument declarations to
 * signal that the argument carries a StringExpression.
 *
 * @see StringExpressionApi.type
 */
export type StringExpressionType = {
  // used when declaring argument types dynamically in HostApi clients
};

/**
 * An immutable, lazily-evaluated string expression tree.
 *
 * Nodes are built via {@link StringExpressionApi.of} and composed with
 * combinators. Evaluation is deferred and performed by the runtime.
 *
 * `of()` on the Api is **eager** (computed and cached at construction time);
 * all other operations produce lazy nodes.
 *
 * StringExpression wrappers are truthy in JS but must not be implicitly
 * coerced to primitive strings — explicit runtime evaluation is required.
 *
 * @see StringExpressionApi
 * @see stringExpression.md
 */
export type StringExpression = {
  /**
   * Convenience factory: creates a literal StringExpression node.
   *
   * Delegates to {@link StringExpressionApi.of}.
   */
  of: (s: string) => StringExpression;

  /**
   * Concatenate two string expressions.
   *
   * Evaluates the receiver, then `other`, and returns their concatenated value.
   *
   * @note Very large concatenations may impact memory; callers should consider
   *       bounding input sizes.
   */
  concat: (other: StringExpression) => StringExpression;

  /**
   * Join multiple string expressions with an optional separator.
   *
   * Evaluates each element of `parts` in declaration order, evaluates
   * `separator` if provided (defaults to empty string), then joins results.
   *
   * @param parts     - Ordered array of string expressions to join.
   * @param separator - Optional separator inserted between elements.
   */
  join: (parts: StringExpression[], separator?: StringExpression) => StringExpression;

  /**
   * Prepend a literal string to this expression.
   *
   * Convenience for `StringExpressionApi.of(s).concat(this)`.
   */
  prefix: (s: string) => StringExpression;

  /**
   * Append a literal string to this expression.
   *
   * Convenience for `this.concat(StringExpressionApi.of(s))`.
   */
  suffix: (s: string) => StringExpression;

  /**
   * Grouping node to control evaluation order within a composed expression.
   *
   * Useful for establishing explicit evaluation boundaries, especially inside
   * nested `oneOf` choices.
   */
  group: (expr: StringExpression) => StringExpression;

  /**
   * Deterministic choice among `choices` alternatives.
   *
   * At evaluation time, selects exactly one entry using the runtime's
   * deterministic instance RNG (ExecutionContext). The choice is reproducible
   * for the same context.
   *
   * @param choices - Non-empty array of alternative StringExpression values.
   * @see randomness.md
   */
  oneOf: (choices: StringExpression[]) => StringExpression;

  /**
   * Reference another registered string rule by id.
   *
   * Resolved at evaluation time via the string rule repository. If the rule
   * is absent, the runtime substitutes an empty string (fail-soft) and logs
   * the event. A strict mode may be provided for CI.
   *
   * @param ruleId - Identifier of the registered StringExpression rule.
   */
  ref: (ruleId: string) => StringExpression;

  /**
   * Return the index of the first occurrence of `other` within this
   * expression's language, or −1 if none exists.
   *
   * Set-semantic: operates over all possible evaluations of both expressions.
   * Returns a NumberExpression (−1 for no match).
   *
   * @note Implementations may use automata-based or bounded-enumeration
   *       approaches; see stringExpression.md for algorithm details.
   */
  indexOfExpression: (other: StringExpression, fromInclusive?: NumberExpressionRef) => NumberExpressionRef;

  /**
   * Existential membership check: returns true if there exists some evaluation
   * of this expression that contains some evaluation of `other` as a substring.
   *
   * Use {@link isContainingExactly} for the stricter universal check.
   *
   * @note May use automata or bounded enumeration internally.
   * @see stringExpression.md for resolution algorithm details
   */
  isContaining: (other: StringExpression) => ConditionExpression;

  /**
   * Universal membership check: returns true only if **every** evaluation of
   * this expression contains **every** evaluation of `other` as a substring.
   *
   * More expensive than {@link isContaining}. Implementations may fall back to
   * conservative results or impose timeouts for complex expressions.
   *
   * @see stringExpression.md for resolution algorithm details
   */
  isContainingExactly: (other: StringExpression) => ConditionExpression;

  /** Optional: convert to upper-case. Implementation may omit. */
  upper?: () => StringExpression;

  /** Optional: convert to lower-case. Implementation may omit. */
  lower?: () => StringExpression;

  /** Optional: trim leading/trailing whitespace. Implementation may omit. */
  trim?: () => StringExpression;
};

/**
 * HostApi surface for constructing and registering {@link StringExpression}
 * values.
 *
 * Exposed as `hostApi.string` inside module scripts.
 *
 * @example
 * ```ts
 * hostApi.string.asRule("title", hostApi.string.of("Gallant"));
 * const hero = hostApi.string.of("Sir ").concat(hostApi.string.ref("title"));
 * // Evaluating hero → "Sir Gallant"
 * ```
 *
 * @see StringExpression
 * @see stringExpression.md
 */
export type StringExpressionApi = {
  /**
   * Create a literal StringExpression node for `s`.
   *
   * Eagerly computes and caches the host String. `null`/`undefined` → error.
   *
   * @param s - A JS string literal.
   */
  of: (s: string) => StringExpression;

  /**
   * Register or replace a named StringExpression rule in the rule repository.
   *
   * Used to define named fragments resolved via `ref(ruleId)` at evaluation
   * time. Returns the API surface for fluent chaining.
   *
   * @param ruleName - Unique identifier for the rule.
   * @param expr     - The StringExpression to register.
   */
  asRule: (ruleName: string, expr: StringExpression) => StringExpressionApi;

  /**
   * Return the API surface scoped to the named rule.
   *
   * The rule is resolved at evaluation time. If absent, the runtime applies
   * its configured fail-soft policy (substitute empty string + log) or throws
   * in strict mode.
   *
   * @param ruleName - Rule identifier to look up.
   */
  getRule: (ruleName: string) => StringExpressionApi;

  /**
   * Type marker for HostApi surfaces.
   *
   * Pass `hostApi.string.type` as the `type` field when declaring event or
   * effect arguments dynamically. No runtime behavior.
   */
  type: StringExpressionType;
};

/** @internal Placeholder for cross-references within StringExpression */
type NumberExpressionRef = unknown;
