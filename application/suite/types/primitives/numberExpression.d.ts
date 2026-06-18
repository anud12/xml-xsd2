import {ConditionExpression} from "./conditionExpression";

export type NumberExpressionApi = {
  of: (number:number) => NumberExpression,
  asRule:(ruleName: string, numberExpression: NumberExpression) => NumberExpressionApi,
  getRule: (ruleName: string) => NumberExpressionApi,
  /** Marker for HostApi surfaces */
  type: unknown,
}


export type NumberExpression = {
  of: (number:number) => NumberExpression,
  sum: (numberExpression:NumberExpression) => NumberExpression,
  subtract: (numberExpression:NumberExpression) => NumberExpression,
  multiply: (numberExpression:NumberExpression) => NumberExpression,
  divide: (numberExpression:NumberExpression) => NumberExpression,
  modulo: (numberExpression:NumberExpression) => NumberExpression,
  random: (fromInclusive:NumberExpression, toInclusive: NumberExpression) => NumberExpression,

  /** Comparison operations returning a lazy ConditionExpression. Prefix 'is' required. */
  isGreaterThan: (other: NumberExpression) => ConditionExpression,
  isLessThan: (other: NumberExpression) => ConditionExpression,
  isGreaterOrEqualTo: (other: NumberExpression) => ConditionExpression,
  isLessOrEqualTo: (other: NumberExpression) => ConditionExpression,
  isEqualTo: (other: NumberExpression) => ConditionExpression,
  isNotEqualTo: (other: NumberExpression) => ConditionExpression,
}