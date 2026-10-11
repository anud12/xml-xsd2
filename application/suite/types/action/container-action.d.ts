import { ConditionExpression } from "../primitives/conditionExpression";
import { TemporalExpression } from "../primitives/temporalExpression";
import { ActionContext } from "./common";

/**
 * Target for container actions: single container ID
 */
export type ContainerActionTarget = {
  type: "container";
  id: string;
};

/**
 * Arguments for setting a container action via hostApi.setContainerAction
 */
export type SetContainerActionArgs = {
  name: string;
  description?: string;
  cooldownGroup?: string;
  guard?: (context: ActionContext, target: ContainerActionTarget) => ConditionExpression;
  cooldown?: (context: ActionContext, target: ContainerActionTarget) => TemporalExpression;
  apply: (context: ActionContext, target: ContainerActionTarget) => void;
};

/**
 * Type for setContainerAction function.
 */
export type SetContainerActionFunction = (args: SetContainerActionArgs) => void;
