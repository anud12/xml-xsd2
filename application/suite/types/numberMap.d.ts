import {NumberExpression, MutableNumberExpression} from "./primitives/numberExpression";
import {ConditionExpression} from "./primitives/conditionExpression";

/**
 * Read-only map of named number values. Values are plain NumberExpressions that
 * do not write back to the owning entity.
 */
export type NumberMap = {
  [name: string]: NumberExpression,
}

/**
 * Mutable map of named number values. Values are MutableNumberExpressions whose
 * mutations (set/sum/...) write back to the owning entity.
 */
export type MutableNumberMap = {
  [name: string]: MutableNumberExpression,
}

export type NumberMapApi = {
  /** Create an empty mutable number map. */
  create: () => MutableNumberMap,
  type: unknown,
}
