import { StringExpression } from "./primitives/stringExpression";
import { NumberExpression } from "./primitives/numberExpression";
import { ConditionExpression } from "./primitives/conditionExpression";
import { ContainerExpression } from "./Contaier";

/**
 * Object-literal entity filter; all fields optional, AND-combined.
 * Pass the literal directly to `getEntityBy` — no factory needed.
 */
export type EntityFilter = {
  /** Match the entity id with a predicate (AND of multiple ids). */
  id?: (id: StringExpression) => ConditionExpression;
  /** Match entities that have the given classification string. */
  classification?: StringExpression;
  /** Match where a text_map entry for `key` satisfies `fn`. */
  text?: { key: StringExpression; where: (value: StringExpression) => ConditionExpression }[];
  /** Match where a number_map entry for `key` satisfies `fn`. */
  number?: { key: StringExpression; where: (value: NumberExpression) => ConditionExpression }[];
  /** Match entities that are members of the given containers. */
  container?: ContainerExpression[];
}
