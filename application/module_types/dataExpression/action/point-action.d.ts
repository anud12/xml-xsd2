import { Expression } from "../../Expression";
import { TemporalReadOnlyExpression } from "../../primitives/temporalExpression";
import { StringReadOnlyExpression } from "../../primitives/stringExpression";
import { EntityReadOnlyExpression } from "../Entity";
import { ActionContext, ActionGuardResult, GroupPoint } from "./common";

/**
 * Target for point actions: container ID and 1D/2D position
 */
export type PointActionTarget = {
  type: "point";
  /** The container the point is in (a {@link EntityReadOnlyExpression} reference). */
  groupId: EntityReadOnlyExpression;
  position: GroupPoint;
};

/**
 * Arguments for setting a point action via hostApi.setPointAction
 */
export type SetPointActionArgs = {
  name: StringReadOnlyExpression;
  description?: string;
  cooldownGroup?: string;
  guard?: (context: ActionContext, target: PointActionTarget) => ActionGuardResult;
  cooldown?: (context: ActionContext, target: PointActionTarget) => TemporalReadOnlyExpression;
  apply: (context: ActionContext, target: PointActionTarget) => void;
};

/**
 * Type for setPointAction function.
 */
export type SetPointActionFunction = (args: SetPointActionArgs) => void;
