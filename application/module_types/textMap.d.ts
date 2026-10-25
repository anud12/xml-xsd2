import {StringReadOnlyExpression} from "./primitives/stringExpression";
import {ConditionReadOnlyExpression} from "./primitives/conditionExpression";
import {PrimitiveReadOnlyExpression} from "./primitives/primitiveExpression";

export type TextMap = {
  [name:string]: StringReadOnlyExpression, //colection of `StringReadOnlyExpression` values accesible by `name`.
}

export type TextMapReadOnlyExpressionApi = {
  create: () => TextMapReadOnlyExpression,
}

export type TextMapReadOnlyExpression = PrimitiveReadOnlyExpression & {
  /** Retrieve the value expression for a key (missing keys may produce an empty StringReadOnlyExpression) */
  get: (key: string) => StringReadOnlyExpression,
  /** Existence check: returns a ConditionReadOnlyExpression */
  has: (key: string) => ConditionReadOnlyExpression<string>,
}