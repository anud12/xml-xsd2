import { ConditionExpression } from "./conditionExpression";

/**
 * Marker type for MaybeExpression values on HostApi surfaces.
 *
 * Pass this as the `type` field in event/effect argument declarations to
 * signal that the argument carries a MaybeExpression.
 *
 * @see MaybeExpressionApi.type
 */
export type MaybeExpressionType = {
  // marker for HostApi surfaces
};

/**
 * An immutable, lazily-evaluated optional value expression.
 *
 * Models a value that may be present (`Some<T>`) or absent (`None`).
 * Evaluation is deferred and performed by the runtime.
 *
 * ## Failure semantics
 *
 * By default the runtime is **fail-soft**: if evaluating an inner expression
 * throws or a referenced rule is missing, the operation treats it as `None`
 * and logs the event. A strict mode can be provided for CI/test runs.
 *
 * ## Interop with other expressions
 *
 * Design decision: **explicit unwrapping preferred**. Expressions that expect
 * a concrete type (e.g., `StringExpression.concat`) SHALL require callers to
 * pass a concrete value. Passing a `Maybe<T>` must be explicitly unwrapped
 * (e.g., `maybeValue.orElse(fallback)`). Implicit coercion is dangerous
 * because absent values might silently become context-dependent defaults
 * and lead to subtle bugs.
 *
 * Prefer explicit unwrapping via {@link orElse}, {@link map}, or
 * {@link flatMap} rather than relying on host-side null checks — absent
 * values represented as `null` in JS can be a source of subtle bugs.
 *
 * @template T - The type of the contained value expression.
 *
 * @see specification/expressions/maybeExpression.md
 * @see MaybeExpressionApi
 */
export type MaybeExpression<T> = {
  /**
   * Convenience factory: wrap a value as `Some`.
   *
   * Delegates to {@link MaybeExpressionApi.of}.
   */
  of: (v: T) => MaybeExpression<T>;

  /**
   * Convenience factory: produce an absent `None` node.
   *
   * Delegates to {@link MaybeExpressionApi.none}.
   */
  none: () => MaybeExpression<T>;

  /**
   * Returns a ConditionExpression that is true when this is `Some`.
   *
   * Evaluated lazily.
   */
  isPresent: () => ConditionExpression;

  /**
   * Returns a ConditionExpression that is true when this is `None`.
   *
   * Evaluated lazily.
   */
  isEmpty: () => ConditionExpression;

  /**
   * Transform the contained value if present.
   *
   * If `Some`, evaluates the contained value, invokes `mapper(value)` to
   * produce a result `U`, and wraps it in `Some<U>`. If `None`, returns a
   * `None<U>` without invoking `mapper`.
   *
   * @param mapper - Pure function mapping a present value to a new value.
   */
  map: <U>(mapper: (v: T) => U) => MaybeExpression<U>;

  /**
   * Chain another optional-producing callback (flatMap / andThen semantics).
   *
   * Like {@link map} but `mapper` returns a `MaybeExpression<U>` directly.
   * The result is flattened — `Some(Some(x))` becomes `Some(x)`.
   *
   * @param mapper - Pure function returning a MaybeExpression.
   */
  flatMap: <U>(mapper: (v: T) => MaybeExpression<U>) => MaybeExpression<U>;

  /**
   * Filter the contained value using a predicate.
   *
   * If `Some` and `predicate(value)` evaluates to true, keeps the value.
   * Otherwise returns `None`.
   *
   * @param predicate - A ConditionExpression-returning predicate.
   */
  filter: (predicate: (v: T) => ConditionExpression) => MaybeExpression<T>;

  /**
   * Unwrap the value, providing a fallback for the absent case.
   *
   * If `Some`, returns the contained value. If `None`, evaluates and returns
   * `defaultValue`.
   *
   * @param defaultValue - Fallback value returned when this is `None`.
   */
  orElse: (defaultValue: T) => T;

  /**
   * Side-effecting callback invoked when the value is present.
   *
   * Invokes `cb(value)` if `Some`; does nothing if `None`. Returns void.
   *
   * @note Callbacks run at evaluation time. Prefer pure `map`/`flatMap` when
   *       no side-effects are needed.
   */
  ifPresent: (cb: (v: T) => void) => void;
};

/**
 * HostApi surface for constructing and registering {@link MaybeExpression}
 * values.
 *
 * Exposed as `hostApi.maybe` inside module scripts.
 *
 * ## Conversions (JS <-> host)
 *
 * - Input (JS -> host):
 *   - `hostApi.maybe.of(expr)` wraps an expression/value into a
 *     MaybeExpression that will evaluate as present.
 *   - `hostApi.maybe.none()` produces an absent MaybeExpression.
 * - Output (host -> JS):
 *   - Evaluating a MaybeExpression yields either a present host value or
 *     an absent marker. Host bindings SHOULD represent absence as `null`
 *     in JS (or an idiomatic host Optional type in typed hosts such as
 *     Java's `Optional<T>`). Consumers should prefer `orElse`/`map`/`flatMap`
 *     rather than relying on raw host null checks.
 *
 * ## Implementation notes
 *
 * - **MaybeExpression<T> is immutable** with an operation queue. The
 *   underlying value (present/absent) never changes; only the queued
 *   operations grow.
 * - **Sequential execution**: operations in the queue apply in declaration
 *   order when the expression is evaluated.
 * - **Fail-soft by default**: if evaluating an inner expression throws or
 *   a referenced rule is missing, the operation treats it as `None` and
 *   logs the event.
 *
 * @example
 * ```ts
 * const maybeName = hostApi.maybe.of(hostApi.string.of("Alice"));
 * const name = maybeName.orElse(hostApi.string.of("Anonymous"));
 * const upper = maybeName.map(s => s.upper?.() ?? s);
 * ```
 *
 * @template T - The type of the contained value expression.
 *
 * @see MaybeExpression
 * @see specification/expressions/maybeExpression.md
 */
export type MaybeExpressionApi = {
  /**
   * Wrap a value as a `Some` node.
   *
   * @param v - The value to wrap as present.
   */
  of: <T>(v: T) => MaybeExpression<T>;

  /**
   * Produce an absent `None` node.
   */
  none: <T>() => MaybeExpression<T>;

  /**
   * Register or replace a named maybe rule in the rule repository.
   *
   * Returns the API surface for fluent chaining.
   *
   * @param ruleName - Unique rule identifier.
   * @param expr     - The MaybeExpression to register.
   */
  asRule: <T>(ruleName: string, expr: MaybeExpression<T>) => MaybeExpressionApi;

  /**
   * Return a MaybeExpression that resolves the named rule at evaluation time.
   *
   * @param ruleName - Rule identifier to look up.
   */
  getRule: <T>(ruleName: string) => MaybeExpression<T>;

  /**
   * Type marker for HostApi surfaces.
   *
   * Pass `hostApi.maybe.type` as the `type` field when declaring event or
   * effect arguments dynamically. No runtime behavior.
   */
  type: MaybeExpressionType;
};
