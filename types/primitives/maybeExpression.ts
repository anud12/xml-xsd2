/**
 * MaybeExpression — Minimal API
 *
 * Models an optional value expression (presence/absence) with lazy evaluation.
 * This is intentionally minimal and focuses on composition and branch handling.
 */
export type MaybeExpression = {
  /**
   * Maps the contained value via cb if present. Returns a new MaybeExpression.
   *
   * Evaluation: if the receiver contains a value, invokes cb (lazily) and returns its result; otherwise returns a none node.
   * Callbacks must return a MaybeExpression. Callbacks are not invoked until evaluation time.
   */
  map: (cb: () => MaybeExpression) => MaybeExpression;

  /**
   * Chains another optional-producing callback if this is present (flatMap/andThen semantics).
   */
  andThen: (cb: () => MaybeExpression) => MaybeExpression;

  /**
   * Provides a fallback by lazily invoking cb if receiver is none.
   */
  orElse: (cb: () => MaybeExpression) => MaybeExpression;

  /**
   * Invokes cb lazily if the receiver is Some; otherwise does nothing. Useful for side-effectful evaluation during runtime.
   */
  ifSome: (cb: () => MaybeExpression) => MaybeExpression;

  /**
   * Invokes cb lazily if the receiver is None; otherwise does nothing.
   */
  ifNone: (cb: () => MaybeExpression) => MaybeExpression;
};

/**
 * API surface for factories and rule registration/lookup.
 */
export type MaybeExpressionApi = {
  /**
   * Constructs a Some(value) node when value is provided, or a None node when value is undefined/null.
   *
   * Each call returns a new MaybeExpression instance.
   */
  of: (value?: unknown) => MaybeExpression;

  /**
   * Registers or replaces a named rule in the maybe rule repository.
   */
  asRule: (ruleName: string, expr: MaybeExpression) => MaybeExpressionApi;

  /**
   * Returns a MaybeExpression that resolves the named rule at evaluation time.
   */
  getRule: (ruleName: string) => MaybeExpression;

  /**
   * Marker for HostApi surfaces. Used for type branding or runtime identification.
   */
  type: unknown; // Placeholder for MaybeExpressionType
};
