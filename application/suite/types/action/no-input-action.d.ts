import { ConditionExpression } from "../primitives/conditionExpression";
import { TemporalExpression } from "../primitives/temporalExpression";
import { StringExpression } from "../primitives/stringExpression";
import { ActionContext } from "./common";

/**
 * A reference to a registered action.
 */
export type ActionReference = {
  /** The name expression used to register this action. */
  readonly name: StringExpression;
};

/**
 * Arguments for registering a no-input action via hostApi.registerAction
 */
export type RegisterActionArgs = {
  name: StringExpression;
  description?: string;
  cooldownGroup?: string;
  guard?: (context: ActionContext) => ConditionExpression;
  cooldown?: (context: ActionContext) => TemporalExpression;
  apply: (context: ActionContext) => void;
};

/**
 * Type for registerAction function (no-input actions).
 */
export type RegisterActionFunction = (args: RegisterActionArgs) => ActionReference;
