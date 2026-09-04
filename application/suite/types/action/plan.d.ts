import { TemporalExpression } from "../primitives/temporalExpression";

/**
 * A single recorded step of an action plan.
 *
 * An action's `apply` is a declaration phase: it records steps, and the
 * runtime walks the plan across ticks. `emit` steps fire the named event
 * (payload expressions evaluate when the walker reaches the step); `wait`
 * steps park the plan until the duration of in-game time has elapsed.
 * A plan without `wait` steps walks to completion in the dispatch tick —
 * the action is instant.
 */
export type ActionPlanStep =
  | { emit: { eventName: string; payload: Record<string, any> } }
  | { wait: TemporalExpression };
