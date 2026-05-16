import { ConditionExpression } from "../primitives/conditionExpression";
import { TemporalExpression } from "../primitives/temporalExpression";
import { ActionContext, ContainerPoint } from "./common";

/**
 * Target for point actions: container ID and 1D/2D position
 */
export type PointActionTarget = {
  type: "point";
  containerId: string;
  position: ContainerPoint;
};

/**
 * Arguments for registering a point action via hostApi.registerPointAction
 */
export type RegisterPointActionArgs = {
  name: string;
  description?: string;
  cooldownGroup?: string;
  guard?: (context: ActionContext, target: PointActionTarget) => ConditionExpression;
  cooldown?: (context: ActionContext, target: PointActionTarget) => TemporalExpression;
  apply: (context: ActionContext, target: PointActionTarget) => void;
};

/**
 * Type for registerPointAction function.
 */
export type RegisterPointActionFunction = (args: RegisterPointActionArgs) => void;
