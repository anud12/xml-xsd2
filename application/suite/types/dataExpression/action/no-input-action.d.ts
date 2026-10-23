import { ConditionExpression } from "../../primitives/conditionExpression";
import { TemporalExpression } from "../../primitives/temporalExpression";
import { StringExpression } from "../../primitives/stringExpression";
import { Expression } from "../../Expression";
import { ActionContext } from "./common";

/**
 * Arguments for setting a no-input action via hostApi.setAction
 */
export type SetActionArgs = {
  name: StringExpression;
  description?: string;
  cooldownGroup?: string;
  guard?: (context: ActionContext) => ConditionExpression;
  cooldown?: (context: ActionContext) => TemporalExpression;
  apply: (context: ActionContext) => void;
};

/**
 * A read-only reference to a registered action, returned by
 * `hostApi.runtime.setAction`. Use it anywhere an action is referenced.
 */
export type ActionExpression = Expression & {
  /** The action's name. */
  name: string;
};

/**
 * Type for setAction function (no-input actions).
 */
export type SetActionFunction = (args: SetActionArgs) => ActionExpression;
