import { Expression } from "../../Expression";
import { ConditionReadOnlyExpression } from "../../primitives/conditionExpression";
import { TemporalReadOnlyExpression } from "../../primitives/temporalExpression";
import { StringReadOnlyExpression } from "../../primitives/stringExpression";
import { ContainerReadOnlyExpression } from "../Contaier";
import { ActionContext } from "./common";

/**
 * Target for container actions: single container ID
 */
export type ContainerActionTarget = {
  type: "container";
  /** The container the action is applied to (a {@link ContainerReadOnlyExpression} reference). */
  id: ContainerReadOnlyExpression;
};

/**
 * Arguments for setting a container action via hostApi.setContainerAction
 */
export type SetContainerActionArgs = {
  name: StringReadOnlyExpression;
  description?: string;
  cooldownGroup?: string;
  guard?: (context: ActionContext, target: ContainerActionTarget) => ConditionReadOnlyExpression<Expression>;
  cooldown?: (context: ActionContext, target: ContainerActionTarget) => TemporalReadOnlyExpression;
  apply: (context: ActionContext, target: ContainerActionTarget) => void;
};

/**
 * Type for setContainerAction function.
 */
export type SetContainerActionFunction = (args: SetContainerActionArgs) => void;
