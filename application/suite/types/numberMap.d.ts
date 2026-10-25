import {NumberReadOnlyExpression} from "./primitives/numberExpression";
import {ConditionReadOnlyExpression} from "./primitives/conditionExpression";

export type NumberMap = {
  [name:string]: NumberReadOnlyExpression, //colection of `NumberReadOnlyExpression` values accesible by `name`.
}

export type NumberMapReadOnlyExpressionApi = {
  create: () => NumberMapReadOnlyExpression,
}

export type NumberMapReadOnlyExpression = {
  get: (key: string) => NumberReadOnlyExpression,
  has: (key: string) => ConditionReadOnlyExpression<string>,
}