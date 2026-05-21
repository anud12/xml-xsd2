import { NumberExpression } from "./numberExpression";
import { MaybeExpression } from "./maybeExpression";
import { ConditionExpression } from "./conditionExpression";

/**
 * Marker type for ListExpression values on HostApi surfaces.
 *
 * Pass this as the `type` field in event/effect argument declarations to
 * signal that the argument carries a ListExpression.
 *
 * @see ListExpressionApi.type
 */
export type ListExpressionType = {
  // used when declaring argument types dynamically in HostApi clients
};

/**
 * An immutable, lazily-evaluated ordered sequence expression.
 *
 * Represents ordered collections of element expressions (strings, numbers,
 * entities, etc.). Evaluation is deferred and performed by the runtime.
 *
 * `of(...items)` on the Api is **eager** when all items are literals;
 * all other operations produce lazy nodes.
 *
 * ListExpression wrappers are truthy in JS but must not be implicitly
 * coerced to arrays — explicit runtime evaluation is required.
 *
 * ## Evaluation semantics
 *
 * - `of(...items)`: accepts variable arguments; if all items are literal
 *   primitives, `of()` eagerly converts and caches the literal list. If
 *   items include expression nodes, `of()` stores references and evaluation
 *   is deferred until list evaluation.
 * - `concat(other)`: at evaluation, evaluate left then right and return
 *   concatenated sequence.
 * - `append(item)`: evaluate receiver to a sequence, evaluate `item` and
 *   append resulting single element.
 * - `group(expr)`: controls grouping/evaluation boundaries for composed
 *   list nodes.
 * - `get(index)`: evaluate the list, evaluate `index`, and return a
 *   MaybeExpression containing the element when present. Out-of-bounds
 *   returns an absent MaybeExpression (fail-soft).
 * - `length()`: returns a NumberExpression representing element count
 *   (evaluated at extraction time).
 * - `forEach(cb)`: evaluate the list; for each element, evaluate the
 *   element expression to a host value and invoke `cb(elementValue, index)`.
 *   Callbacks run at evaluation time for side-effects; returns void.
 * - `map(cb)`: lazily transform each element by invoking `cb` with the
 *   element **expression** (not evaluated value). `cb` should return an
 *   expression or literal. Produces a new ListExpression.
 * - `isContaining(element)`: returns a ConditionExpression that is true
 *   if some evaluated element equals the provided element.
 * - `oneOf(choices)`: treat the list as a set of alternative lists and
 *   pick one deterministically using the instance RNG. Returns a
 *   MaybeExpression containing the chosen ListExpression.
 * - `randomElement()`: selects one element using deterministic instance
 *   RNG semantics. Returns a MaybeExpression containing the chosen
 *   element when the list is non-empty.
 *
 * ## Failure modes
 *
 * - Out-of-bounds access: `get(index)` returns an absent MaybeExpression
 *   by default (fail-soft); strict mode can throw.
 * - Empty lists: `randomElement()` on empty lists returns an absent
 *   MaybeExpression (fail-soft).
 * - Deeply-nested or huge lists: avoid naive expansion; provide streaming
 *   or capped enumeration.
 * - Cyclic rule refs: detect and cap expansion depth (configurable) to
 *   avoid infinite loops.
 *
 * @see specification/expressions/listExpression.md
 * @see ListExpressionApi
 */
export type ListExpression<T = unknown> = {
  /**
   * Convenience factory: create a literal list.
   *
   * Delegates to {@link ListExpressionApi.of}. Eagerly computes and caches
   * when all items are literals.
   */
  of: (...items: T[]) => ListExpression<T>;

  /**
   * Concatenate two lists.
   *
   * Evaluates the receiver, then `other`, and returns their concatenation.
   */
  concat: (other: ListExpression<T>) => ListExpression<T>;

  /**
   * Append a single element to the list.
   *
   * Evaluates the receiver to a sequence, evaluates `element`, and appends it.
   */
  append: (element: T) => ListExpression<T>;

  /**
   * Grouping node to control evaluation order within composed list expressions.
   */
  group: (expr: ListExpression<T>) => ListExpression<T>;

  /**
   * Zero-based index access.
   *
   * Evaluates the list, evaluates `index` (NumberExpression semantics), and
   * returns a MaybeExpression containing the element when present. Returns
   * `None` when the index is out of bounds (fail-soft); implementations may
   * provide a strict mode that throws.
   *
   * @param index - Zero-based index (NumberExpression).
   */
  get: (index: NumberExpression) => MaybeExpression<T>;

  /**
   * Length of the list as a NumberExpression.
   *
   * Evaluated at extraction time.
   */
  length: () => NumberExpression;

  /**
   * Iterate elements and invoke `cb` for side-effects.
   *
   * Evaluates the list and invokes `cb(elementValue, index)` for each element
   * at evaluation time. Returns void.
   *
   * @note `cb` is explicitly side-effecting. Prefer pure {@link map} when
   *       building new expression trees rather than observing values.
   */
  forEach: (cb: (element: T, index?: number) => void) => void;

  /**
   * Lazily transform each element.
   *
   * Invokes `cb` with each element **expression** (not the evaluated value).
   * `cb` should return an expression or literal. Produces a new ListExpression
   * where each element is the callback's result. Preserves laziness.
   *
   * @param cb - Callback receiving the element expression and optional index,
   *             returning an expression or literal.
   */
  map: (cb: (elementExpr: T, index?: number) => any) => ListExpression<T>;

  /**
   * Existential membership test.
   *
   * Returns a ConditionExpression that is true if some evaluated element
   * equals `element` (equality semantics depend on element type).
   */
  isContaining: (element: T) => ConditionExpression;

  /**
   * Deterministic selection of one whole list from a collection of alternatives.
   *
   * Picks one `ListExpression` from `choices` using the instance RNG. Returns
   * a MaybeExpression containing the chosen list. If `choices` is empty,
   * returns `None`.
   *
   * @param choices - Array of ListExpression alternatives.
   * @see specification/runtime/randomness.md
   */
  oneOf?: (choices: ListExpression<T>[]) => MaybeExpression<T>;

  /**
   * Deterministic random element selection.
   *
   * Selects one element from the list using the runtime's deterministic
   * instance RNG. Returns a MaybeExpression containing the chosen element.
   * Returns `None` when the list is empty.
   *
   * @see specification/runtime/randomness.md
   */
  randomElement: () => MaybeExpression<T>;
};

/**
 * HostApi surface for constructing and registering {@link ListExpression}
 * values.
 *
 * Exposed as `hostApi.list` inside module scripts.
 *
 * ## Conversions (JS <-> host)
 *
 * - Input (JS -> host): `null`/`undefined` -> error. Arguments passed to
 *   `of()` become the list elements; the API accepts variable arguments
 *   (e.g., `of(a, b, c)` or `of(expr1, expr2)`). Element values that are
 *   primitives are wrapped using the appropriate element-type `of()` where
 *   applicable.
 * - Output (host -> JS): Evaluating a ListExpression returns a host sequence
 *   (e.g., List<T> or array) whose elements are the evaluated results of the
 *   element expressions.
 *
 * ## Implementation notes
 *
 * - **ListExpression<T> is immutable** with an operation queue. The
 *   underlying list value never changes; only the queued operations grow.
 * - **Sequential execution**: operations in the queue apply in declaration
 *   order when the expression is evaluated.
 * - `of(...items)` is eagerly computed and cached for literal items only;
 *   other constructors remain lazy.
 * - `forEach(...)` invokes callbacks at evaluation time and is explicitly
 *   side-effecting; callers should avoid non-deterministic side-effects
 *   and prefer pure transforms where possible.
 *
 * @example
 * ```ts
 * const palette = hostApi.list.of(
 *   hostApi.string.of("red"),
 *   hostApi.string.of("blue"),
 * );
 * const size   = palette.length();                // NumberExpression
 * const first  = palette.get(hostApi.number.of(0)); // MaybeExpression
 * const chosen = palette.randomElement();           // MaybeExpression
 * const more   = palette.concat(hostApi.list.of(hostApi.string.of("green")));
 * ```
 *
 * @see ListExpression
 * @see specification/expressions/listExpression.md
 */
export type ListExpressionApi = {
  /**
   * Create a literal list from variable arguments.
   *
   * Eagerly computes and caches the list when all items are literals.
   * Mixed expression items remain lazy until evaluation.
   *
   * @param items - Element expressions or literal values.
   */
  of: <T>(...items: T[]) => ListExpression<T>;

  /**
   * Register or replace a named ListExpression rule in the rule repository.
   *
   * Returns the API surface for fluent chaining.
   *
   * @param ruleName - Unique rule identifier.
   * @param expr     - The ListExpression to register.
   */
  asRule: (ruleName: string, expr: ListExpression<unknown>) => ListExpressionApi;

  /**
   * Return a ListExpression that resolves the named rule at evaluation time.
   *
   * @param ruleName - Rule identifier to look up.
   */
  getRule: (ruleName: string) => ListExpression<unknown>;

  /**
   * Type marker for HostApi surfaces.
   *
   * Pass `hostApi.list.type` as the `type` field when declaring event or
   * effect arguments dynamically. No runtime behavior.
   */
  type: ListExpressionType;
};
