import {StringExpression, MutableStringExpression} from "./primitives/stringExpression";
import {ConditionExpression} from "./primitives/conditionExpression";

/**
 * Read-only map of named text values. Values are plain StringExpressions that
 * do not write back to the owning entity.
 */
export type TextMap = {
  [name: string]: StringExpression,
}

/**
 * Mutable map of named text values. Values are MutableStringExpressions whose
 * mutations (set/concat/...) write back to the owning entity.
 */
export type MutableTextMap = {
  [name: string]: MutableStringExpression,
}

export type TextMapApi = {
  /** Create an empty mutable text map. */
  create: () => MutableTextMap,
  type: unknown,
}
