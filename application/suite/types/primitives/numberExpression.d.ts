import {ConditionExpression} from "./conditionExpression";

export type NumberExpressionApi = {
  of: (number:number) => MutableNumberExpression,
  /** Marker for HostApi surfaces */
  type: unknown,
}


export type NumberExpression = {
  /** Comparison operations returning a lazy ConditionExpression. Prefix 'is' required. */
  isGreaterThan: (other: NumberExpression) => ConditionExpression,
  isLessThan: (other: NumberExpression) => ConditionExpression,
  isGreaterOrEqualTo: (other: NumberExpression) => ConditionExpression,
  isLessOrEqualTo: (other: NumberExpression) => ConditionExpression,
  isEqualTo: (other: NumberExpression) => ConditionExpression,
  isNotEqualTo: (other: NumberExpression) => ConditionExpression,
}

export type MutableNumberExpression = NumberExpression & {
  set: (numberExpression: NumberExpression) => MutableNumberExpression,
  sum: (numberExpression: NumberExpression) => MutableNumberExpression,
  subtract: (numberExpression: NumberExpression) => MutableNumberExpression,
  multiply: (numberExpression: NumberExpression) => MutableNumberExpression,
  divide: (numberExpression: NumberExpression) => MutableNumberExpression,
  modulo: (numberExpression: NumberExpression) => MutableNumberExpression,
  random: (fromInclusive: NumberExpression, toInclusive: NumberExpression) => MutableNumberExpression,
}
