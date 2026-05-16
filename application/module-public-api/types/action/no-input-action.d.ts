import { ConditionExpression } from "../primitives/conditionExpression";
import { TemporalExpression } from "../primitives/temporalExpression";
import { ActionContext } from "./common";

/**
 * Arguments for registering a no-input action via hostApi.registerAction
 */
export type RegisterActionArgs = {
  name: string;
  description?: string;
  cooldownGroup?: string;
  guard?: (context: ActionContext) => ConditionExpression;
  cooldown?: (context: ActionContext) => TemporalExpression;
  apply: (context: ActionContext) => void;
};

/**
 * Type for registerAction function (no-input actions).
 */
export type RegisterActionFunction = (args: RegisterActionArgs) => void;
