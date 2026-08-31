import {NumberExpression} from "./numberExpression";
import {MaybeExpression, MutableMaybeExpression} from "./maybeExpression";
import {ConditionExpression} from "./conditionExpression";

export type HostApi = {
/* ... rest of declarations ... */
list: ListExpressionApi
}

export type ListExpressionApi = {
/** Create a literal immutable list (elements may be primitives or expression wrappers). Accepts variable arguments. */
of: <T> (...items: T[]) => ListExpression<T>;

/** Register and retrieve named list rules. */
asRule: (ruleName: string, expr: ListExpression<unknown>) => ListExpressionApi;
getRule: (ruleName: string) => ListExpression<unknown>;
}

export type ListExpressionType = {
// used when declaring argument types dynamically in HostApi clients
}

export type ListExpression<T> = {
/** Zero-based index access; returns a MaybeExpression containing the evaluated element when present */
get: (index: NumberExpression) => MaybeExpression<T>;

/** Length as a NumberExpression */
length: () => NumberExpression;

/** Iterate elements and invoke cb(elementValue:any, index?:number) for side-effects. Callback invoked at evaluation time. Returns void. */
forEach: (cb: (element: T, index?: number) => void) => void;

/** Lazily transform each element. Callback receives the element expression (not evaluated value) and returns an expression or literal. Returns a new ListExpression. */
map: (cb: (elementExpr: T, index?: number) => any) => ListExpression<T>;

/** Lazily transform each element into a MaybeExpression and flatten one level. Returns a new ListExpression of unwrapped elements. */
flatMap: (cb: (elementExpr: T, index?: number) => MaybeExpression<any>) => ListExpression<any>;

/** Existential membership test returning a ConditionExpression */
isContaining: (element: T) => ConditionExpression;

/** Deterministic selection of an element using the instance RNG (returns MaybeExpression) */
randomElement: () => MaybeExpression<T>;
}

export type MutableListExpression<T> = ListExpression<T> & {
/** Convenience to create a literal mutable list (delegates to ListExpressionApi.of) */
of: (...items: T[]) => MutableListExpression<T>;

/** Concatenate two lists. Returns a new list. */
concat: (other: ListExpression<T>) => MutableListExpression<T>;

/** Append a single element. Returns self. */
append: (element: T) => MutableListExpression<T>;

/** Grouping node to control evaluation order. Returns self. */
group: (expr: ListExpression<T>) => MutableListExpression<T>;

/** Remove the element at the given index. Returns self. */
removeAt: (index: NumberExpression) => MutableListExpression<T>;

/** Set the element at the given index. Returns self. */
set: (index: NumberExpression, element: T) => MutableListExpression<T>;

/** Treat a collection of list alternatives and pick one whole list deterministically.
*  Accepts an array of ListExpression alternatives and returns a MaybeExpression containing the chosen ListExpression.
   */
   oneOf: (choices: ListExpression<T>[]) => MutableMaybeExpression<ListExpression<T>>;
}
