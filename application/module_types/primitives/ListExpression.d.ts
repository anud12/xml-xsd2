import {Expression} from "../Expression";
import {NumberReadOnlyExpression} from "./numberExpression";
import {MaybeReadOnlyExpression} from "./maybeExpression";
import {ConditionReadOnlyExpression} from "./conditionExpression";
import {PrimitiveReadOnlyExpression} from "./primitiveExpression";

export type HostApi = {
  /* ... rest of declarations ... */
  list: ListReadOnlyExpressionApi
}

export type ListReadOnlyExpressionApi = {
  /** Create a literal list (elements may be primitives or expression wrappers). Accepts variable arguments. */
  of: <T extends Expression> (...items: T[]) => ListReadOnlyExpression<T>;

  /** Register and retrieve named list rules. */
  asRule: (ruleName: string, expr: ListReadOnlyExpression<Expression>) => ListReadOnlyExpressionApi;
  getRule: (ruleName: string) => ListReadOnlyExpression<Expression>;
}

export type ListReadOnlyExpressionType = {
// used when declaring argument types dynamically in HostApi clients
}

export type ListReadOnlyExpression<T extends Expression> = PrimitiveReadOnlyExpression & {
  /** Convenience to create a literal (delegates to ListReadOnlyExpressionApi.of) */
  of: (...items: T[]) => ListReadOnlyExpression<T>;

  /** Concatenate two lists */
  concat: (other: ListReadOnlyExpression<T>) => ListReadOnlyExpression<T>;

  /** Append a single element */
  append: (element: T) => ListReadOnlyExpression<T>;

  /** Grouping node to control evaluation order */
  group: (expr: ListReadOnlyExpression<T>) => ListReadOnlyExpression<T>;

  /** Zero-based index access; returns a MaybeReadOnlyExpression containing the evaluated element when present */
  get: (index: NumberReadOnlyExpression) => MaybeReadOnlyExpression<T>;

  /** Length as a NumberReadOnlyExpression */
  length: () => NumberReadOnlyExpression;

  /** The element count as a NumberReadOnlyExpression. Reflects the current size of the list, so it updates whenever the underlying collection changes. */
  count: () => NumberReadOnlyExpression;

  /** Iterate elements and invoke cb(elementValue:any, index?:number) for side-effects. Callback invoked at evaluation time. Returns void. */
  forEach: (cb: (element: T, index?: number) => void) => void;

  /** Lazily transform each element. The callback receives the element expression (not the evaluated value) and returns an expression or literal. Always returns a new `ListReadOnlyExpression<U>` of the mapped elements. */
  map: <U extends Expression>(cb: (elementExpr: T, index?: number) => U) => ListReadOnlyExpression<U>;

  /** Existential membership test returning a ConditionReadOnlyExpression */
  isContaining: (element: T) => ConditionReadOnlyExpression<T>;

  /** Treat a collection of list alternatives and pick one whole list deterministically.
   *  Accepts an array of ListReadOnlyExpression alternatives and returns a MaybeReadOnlyExpression containing the chosen ListReadOnlyExpression.
   */
  oneOf?: (choices: ListReadOnlyExpression<T>[]) => MaybeReadOnlyExpression<T>;

  /** Deterministic selection of an element using the instance RNG (returns MaybeReadOnlyExpression) */
  randomElement: () => MaybeReadOnlyExpression<T>;

  /** Extracts the element when the list holds exactly one value; an absent `MaybeReadOnlyExpression` when it holds zero or more than one. */
  exactlyOne: () => MaybeReadOnlyExpression<T>;
}