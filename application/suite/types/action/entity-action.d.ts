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
 * Arguments for registering an entity action via hostApi.registerEntityAction
 */
export type RegisterEntityActionArgs = {
  name: string;
  description?: string;
  cooldownGroup?: string;
  guard?: (context: ActionContext, target: EntityActionTarget) => ConditionExpression;
  cooldown?: (context: ActionContext, target: EntityActionTarget) => TemporalExpression;
  apply: (context: ActionContext, target: EntityActionTarget) => void;
};

/**
 * Type for registerEntityAction function.
 */
export type RegisterEntityActionFunction = (args: RegisterEntityActionArgs) => void;
