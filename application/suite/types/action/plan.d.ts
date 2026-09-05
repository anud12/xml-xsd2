import { TemporalExpression } from "../primitives/temporalExpression";
import { NumberExpression } from "../primitives/numberExpression";
import { StringExpression } from "../primitives/stringExpression";

/**
 * A single recorded step of an action plan.
 *
 * An action's `apply` is a declaration phase: it records steps, and the
 * runtime walks the plan across ticks. `emit` steps fire the named event
 * (payload expressions evaluate when the walker reaches the step); `wait`
 * steps park the plan until the duration of in-game time has elapsed; `move`
 * steps walk the actor along a straight line across ticks. A plan without
 * `wait`/`move` steps walks to completion in the dispatch tick — the action
 * is instant.
 */
export type ActionPlanStep =
  | { emit: { eventName: string; payload: Record<string, any> } }
  | { wait: TemporalExpression }
  | {
      move: {
        containerId: StringExpression;
        entityId: StringExpression;
        x: NumberExpression;
        y: NumberExpression;
        speed: NumberExpression;
        // Walker-mutated at walk time (not set by module code):
        remainingLength?: number;
        start?: { x: number; y: number };
      }
    };
