import {Expression} from "../Expression";
import {ConditionReadOnlyExpression} from "./conditionExpression";
import {PrimitiveReadOnlyExpression} from "./primitiveExpression";

export type MaybeReadOnlyExpressionApi = {
  /** Wrap an expression/value as present */
  of: <T extends Expression>(v: T) => MaybeReadOnlyExpression<T>,
  /** Create an absent value */
  none: <T extends Expression>() => MaybeReadOnlyExpression<T>,

  /** Register/lookup named maybe rules (optional) */
  asRule: <T extends Expression>(ruleName: string, expr: MaybeReadOnlyExpression<T>) => MaybeReadOnlyExpressionApi,
  getRule:<T extends Expression>(ruleName: string) => MaybeReadOnlyExpression<T>,
  /** Marker for HostApi surfaces */
  type: unknown,
}


type ConditionalMaybe<T extends Expression> = {
  /** Get true/false branches based on condition result */
  getOnTrueOrFalse: <U extends Expression>(trueCase: MaybeReadOnlyExpression<U>, falseCase: MaybeReadOnlyExpression<U>) => MaybeReadOnlyExpression<U>,
}

export type MaybeReadOnlyExpression<T extends Expression> = PrimitiveReadOnlyExpression & {
  of: (v: T) => MaybeReadOnlyExpression<T>,
  none: () => MaybeReadOnlyExpression<T>,

  /** Presence checks */
  isPresent: () => ConditionReadOnlyExpression<T>,
  isEmpty: () => ConditionReadOnlyExpression<T>,

  /** Transformations */
  map: <U extends Expression>(mapper: (v: T) => U) => MaybeReadOnlyExpression<U>,
  flatMap: <U extends Expression>(mapper: (v: T) => MaybeReadOnlyExpression<U>) => MaybeReadOnlyExpression<U>,
  filter: (predicate: (v: T) => ConditionReadOnlyExpression<T>) => MaybeReadOnlyExpression<T>,

  /** Unwrapping */
  orElse: (defaultValue: T) => T,

  /** Side-effects */
  ifPresent: (cb: (v: T) => void) => void,

  /** Check presence and apply a condition on the unwrapped value */
  isCondition: (predicate: (v: T) => ConditionReadOnlyExpression<T>) => ConditionalMaybe<T>,
}