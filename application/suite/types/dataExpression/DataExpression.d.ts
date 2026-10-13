import { EntityExpression } from "./Entity";
import { ContainerExpression, GridExpression } from "./Contaier";
import { BehaviorExpression } from "./behavior";
import { ActionExpression } from "./action/no-input-action";
import { EffectExpression } from "./Effect";
import { AnimationExpression } from "./animation/AnimationRegistration";

/**
 * A read-only reference to any declared module data structure. Unites all
 * cross-structure "edge" handles so a single type can name a target of any
 * kind (entity, container, grid, behavior, action, effect, or animation).
 */
export type DataExpression =
  | EntityExpression
  | ContainerExpression
  | GridExpression
  | BehaviorExpression
  | ActionExpression
  | EffectExpression
  | AnimationExpression;
