import { EntityReadOnlyExpression } from "./Entity";
import { ContainerReadOnlyExpression, GridReadOnlyExpression } from "./Contaier";
import { BehaviorReadOnlyExpression } from "./behavior";
import { ActionReadOnlyExpression } from "./action/no-input-action";
import { EffectReadOnlyExpression } from "./Effect";
import { AnimationReadOnlyExpression } from "./animation/AnimationRegistration";

/**
 * A read-only reference to any declared module data structure. Unites all
 * cross-structure "edge" handles so a single type can name a target of any
 * kind (entity, container, grid, behavior, action, effect, or animation).
 */
export type DataExpression =
  | EntityReadOnlyExpression
  | ContainerReadOnlyExpression
  | GridReadOnlyExpression
  | BehaviorReadOnlyExpression
  | ActionReadOnlyExpression
  | EffectReadOnlyExpression<unknown, unknown>
  | AnimationReadOnlyExpression;
