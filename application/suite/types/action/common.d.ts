import { ConditionExpression } from "../primitives/conditionExpression";
import { TemporalExpression } from "../primitives/temporalExpression";
import { EntityExpression } from "../Entity";
import { NumberExpression } from "../primitives/numberExpression";

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
};
