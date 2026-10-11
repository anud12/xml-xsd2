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
 * Arguments for setting a point action via hostApi.setPointAction
 */
export type SetPointActionArgs = {
  name: string;
  description?: string;
  cooldownGroup?: string;
  guard?: (context: ActionContext, target: PointActionTarget) => ConditionExpression;
  cooldown?: (context: ActionContext, target: PointActionTarget) => TemporalExpression;
  apply: (context: ActionContext, target: PointActionTarget) => void;
};

/**
 * Type for setPointAction function.
 */
export type SetPointActionFunction = (args: SetPointActionArgs) => void;
