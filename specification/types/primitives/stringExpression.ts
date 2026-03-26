/**
 * StringExpression — Minimal API
 *
 * This type models an immutable string expression tree with lazy evaluation semantics.
 *
 * See per-field documentation for evaluation semantics, edge cases, and tradeoffs.
 */
export type StringExpression = {
  /**
   * Concatenation. Returns a new StringExpression.
   *
   * Evaluation: evaluates the receiver and `other`, then concatenates their string values.
   * Immutable: returns a new StringExpression.
   *
   * Edge cases: callers should consider very large concatenations which may impact memory.
   */
  concat: (other: StringExpression) => StringExpression;

  /**
   * Trims whitespace from both ends. Returns a new StringExpression.
   *
   * Evaluation: evaluates the receiver and returns its trimmed string.
   */
  trim: () => StringExpression;

  /**
   * Converts to upper-case. Returns a new StringExpression.
   */
  toUpperCase: () => StringExpression;

  /**
   * Converts to lower-case. Returns a new StringExpression.
   */
  toLowerCase: () => StringExpression;

  /**
   * Lazily invokes cb only if the receiver evaluates to a non-empty string.
   *
   * Evaluation: callback is only invoked if the receiver's string length > 0; otherwise returns an empty-string node without invoking cb.
   * Callbacks must return a StringExpression. Callbacks are not invoked until evaluation time.
   */
  ifNonEmpty: (cb: () => StringExpression) => StringExpression;

  /**
   * Lazily invokes cb only if the receiver evaluates to an empty string.
   *
   * Evaluation: callback is only invoked if the receiver's string is empty; otherwise returns the receiver.
   */
  ifEmpty: (cb: () => StringExpression) => StringExpression;
};

/**
 * API surface for factories and rule registration/lookup.
 *
 * - of(value): Returns a fresh literal node.
 * - asRule(ruleName, expr): Registers/replaces a named rule in the repository.
 * - getRule(ruleName): Returns a rule-reference node, resolved at evaluation time.
 * - type: Marker for HostApi surfaces.
 */
export type StringExpressionApi = {
  /**
   * Returns a fresh literal node for the given string value.
   *
   * Each call returns a new StringExpression instance; callers must not rely on object identity across calls.
   *
   * Edge cases: Accepts any JS string. Consumers should decide how to handle very long strings or binary data in strings.
   */
  of: (value: string) => StringExpression;

  /**
   * Registers or replaces a named rule in the string rule repository.
   *
   * Returns the API surface for fluent host usage.
   *
   * Edge cases: Overwrites any existing rule with the same name. Rule names must be unique within the repository.
   */
  asRule: (ruleName: string, expr: StringExpression) => StringExpressionApi;

  /**
   * Returns a StringExpression that resolves the named rule at evaluation time.
   *
   * Evaluation: At evaluation, resolves `ruleName` from the string rule repository and evaluates the resolved expression.
   *
   * Edge cases: If the rule is missing, must fail predictably or as specified by the runtime contract (e.g., fail-fast or fail-soft).
   */
  getRule: (ruleName: string) => StringExpression;

  /**
   * Marker for HostApi surfaces. Used for type branding or runtime identification.
   *
   * No runtime behavior; for type-level distinction only.
   */
  type: unknown; // Placeholder for StringExpressionType
};
