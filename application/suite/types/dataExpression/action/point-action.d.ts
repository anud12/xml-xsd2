import { ConditionExpression } from "../../primitives/conditionExpression";
import { TemporalExpression } from "../../primitives/temporalExpression";
import { StringExpression } from "../../primitives/stringExpression";
import { ContainerExpression } from "../Contaier";
import { ActionContext, ContainerPoint } from "./common";

/**
 * Target for point actions: container ID and 1D/2D position
 */
export type PointActionTarget = {
  type: "point";
  /** The container the point is in (a {@link ContainerExpression} reference). */
  containerId: ContainerExpression;
  position: ContainerPoint;
};

/**
 * Arguments for setting a point action via hostApi.setPointAction
 */
export type SetPointActionArgs = {
  name: StringExpression;
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
