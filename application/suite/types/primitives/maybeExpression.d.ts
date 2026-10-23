import {ConditionReadOnlyExpression} from "./conditionExpression";
import {PrimitiveReadOnlyExpression} from "./primitiveExpression";

export type MaybeReadOnlyExpressionApi = {
  /** Wrap an expression/value as present */
  of: <T>(v: T) => MaybeReadOnlyExpression<T>,
  /** Create an absent value */
  none: <T>() => MaybeReadOnlyExpression<T>,

  /** Register/lookup named maybe rules (optional) */
  asRule: <T>(ruleName: string, expr: MaybeReadOnlyExpression<T>) => MaybeReadOnlyExpressionApi,
  getRule:<T>(ruleName: string) => MaybeReadOnlyExpression<T>,
  /** Marker for HostApi surfaces */
  type: unknown,
}


type ConditionalMaybe<T> = {
  /** Get true/false branches based on condition result */
  getOnTrueOrFalse: <U>(trueCase: MaybeReadOnlyExpression<U>, falseCase: MaybeReadOnlyExpression<U>) => MaybeReadOnlyExpression<U>,
}

export type MaybeReadOnlyExpression<T> = PrimitiveReadOnlyExpression & {
  of: (v: T) => MaybeReadOnlyExpression<T>,
  none: () => MaybeReadOnlyExpression<T>,

  /** Presence checks */
  isPresent: () => ConditionReadOnlyExpression,
  isEmpty: () => ConditionReadOnlyExpression,

  /** Transformations */
  map: <U>(mapper: (v: T) => U) => MaybeReadOnlyExpression<U>,
  flatMap: <U>(mapper: (v: T) => MaybeReadOnlyExpression<U>) => MaybeReadOnlyExpression<U>,
  filter: (predicate: (v: T) => ConditionReadOnlyExpression) => MaybeReadOnlyExpression<T>,

  /** Unwrapping */
  orElse: (defaultValue: T) => T,

  /** Side-effects */
  ifPresent: (cb: (v: T) => void) => void,

  /** Check presence and apply a condition on the unwrapped value */
  isCondition: (predicate: (v: T) => ConditionReadOnlyExpression) => ConditionalMaybe<T>,
}