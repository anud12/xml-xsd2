import { TemporalReadOnlyExpression } from "../../primitives/temporalExpression";
import { NumberReadOnlyExpression } from "../../primitives/numberExpression";
import { EntityReadOnlyExpression } from "../Entity";
import { ContainerReadOnlyExpression } from "../Contaier";
import { EffectReadOnlyExpression } from "../Effect";

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
  | { emit: { effect: EffectReadOnlyExpression; payload: Record<string, any> } }
  | { wait: TemporalReadOnlyExpression }
  | {
      move: {
        containerId: ContainerReadOnlyExpression;
        entityId: EntityReadOnlyExpression;
        x: NumberReadOnlyExpression;
        y: NumberReadOnlyExpression;
        speed: NumberReadOnlyExpression;
        // Walker-mutated at walk time (not set by module code). The move is
        // advanced by integer Bresenham stepping in Q16.16 fixed-point: each
        // axis pools `delta * speed` per tick into a signed remainder and
        // releases whole GTUs as the remainder crosses the distance scale.
        startX?: number;
        startY?: number;
        deltaX?: number;
        deltaY?: number;
        dist?: number;
        remX?: number;
        remY?: number;
        posX?: number;
        posY?: number;
        lastTick?: number;
      }
    };
