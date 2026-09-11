import { ConditionExpression } from "../primitives/conditionExpression";
import { TemporalExpression } from "../primitives/temporalExpression";
import { EntityExpression } from "../Entity";
import { NumberExpression } from "../primitives/numberExpression";
import { EventContext } from "../Effect";

/**
 * 1D or 2D position within a container.
 */
export type ContainerPoint =
  | { dimension1: NumberExpression }
  | { dimension1: NumberExpression; dimension2: NumberExpression };

/**
 * Execution context available to guard, cooldown, and apply callbacks.
 */
export type ActionContext = {
  actor: EntityExpression;
  /** Raw arguments the action was invoked with (empty for no-input actions). */
  args: Record<string, any>;
  /** Emit a named effect with arbitrary input. */
  emitEffect: (eventName: string, input: Record<string, any>) => any;
  /** Schedule the actor to idle for a temporal duration before resuming. */
  wait: (duration: TemporalExpression) => void;
  /** Teleport an entity to a cell in a container. */
  teleportTo: (target: {
    containerId: any;
    entityId: any;
    x: any;
    y: any;
    clamp?: boolean;
  }) => void;
  /** Begin a per-tick movement plan toward a target cell (advances by speed per tick). */
  moveTo: (target: {
    containerId: any;
    entityId: any;
    x: any;
    y: any;
    speed: any;
  }) => void;
  /** Trigger another registered action by name. */
  action?: (name: any) => any;
  [key: string]: any;
};
