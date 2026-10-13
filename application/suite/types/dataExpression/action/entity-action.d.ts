import { ConditionExpression } from "../../primitives/conditionExpression";
import { TemporalExpression } from "../../primitives/temporalExpression";
import { StringExpression } from "../../primitives/stringExpression";
import { EntityExpression } from "../Entity";
import { ActionContext } from "./common";

/**
 * Target for entity actions: single entity ID
 */
export type EntityActionTarget = {
  type: "entity";
  /** The entity the action is applied to (an {@link EntityExpression} reference). */
  id: EntityExpression;
};

/**
 * Arguments for setting an entity action via hostApi.setEntityAction
 */
export type SetEntityActionArgs = {
  name: StringExpression;
  description?: string;
  cooldownGroup?: string;
  guard?: (context: ActionContext, target: EntityActionTarget) => ConditionExpression;
  cooldown?: (context: ActionContext, target: EntityActionTarget) => TemporalExpression;
  apply: (context: ActionContext, target: EntityActionTarget) => void;
};

/**
 * Type for setEntityAction function.
 */
export type SetEntityActionFunction = (args: SetEntityActionArgs) => void;
