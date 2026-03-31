import type { ConditionExpression } from './conditionExpression';
import type { NumberExpression } from './numberExpression';
import type { MaybeExpression } from './maybeExpression';

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
 * {@link ListExpressionApi.of} is **eager** when all items are literals;
 * all other operations produce lazy nodes.
 *
 * ListExpression wrappers are truthy in JS but must not be implicitly coerced
 * to arrays — explicit runtime evaluation is required.
 *
 * @note The `any` element type in this surface reflects the intentionally
 *       generic nature of the spec. Concrete host bindings should provide typed
 *       helpers (e.g. `listOfStrings`) for ergonomic usage.
 *
 * @see ListExpressionApi
 * @see listExpression.md
 */
export type ListExpression = {
  /**
   * Convenience factory: create a literal list.
   *
   * Delegates to {@link ListExpressionApi.of}.
   */
  of: (...items: any[]) => ListExpression;

  /**
   * Concatenate two lists.
   *
   * Evaluates the receiver, then `other`, and returns their concatenation.
   */
  concat: (other: ListExpression) => ListExpression;

  /**
   * Append a single element to the list.
   *
   * Evaluates the receiver to a sequence, evaluates `element`, and appends it.
   */
  append: (element: any) => ListExpression;

  /**
   * Grouping node to control evaluation order within composed list expressions.
   */
  group: (expr: ListExpression) => ListExpression;

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
  get: (index: NumberExpression) => MaybeExpression<any>;

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
  forEach: (cb: (element: any, index?: number) => void) => void;

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
  map: (cb: (elementExpr: any, index?: number) => any) => ListExpression;

  /**
   * Existential membership test.
   *
   * Returns a ConditionExpression that is true if some evaluated element equals
   * `element` (equality semantics depend on element type).
   */
  isContaining: (element: any) => ConditionExpression;

  /**
   * Deterministic selection of one whole list from a collection of alternatives.
   *
   * Picks one `ListExpression` from `choices` using the instance RNG. Returns a
   * MaybeExpression containing the chosen list. If `choices` is empty, returns
   * `None`.
   *
   * @param choices - Array of ListExpression alternatives.
   * @see randomness.md
   */
  oneOf?: (choices: ListExpression[]) => MaybeExpression<ListExpression>;

  /**
   * Deterministic random element selection.
   *
   * Selects one element from the list using the runtime's deterministic instance
   * RNG. Returns a MaybeExpression containing the chosen element. Returns `None`
   * when the list is empty.
   *
   * @see randomness.md
   */
  randomElement: () => MaybeExpression<any>;
};

/**
 * HostApi surface for constructing and registering {@link ListExpression}
 * values.
 *
 * Exposed as `hostApi.list` inside module scripts.
 *
 * @example
 * ```ts
 * const palette = hostApi.list.of(
 *   hostApi.string.of("red"),
 *   hostApi.string.of("blue"),
 * );
 * const size    = palette.length();                 // NumberExpression
 * const first   = palette.get(hostApi.number.of(0)); // MaybeExpression
 * const chosen  = palette.randomElement();            // MaybeExpression
 * const doubled = hostApi.list.of(hostApi.number.of(1)).concat(palette);
 * ```
 *
 * @see ListExpression
 * @see listExpression.md
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
  of: (...items: any[]) => ListExpression;

  /**
   * Register or replace a named ListExpression rule in the rule repository.
   *
   * Returns the API surface for fluent chaining.
   *
   * @param ruleName - Unique rule identifier.
   * @param expr     - The ListExpression to register.
   */
  asRule: (ruleName: string, expr: ListExpression) => ListExpressionApi;

  /**
   * Return a ListExpression that resolves the named rule at evaluation time.
   *
   * @param ruleName - Rule identifier to look up.
   */
  getRule: (ruleName: string) => ListExpression;

  /**
   * Type marker for HostApi surfaces.
   *
   * Pass `hostApi.list.type` as the `type` field when declaring event or
   * effect arguments dynamically. No runtime behavior.
   */
  type: ListExpressionType;
};
