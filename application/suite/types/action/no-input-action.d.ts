import { ConditionExpression } from "../primitives/conditionExpression";
import { TemporalExpression } from "../primitives/temporalExpression";
import { ActionContext } from "./common";

/**
 * Arguments for registering a no-input action via hostApi.registerAction
 */
export type RegisterActionArgs = {
  name: string | any;
  description?: string;
  cooldownGroup?: string;
  guard?: (context: ActionContext) => ConditionExpression;
  cooldown?: (context: ActionContext) => TemporalExpression;
  apply: (context: ActionContext) => void;
};

/** A registered action handle: its name plus optional metadata. */
export type RegisteredAction = {
  name: string;
  [key: string]: any;
};

/**
 * Type for registerAction function (no-input actions).
 */
export type RegisterActionFunction = (args: RegisterActionArgs) => RegisteredAction;
