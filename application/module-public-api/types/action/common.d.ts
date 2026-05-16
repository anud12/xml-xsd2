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
  emitEffect: (eventName: string, input: Record<string, any>) => any;
};
