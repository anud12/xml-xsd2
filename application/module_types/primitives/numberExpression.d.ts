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

  /** Comparison operations returning a lazy ConditionReadOnlyExpression carrying this value. Prefix 'is' required. */
  isGreaterThan: (other: NumberReadOnlyExpression) => ConditionReadOnlyExpression<NumberReadOnlyExpression>,
  isLessThan: (other: NumberReadOnlyExpression) => ConditionReadOnlyExpression<NumberReadOnlyExpression>,
  isGreaterOrEqualTo: (other: NumberReadOnlyExpression) => ConditionReadOnlyExpression<NumberReadOnlyExpression>,
  isLessOrEqualTo: (other: NumberReadOnlyExpression) => ConditionReadOnlyExpression<NumberReadOnlyExpression>,
  isEqualTo: (other: NumberReadOnlyExpression) => ConditionReadOnlyExpression<NumberReadOnlyExpression>,
  isNotEqualTo: (other: NumberReadOnlyExpression) => ConditionReadOnlyExpression<NumberReadOnlyExpression>,
}