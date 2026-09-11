import {NumberExpression} from "./primitives/numberExpression";
import {ConditionExpression} from "./primitives/conditionExpression";

export type NumberMap = {
  [name:string]: NumberExpression, //colection of `NumberExpression` values accesible by `name`.
}

export type NumberMapExpressionApi = {
  create: () => NumberMapExpression,
}

export type NumberMapExpression = {
  put: (key: string, value: NumberExpression) => NumberMapExpression,
  remove?: (key: string) => NumberMapExpression,
  get: (key: string) => NumberExpression,
  has: (key: string) => ConditionExpression,
  equals: (key: string, value: NumberExpression) => ConditionExpression,
}