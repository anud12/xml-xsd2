import { ConditionExpression } from "../primitives/conditionExpression";
import { TemporalExpression } from "../primitives/temporalExpression";
import { EntityExpression } from "../Entity";
import { NumberExpression } from "../primitives/numberExpression";
import { StringExpression } from "../primitives/stringExpression";

/**
 * 1D or 2D position within a container.
 */
export type ContainerPoint =
  | { dimension1: NumberExpression }
  | { dimension1: NumberExpression; dimension2: NumberExpression };

/**
 * Arguments for `ActionContext.moveTo`.
 */
export type MoveToArgs = {
  containerId: StringExpression;
  entityId: StringExpression;
  x: NumberExpression;
  y: NumberExpression;
  /**
   * Amount of path length covered per tick. May be a constant
   * (`hostApi.number.of(2)`) or read from the entity's number map
   * (`ctx.actor.numberMap().get("moveSpeed")`). Re-resolved each tick.
   */
  speed: NumberExpression;
};

/**
 * Arguments for `ActionContext.teleportTo`.
 */
export type TeleportToArgs = {
  containerId: StringExpression;
  entityId: StringExpression;
  x: NumberExpression;
  y: NumberExpression;
  /**
   * When true, the destination is clamped into the container's
   * `sizeX`/`sizeY` bounds (and `[0, bound]`). When false, the entity is
   * placed at the exact (x, y) even if it lies outside the container.
   */
  clamp?: boolean;
};

/**
 * Execution context available to guard, cooldown, and apply callbacks.
 */
export type ActionContext = {
  actor: EntityExpression;
  /**
   * The args payload the action was emitted with (e.g. by a panel's
   * `onClick` plan). Defaults to `{}` when none was supplied.
   */
  args: Record<string, any>;
  emitEffect: (eventName: string, input: Record<string, any>) => any;
  /**
   * Records a suspension step: the action plan parks here until `duration`
   * of in-game time has elapsed, then continues with the next recorded step.
   *
   * `apply` is a declaration phase — calls to `emitEffect` and `wait` only
   * record plan steps; nothing executes until the runtime walks the plan.
   * Payload expressions are evaluated when the walker reaches the step.
   */
  wait: (duration: TemporalExpression) => void;
  /**
   * Records a movement step: the actor travels in a straight line (8-directional)
   * from its current position to (x, y) within `containerId`, advancing `speed`
   * of path per tick. The path length is axis-aligned distance for straight
   * moves and floor(√2 * min(|dx|,|dy|)) for diagonal moves.
   *
   * `apply` is a declaration phase — this records a plan step; the runtime walks
   * it across ticks. `speed` is a NumberExpression (constant or from the entity's
   * number map) and is re-resolved each tick. The move is interruptible by
   * default: a new action for the same actor aborts the move (the actor stays at
   * its current cell) and runs in its place. Call denyInterrupt() before moveTo
   * to make the move uninterruptible (a new action is rejected instead).
   */
  moveTo: (args: MoveToArgs) => void;
  /**
   * Instantly relocates an entity within its container to (x, y), writing the
   * position into the container's `getX`/`getY` number-map keys and rebaking the
   * container. Unlike `moveTo` this mutates immediately (no plan step) and does
   * not span ticks.
   *
   * `clamp` (default false) clamps the destination into the container's
   * `sizeX`/`sizeY` bounds before writing.
   */
  teleportTo: (args: TeleportToArgs) => void;
};
