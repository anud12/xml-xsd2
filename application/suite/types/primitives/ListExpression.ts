import {NumberExpression} from "./numberExpression";
import {MaybeExpression} from "./maybeExpression";
import {ConditionExpression} from "./conditionExpression";

export type HostApi = {
/* ... rest of declarations ... */
list: ListExpressionApi
}

export type ListExpressionApi = {
/** Create a literal list (elements may be primitives or expression wrappers). Accepts variable arguments. */
of: <T> (...items: T[]) => ListExpression<T>;

/** Register and retrieve named list rules. */
asRule: (ruleName: string, expr: ListExpression<unknown>) => ListExpressionApi;
getRule: (ruleName: string) => ListExpression<unknown>;
}

export type ListExpressionType = {
// used when declaring argument types dynamically in HostApi clients
}

export type ListExpression<T> = {
/** Convenience to create a literal (delegates to ListExpressionApi.of) */
of: (...items: T[]) => ListExpression<T>;

/** Concatenate two lists */
concat: (other: ListExpression<T>) => ListExpression<T>;

/** Append a single element */
append: (element: T) => ListExpression<T>;

/** Grouping node to control evaluation order */
group: (expr: ListExpression<T>) => ListExpression<T>;

/** Zero-based index access; returns a MaybeExpression containing the evaluated element when present */
get: (index: NumberExpression) => MaybeExpression<T>;

/** Length as a NumberExpression */
length: () => NumberExpression;

/** Iterate elements and invoke cb(elementValue:any, index?:number) for side-effects. Callback invoked at evaluation time. Returns void. */
forEach: (cb: (element: T, index?: number) => void) => void;

/** Lazily transform each element. Callback receives the element expression (not evaluated value) and returns an expression or literal. Returns a new ListExpression. */
map: (cb: (elementExpr: T, index?: number) => any) => ListExpression<T>;

/** Existential membership test returning a ConditionExpression */
isContaining: (element: T) => ConditionExpression;

/** Treat a collection of list alternatives and pick one whole list deterministically.
*  Accepts an array of ListExpression alternatives and returns a MaybeExpression containing the chosen ListExpression.
   */
   oneOf?: (choices: ListExpression<T>[]) => MaybeExpression<T>;

/** Deterministic selection of an element using the instance RNG (returns MaybeExpression) */
randomElement: () => MaybeExpression<T>;
}