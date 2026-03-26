/**
 * ConditionExpression — Minimal API
 *
 * This type models a recursive, immutable boolean expression tree with lazy evaluation and short-circuiting semantics.
 *
 * See per-field documentation for evaluation semantics, edge cases, and tradeoffs.
 */
export type ConditionExpression = {
  /**
   * Short-circuiting logical AND. Returns a new ConditionExpression.
   *
   * Evaluation: evaluates the receiver; if false, returns false without evaluating `other`.
   * Immutable: returns a new ConditionExpression.
   *
   * Edge cases: Deeply nested trees may risk stack overflow; prefer iterative or bounded evaluators.
   */
  and: (other: ConditionExpression) => ConditionExpression;
  /**
   * Short-circuiting logical OR. Returns a new ConditionExpression.
   *
   * Evaluation: evaluates the receiver; if true, returns true without evaluating `other`.
   * Immutable: returns a new ConditionExpression.
   *
   * Edge cases: Deeply nested trees may risk stack overflow; prefer iterative or bounded evaluators.
   */
  or: (other: ConditionExpression) => ConditionExpression;

  /**
   * Logical NOT. Returns a new ConditionExpression.
   *
   * Evaluation: inverts the boolean result of the receiver.
   * Immutable: returns a new ConditionExpression.
   */
  negate: () => ConditionExpression;

  /**
   * Lazily invokes cb only if the receiver evaluates to true.
   *
   * Evaluation: callback is only invoked if the receiver's value is true; otherwise, returns false without invoking cb.
   * Callbacks must return a ConditionExpression. Callbacks are not invoked until evaluation time.
   *
   * Edge cases: Callbacks must be pure or their side-effects must be acceptable, as they run at evaluation time.
   */
  ifTrue: (cb: () => ConditionExpression) => ConditionExpression;
  /**
   * Lazily invokes cb only if the receiver evaluates to false.
   *
   * Evaluation: callback is only invoked if the receiver's value is false; otherwise, returns false without invoking cb.
   * Callbacks must return a ConditionExpression. Callbacks are not invoked until evaluation time.
   *
   * Edge cases: Callbacks must be pure or their side-effects must be acceptable, as they run at evaluation time.
   */
  ifFalse: (cb: () => ConditionExpression) => ConditionExpression;
};

/**
 * API surface for factories and rule registration/lookup.
 *
 * - of(value): Returns a fresh literal node.
 * - asRule(ruleName, expr): Registers/replaces a named rule in the repository.
 * - getRule(ruleName): Returns a rule-reference node, resolved at evaluation time.
 * - type: Marker for HostApi surfaces.
 */
export type ConditionExpressionApi = {
  /**
   * Returns a fresh literal node for the given boolean value.
   *
   * Each call returns a new ConditionExpression instance; callers must not rely on object identity across calls.
   *
   * Edge cases: Only accepts boolean values. Returns a node that always evaluates to the provided value.
   */
  of: (value: boolean) => ConditionExpression;
  /**
   * Registers or replaces a named rule in the condition rule repository.
   *
   * Returns the API surface for fluent host usage.
   *
   * Edge cases: Overwrites any existing rule with the same name. Rule names must be unique within the repository.
   */
  asRule: (ruleName: string, expr: ConditionExpression) => ConditionExpressionApi;
  /**
   * Returns a ConditionExpression that resolves the named rule at evaluation time.
   *
   * Evaluation: At evaluation, resolves `ruleName` from the condition rule repository and evaluates the resolved expression.
   *
   * Edge cases: If the rule is missing, must fail predictably or as specified by the runtime contract (e.g., fail-fast or fail-soft).
   */
  getRule: (ruleName: string) => ConditionExpression;
  /**
   * Marker for HostApi surfaces. Used for type branding or runtime identification.
   *
   * No runtime behavior; for type-level distinction only.
   */
  type: unknown; // Placeholder for ConditionExpressionType
};
