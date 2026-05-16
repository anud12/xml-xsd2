import {StringExpression} from "./primitives/stringExpression";
import {ConditionExpression} from "./primitives/conditionExpression";

export type TextMap = {
  [name:string]: StringExpression, //colection of `StringExpression` values accesible by `name`.
}

export type TextMapExpressionApi = {
  create: () => TextMapExpression,
}

export type TextMapExpression = {
  /** Insert or replace a key's value with a StringExpression */
  put: (key: string, value: StringExpression) => TextMapExpression,
  /** Remove a key (optional) */
  remove?: (key: string) => TextMapExpression,
  /** Retrieve the value expression for a key (missing keys may produce an empty StringExpression) */
  get: (key: string) => StringExpression,
  /** Existence check: returns a ConditionExpression */
  has: (key: string) => ConditionExpression,
  /** Equality check: compare stored value to provided StringExpression */
  equals: (key: string, value: StringExpression) => ConditionExpression,
}