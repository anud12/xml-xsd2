import { TemporalExpression } from "../../primitives/temporalExpression";
import { EntityExpression } from "../Entity";
import { ContainerExpression } from "../Contaier";
import { NumberExpression } from "../../primitives/numberExpression";
import { EffectExpression } from "../Effect";
import { ActionExpression } from "./no-input-action";

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
  /** The entity performing the action. */
  actor: EntityExpression;
  /** Raw arguments the action was invoked with (empty for no-input actions). */
  args: Record<string, any>;
  /** Emit a registered effect (a {@link EffectExpression} reference) with arbitrary input. */
  emitEffect: (effect: EffectExpression, input: Record<string, any>) => any;
  /** Schedule the actor to idle for a temporal duration before resuming. */
  wait: (duration: TemporalExpression) => void;
  /** Teleport an entity to a cell in a container. */
  teleportTo: (target: {
    containerId: ContainerExpression;
    entityId: EntityExpression;
    x: any;
    y: any;
    clamp?: boolean;
  }) => void;
  /** Begin a per-tick movement plan toward a target cell (advances by speed per tick). */
  moveTo: (target: {
    containerId: ContainerExpression;
    entityId: EntityExpression;
    x: any;
    y: any;
    speed: any;
  }) => void;
  /** Trigger another registered action (an {@link ActionExpression} reference). */
  action?: (action: ActionExpression) => any;
  [key: string]: any;
};
