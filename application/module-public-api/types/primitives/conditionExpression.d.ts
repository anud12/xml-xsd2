/**
 * Marker type for ConditionExpression values on HostApi surfaces.
 *
 * Pass this as the `type` field in event/effect argument declarations to
 * signal that the argument carries a ConditionExpression.
 *
 * @see ConditionExpressionApi.type
 */
export type ConditionExpressionType = {
  // marker for dynamic HostApi typing
};

/**
 * An immutable, lazily-evaluated boolean expression tree.
 *
 * Nodes are built via {@link ConditionExpressionApi.of} and composed with
 * combinators. Evaluation is deferred and performed by the runtime when the
 * expression is applied.
 *
 * All methods return a **new** ConditionExpression — the receiver is never
 * mutated. Short-circuit semantics apply: right-hand operands or callbacks
 * are not evaluated unless required.
 *
 * ## Evaluation semantics
 *
 * - `and(other)`: evaluate left; if false, result is false without evaluating
 *   right.
 * - `or(other)`: evaluate left; if true, result is true without evaluating
 *   right.
 * - `negate()`: evaluate operand and invert the boolean result.
 * - `ifTrue(cb)`: evaluate the receiver; if true, invoke `cb()` to obtain a
 *   ConditionExpression, evaluate that expression and return its result;
 *   otherwise return false without invoking `cb`.
 * - `ifFalse(cb)`: evaluate the receiver; if false, invoke `cb()` to obtain
 *   a ConditionExpression, evaluate that expression and return its result;
 *   otherwise return false without invoking `cb`.
 *
 * Short-circuiting ensures that side-effectful evaluations (if any exist
 * elsewhere in the runtime) are not invoked unless necessary.
 *
 * @see specification/expressions/conditionExpression.md
 * @see ConditionExpressionApi
 */
export type ConditionExpression = {
  /**
   * Short-circuiting logical AND.
   *
   * Evaluates the receiver first. If false, returns false immediately without
   * evaluating `other`. Returns a new ConditionExpression.
   *
   * @note Deeply nested trees may risk stack overflow; prefer iterative or
   *       bounded evaluators.
   */
  and: (other: ConditionExpression) => ConditionExpression;

  /**
   * Short-circuiting logical OR.
   *
   * Evaluates the receiver first. If true, returns true immediately without
   * evaluating `other`. Returns a new ConditionExpression.
   *
   * @note Deeply nested trees may risk stack overflow; prefer iterative or
   *       bounded evaluators.
   */
  or: (other: ConditionExpression) => ConditionExpression;

  /**
   * Logical NOT.
   *
   * Inverts the boolean result of the receiver. Returns a new
   * ConditionExpression.
   */
  negate: () => ConditionExpression;

  /**
   * Lazy branch: invokes `cb` only when the receiver evaluates to `true`.
   *
   * If the receiver is true, `cb()` is invoked at evaluation time to produce
   * a ConditionExpression, and that expression's result is returned. If the
   * receiver is false, returns false without invoking `cb`.
   *
   * @param cb - Pure callback returning a ConditionExpression; invoked only
   *             at evaluation time and only when the receiver is true.
   */
  ifTrue: (cb: () => ConditionExpression) => ConditionExpression;

  /**
   * Lazy branch: invokes `cb` only when the receiver evaluates to `false`.
   *
   * If the receiver is false, `cb()` is invoked at evaluation time to produce
   * a ConditionExpression, and that expression's result is returned. If the
   * receiver is true, returns false without invoking `cb`.
   *
   * @param cb - Pure callback returning a ConditionExpression; invoked only
   *             at evaluation time and only when the receiver is false.
   */
  ifFalse: (cb: () => ConditionExpression) => ConditionExpression;
};

/**
 * HostApi surface for constructing and registering {@link ConditionExpression}
 * values.
 *
 * Exposed as `hostApi.condition` (or destructured equivalent) inside module
 * scripts.
 *
 * ## Implementation notes
 *
 * - **ConditionExpression is immutable** with a combinator queue. The
 *   underlying truth value never changes; only the queued combinators grow.
 * - **Sequential execution**: combinators in the queue apply in declaration
 *   order when the expression is evaluated.
 * - All nodes are lazy; factories construct tree nodes and the runtime is
 *   responsible for evaluation.
 * - `of(value: boolean)` returns a fresh literal node on each call. Callers
 *   must not rely on object identity across separate `of(...)` invocations.
 * - `asRule(ruleName, expr)` registers or replaces the named rule in the
 *   condition rule repository and returns the API surface for fluent host
 *   usage.
 * - `getRule(ruleName)` returns a ConditionExpression that resolves the named
 *   rule at evaluation time.
 * - Callbacks supplied to `ifTrue`/`ifFalse` must return a ConditionExpression.
 *   Callbacks are not invoked until evaluation time and only when the
 *   receiver's evaluation result triggers them.
 *
 * @example
 * ```ts
 * const T = hostApi.condition.of(true);
 * hostApi.condition.asRule("isEnabled", T);
 * const isEnabled = hostApi.condition.getRule("isEnabled");
 *
 * // Callback-based branching — cb is not called unless isEnabled is true
 * const branch = isEnabled.ifTrue(() => hostApi.condition.of(false).or(T));
 * ```
 *
 * @see ConditionExpression
 * @see specification/expressions/conditionExpression.md
 */
export type ConditionExpressionApi = {
  /**
   * Create a literal ConditionExpression node for `value`.
   *
   * Each call returns a new instance. Callers must not rely on object identity
   * across separate `of(...)` invocations.
   *
   * @param value - The boolean literal to wrap.
   */
  of: (value: boolean) => ConditionExpression;

  /**
   * Register or replace a named condition rule in the rule repository.
   *
   * Overwrites any existing rule with the same name. Returns the API surface
   * for fluent chaining.
   *
   * @param ruleName - Unique identifier for the rule within the condition
   *                   repository.
   * @param expr     - The ConditionExpression to associate with the rule.
   */
  asRule: (ruleName: string, expr: ConditionExpression) => ConditionExpressionApi;

  /**
   * Return a ConditionExpression that resolves the named rule at evaluation
   * time.
   *
   * Rule resolution is deferred until the expression tree is evaluated. If
   * the named rule is absent at evaluation time, the runtime should fail-fast
   * with a descriptive error (recommended) or apply its configured fail-soft
   * policy.
   *
   * @param ruleName - Identifier of the rule to look up.
   */
  getRule: (ruleName: string) => ConditionExpression;

  /**
   * Type marker for HostApi surfaces.
   *
   * Pass `hostApi.condition.type` as the `type` field when declaring event or
   * effect arguments dynamically. No runtime behavior.
   */
  type: ConditionExpressionType;
};
