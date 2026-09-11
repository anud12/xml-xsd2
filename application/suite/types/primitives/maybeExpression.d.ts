import {ConditionExpression} from "./conditionExpression";

export type MaybeExpressionApi = {
  /** Wrap an expression/value as present */
  of: <T>(v: T) => MaybeExpression<T>,
  /** Create an absent value */
  none: <T>() => MaybeExpression<T>,

  /** Register/lookup named maybe rules (optional) */
  asRule: <T>(ruleName: string, expr: MaybeExpression<T>) => MaybeExpressionApi,
  getRule:<T>(ruleName: string) => MaybeExpression<T>,
  /** Marker for HostApi surfaces */
  type: unknown,
}


type ConditionalMaybe<T> = {
  /** Get true/false branches based on condition result */
  getOnTrueOrFalse: <U>(trueCase: MaybeExpression<U>, falseCase: MaybeExpression<U>) => MaybeExpression<U>,
}

export type MaybeExpression<T> = {
  of: (v: T) => MaybeExpression<T>,
  none: () => MaybeExpression<T>,

  /** Presence checks */
  isPresent: () => ConditionExpression,
  isEmpty: () => ConditionExpression,

  /** Transformations */
  map: <U>(mapper: (v: T) => U) => MaybeExpression<U>,
  flatMap: <U>(mapper: (v: T) => MaybeExpression<U>) => MaybeExpression<U>,
  filter: (predicate: (v: T) => ConditionExpression) => MaybeExpression<T>,

  /** Unwrapping */
  orElse: (defaultValue: T) => T,

  /** Side-effects */
  ifPresent: (cb: (v: T) => void) => void,

  /** Check presence and apply a condition on the unwrapped value */
  isCondition: (predicate: (v: T) => ConditionExpression) => ConditionalMaybe<T>,
}