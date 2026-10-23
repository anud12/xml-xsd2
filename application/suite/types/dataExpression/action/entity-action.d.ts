import { ConditionReadOnlyExpression } from "../../primitives/conditionExpression";
import { TemporalReadOnlyExpression } from "../../primitives/temporalExpression";
import { StringReadOnlyExpression } from "../../primitives/stringExpression";
import { EntityReadOnlyExpression } from "../Entity";
import { ActionContext } from "./common";

/**
 * Target for entity actions: single entity ID
 */
export type EntityActionTarget = {
  type: "entity";
  /** The entity the action is applied to (an {@link EntityReadOnlyExpression} reference). */
  id: EntityReadOnlyExpression;
};

/**
 * Arguments for setting an entity action via hostApi.setEntityAction
 */
export type SetEntityActionArgs = {
  name: StringReadOnlyExpression;
  description?: string;
  cooldownGroup?: string;
  guard?: (context: ActionContext, target: EntityActionTarget) => ConditionReadOnlyExpression;
  cooldown?: (context: ActionContext, target: EntityActionTarget) => TemporalReadOnlyExpression;
  apply: (context: ActionContext, target: EntityActionTarget) => void;
};

/**
 * Type for setEntityAction function.
 */
export type SetEntityActionFunction = (args: SetEntityActionArgs) => void;
