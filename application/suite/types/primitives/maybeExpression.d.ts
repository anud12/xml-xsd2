import {ConditionExpression} from "./conditionExpression";

export type MaybeExpressionApi = {
  /** Wrap an expression/value as present */
  of: <T>(v: T) => MutableMaybeExpression<T>,
  /** Create an absent value */
  none: <T>() => MutableMaybeExpression<T>,
  /** Marker for HostApi surfaces */
  type: unknown,
}


type ConditionalMaybe<T> = {
  /** Get true/false branches based on condition result */
  getOnTrueOrFalse: <U>(trueCase: MaybeExpression<U>, falseCase: MaybeExpression<U>) => MaybeExpression<U>,
}

export type MaybeExpression<T> = {
  /** Presence checks */
  isPresent: () => ConditionExpression,
  isEmpty: () => ConditionExpression,

  /** Unwrapping */
  orElse: (defaultValue: T) => T,

  /** Lazily transform the wrapped value to another value type. Read-only: returns a new MaybeExpression. */
  map: <U>(mapper: (v: T) => U) => MaybeExpression<U>,

  /** Lazily transform the wrapped value into a MaybeExpression and flatten one level. Read-only. */
  flatMap: <U>(mapper: (v: T) => MaybeExpression<U>) => MaybeExpression<U>,

  /** Check presence and apply a condition on the unwrapped value */
  isCondition: (predicate: (v: T) => ConditionExpression) => ConditionalMaybe<T>,
}

export type MutableMaybeExpression<T> = MaybeExpression<T> & {
  /** Overwrite with a new value */
  set: (v: T) => MutableMaybeExpression<T>,
  /** Remove the value */
  clear: () => MutableMaybeExpression<T>,

  /** Transformations */
  map: <U>(mapper: (v: T) => U) => MaybeExpression<U>,
  flatMap: <U>(mapper: (v: T) => MaybeExpression<U>) => MaybeExpression<U>,
  filter: (predicate: (v: T) => ConditionExpression) => MutableMaybeExpression<T>,

  /** Side-effects */
  ifPresent: (cb: (v: T) => void) => void,
}
