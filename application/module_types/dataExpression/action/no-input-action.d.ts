import { ConditionReadOnlyExpression } from "../../primitives/conditionExpression";
import { TemporalReadOnlyExpression } from "../../primitives/temporalExpression";
import { StringReadOnlyExpression } from "../../primitives/stringExpression";
import { Expression } from "../../Expression";
import { ActionContext } from "./common";

/**
 * Arguments for setting a no-input action via hostApi.setAction
 */
export type SetActionArgs = {
  name: StringReadOnlyExpression;
  description?: string;
  cooldownGroup?: string;
  guard?: (context: ActionContext) => ConditionReadOnlyExpression<Expression>;
  cooldown?: (context: ActionContext) => TemporalReadOnlyExpression;
  apply: (context: ActionContext) => void;
};

/**
 * A read-only reference to a registered action, returned by
 * `hostApi.runtime.setAction`. Use it anywhere an action is referenced.
 */
export type ActionReadOnlyExpression = Expression & {
  /** The action's name. */
  name: string;
};

/**
 * Type for setAction function (no-input actions).
 */
export type SetActionFunction = (args: SetActionArgs) => ActionReadOnlyExpression;
