import {ConditionReadOnlyExpression} from "./conditionExpression";
import {PrimitiveReadOnlyExpression} from "./primitiveExpression";

export type NumberReadOnlyExpressionApi = {
  of: (number:number) => NumberReadOnlyExpression,
  asRule:(ruleName: string, numberExpression: NumberReadOnlyExpression) => NumberReadOnlyExpressionApi,
  getRule: (ruleName: string) => NumberReadOnlyExpressionApi,
  /** Marker for HostApi surfaces */
  type: unknown,
}


export type NumberReadOnlyExpression = PrimitiveReadOnlyExpression & {
  of: (number:number) => NumberReadOnlyExpression,
  sum: (numberExpression:NumberReadOnlyExpression) => NumberReadOnlyExpression,
  subtract: (numberExpression:NumberReadOnlyExpression) => NumberReadOnlyExpression,
  multiply: (numberExpression:NumberReadOnlyExpression) => NumberReadOnlyExpression,
  divide: (numberExpression:NumberReadOnlyExpression) => NumberReadOnlyExpression,
  modulo: (numberExpression:NumberReadOnlyExpression) => NumberReadOnlyExpression,
  random: (fromInclusive:NumberReadOnlyExpression, toInclusive: NumberReadOnlyExpression) => NumberReadOnlyExpression,

  /** Comparison operations returning a lazy ConditionReadOnlyExpression. Prefix 'is' required. */
  isGreaterThan: (other: NumberReadOnlyExpression) => ConditionReadOnlyExpression,
  isLessThan: (other: NumberReadOnlyExpression) => ConditionReadOnlyExpression,
  isGreaterOrEqualTo: (other: NumberReadOnlyExpression) => ConditionReadOnlyExpression,
  isLessOrEqualTo: (other: NumberReadOnlyExpression) => ConditionReadOnlyExpression,
  isEqualTo: (other: NumberReadOnlyExpression) => ConditionReadOnlyExpression,
  isNotEqualTo: (other: NumberReadOnlyExpression) => ConditionReadOnlyExpression,
}