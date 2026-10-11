import { ConditionExpression } from "../primitives/conditionExpression";
import { TemporalExpression } from "../primitives/temporalExpression";
import { ActionContext } from "./common";

/**
 * Arguments for setting a no-input action via hostApi.setAction
 */
export type SetActionArgs = {
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
 * Type for setAction function (no-input actions).
 */
export type SetActionFunction = (args: SetActionArgs) => RegisteredAction;
