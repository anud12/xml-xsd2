import { ConditionExpression } from "../primitives/conditionExpression";
import { TemporalExpression } from "../primitives/temporalExpression";
import { ActionContext } from "./common";

/**
 * Target for entity actions: single entity ID
 */
export type EntityActionTarget = {
  type: "entity";
  id: string;
};

/**
 * Arguments for setting an entity action via hostApi.setEntityAction
 */
export type SetEntityActionArgs = {
  name: string;
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
