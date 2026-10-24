import { TemporalReadOnlyExpression } from "../../primitives/temporalExpression";
import { EntityReadOnlyExpression } from "../Entity";
import { ContainerReadOnlyExpression } from "../Contaier";
import { NumberReadOnlyExpression } from "../../primitives/numberExpression";
import { EffectReadOnlyExpression } from "../Effect";
import { ActionReadOnlyExpression } from "./no-input-action";
import { EntityFilter } from "../../EntityFilter";
import { ListReadOnlyExpression } from "../../primitives/ListExpression";

/**
 * 1D or 2D position within a container.
 */
export type ContainerPoint =
  | { dimension1: NumberReadOnlyExpression }
  | { dimension1: NumberReadOnlyExpression; dimension2: NumberReadOnlyExpression };

/**
 * Execution context available to guard, cooldown, and apply callbacks.
 */
export type ActionContext = {
  /** The entity performing the action. */
  actor: EntityReadOnlyExpression;
  /** Raw arguments the action was invoked with (empty for no-input actions). */
  args: Record<string, any>;
  /** Emit a registered effect (a {@link EffectReadOnlyExpression} reference) with arbitrary input. */
  emitEffect: (effect: EffectReadOnlyExpression, input: Record<string, any>) => any;
  /** Query entities matching a filter. */
  getEntityBy: (entityFilter: EntityFilter) => ListReadOnlyExpression<EntityReadOnlyExpression>;
  /** Schedule the actor to idle for a temporal duration before resuming. */
  wait: (duration: TemporalReadOnlyExpression) => void;
  /** Teleport an entity to a cell in a container. */
  teleportTo: (target: {
    containerId: ContainerReadOnlyExpression;
    entityId: EntityReadOnlyExpression;
    x: any;
    y: any;
    clamp?: boolean;
  }) => void;
  /** Begin a per-tick movement plan toward a target cell (advances by speed per tick). */
  moveTo: (target: {
    containerId: ContainerReadOnlyExpression;
    entityId: EntityReadOnlyExpression;
    x: any;
    y: any;
    speed: any;
  }) => void;
  /** Trigger another registered action (an {@link ActionReadOnlyExpression} reference). */
  action?: (action: ActionReadOnlyExpression) => any;
};
