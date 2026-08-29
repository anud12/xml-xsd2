import { ConditionExpression } from "../primitives/conditionExpression";
import { TemporalExpression } from "../primitives/temporalExpression";
import { StringExpression } from "../primitives/stringExpression";
import { ActionContext } from "./common";

/**
 * Arguments for registering a no-input action via hostApi.registerAction
 */
export type RegisterActionArgs = {
  name: string | StringExpression;
  description?: string;
  cooldownGroup?: string;
  guard?: (context: ActionContext) => ConditionExpression;
  cooldown?: (context: ActionContext) => TemporalExpression;
  apply: (context: ActionContext) => void;
};

/**
 * Reference to a registered action.
 */
export type ActionReference = {
  name: StringExpression;
};

/**
 * Type for registerAction function (no-input actions).
 */
export type RegisterActionFunction = (args: RegisterActionArgs) => ActionReference;
