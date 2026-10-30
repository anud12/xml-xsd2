import {Expression} from "../Expression";
import {NumberReadOnlyExpression} from "./numberExpression";
import {MaybeReadOnlyExpression} from "./maybeExpression";
import {ConditionReadOnlyExpression} from "./conditionExpression";
import {PrimitiveReadOnlyExpression} from "./primitiveExpression";

export type ArrayReadOnlyExpressionType = {
  // marker for dynamic HostApi typing
}

/**
 * A read-only reference to a fixed, ordered array of {@link Expression} values.
 * Index access and element extraction resolve lazily to expressions (a
 * {@link MaybeReadOnlyExpression} when the requested element may be absent).
 */
export type ArrayReadOnlyExpression<T extends Expression> = PrimitiveReadOnlyExpression & {
  /** Convenience to create a literal array (elements may be primitives or expression wrappers). */
  of: (...items: T[]) => ArrayReadOnlyExpression<T>;

  /** Zero-based index access; returns a MaybeReadOnlyExpression containing the element when present. */
  get: (index: NumberReadOnlyExpression) => MaybeReadOnlyExpression<T>;

  /** Length as a NumberReadOnlyExpression. */
  length: () => NumberReadOnlyExpression;

  /** The element count as a NumberReadOnlyExpression. Reflects the current size of the array. */
  count: () => NumberReadOnlyExpression;

  /** Iterate elements and invoke cb(elementValue, index?) for side-effects. Returns void. */
  forEach: (cb: (element: T, index?: number) => void) => void;

  /** Lazily transform each element. Callback receives the element expression (not the evaluated value) and returns an expression or literal. Returns a new ArrayReadOnlyExpression. */
  map: (cb: (elementExpr: T, index?: number) => any) => ArrayReadOnlyExpression<T>;

  /** Existential membership test returning a ConditionReadOnlyExpression. */
  isContaining: (element: T) => ConditionReadOnlyExpression<T>;

  /** Deterministic selection of an element using the instance RNG (returns MaybeReadOnlyExpression). */
  randomElement: () => MaybeReadOnlyExpression<T>;

  /** Extracts the element when the array holds exactly one value; an absent MaybeReadOnlyExpression when it holds zero or more than one. */
  exactlyOne: () => MaybeReadOnlyExpression<T>;
}
