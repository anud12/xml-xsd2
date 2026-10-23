import { ConditionReadOnlyExpression } from "../../primitives/conditionExpression";
import { TemporalReadOnlyExpression } from "../../primitives/temporalExpression";
import { StringReadOnlyExpression } from "../../primitives/stringExpression";
import { ContainerReadOnlyExpression } from "../Contaier";
import { ActionContext, ContainerPoint } from "./common";

/**
 * Target for point actions: container ID and 1D/2D position
 */
export type PointActionTarget = {
  type: "point";
  /** The container the point is in (a {@link ContainerReadOnlyExpression} reference). */
  containerId: ContainerReadOnlyExpression;
  position: ContainerPoint;
};

/**
 * Arguments for setting a point action via hostApi.setPointAction
 */
export type SetPointActionArgs = {
  name: StringReadOnlyExpression;
  description?: string;
  cooldownGroup?: string;
  guard?: (context: ActionContext, target: PointActionTarget) => ConditionReadOnlyExpression;
  cooldown?: (context: ActionContext, target: PointActionTarget) => TemporalReadOnlyExpression;
  apply: (context: ActionContext, target: PointActionTarget) => void;
};

/**
 * Type for setPointAction function.
 */
export type SetPointActionFunction = (args: SetPointActionArgs) => void;
