import { ConditionExpression } from "./primitives/conditionExpression";
import { NumberExpression } from "./primitives/numberExpression";
import { StringExpression } from "./primitives/stringExpression";

/**
 * Level 0 script builder — a linear sequence of atomic steps.
 */
export type ScriptBuilder = {
  /** Wait for a duration (in game time units). */
  wait: (duration: NumberExpression) => ScriptBuilder;
  /** Execute a named action. */
  executeAction: (action: StringExpression) => ScriptBuilder;
  /** Navigate to a position. */
  navigateTo: (x: NumberExpression, y: NumberExpression) => ScriptBuilder;
  /** Transition to a named reaction state. */
  goto: (stateName: string) => ScriptBuilder;
  /** Return to the idle state. */
  gotoIdle: () => ScriptBuilder;
};

/**
 * Chain builder for appending reactions to the idle state.
 */
export type ReactionChainBuilder = {
  /**
   * Append a reaction state: when `condition` is true, execute the script body,
   * then transition as the script dictates via `goto` / `gotoIdle`.
   *
   * @param name      - Unique name for this reaction state.
   * @param condition - ConditionExpression gating the transition into this state.
   * @param fn        - Callback receiving the level 0 script builder.
   */
  reaction: (
    name: string,
    condition: ConditionExpression,
    fn: (script: ScriptBuilder) => void
  ) => ReactionChainBuilder;
};

/**
 * Top-level autonomy builder for a reactive state machine.
 */
export type AutonomyBuilder = {
  /**
   * Define the initial idle state with a level 0 script body.
   * Returns a chain builder for appending reactions.
   *
   * @param fn - Callback receiving the level 0 script builder.
   */
  idle: (fn: (script: ScriptBuilder) => void) => ReactionChainBuilder;
};

/**
 * API for registering autonomy (reactive state machines) on entities.
 *
 * Exposed as `hostApi.runtime.setAutonomy` inside module scripts.
 */
export type AutonomyApi = {
  /**
   * Register a reactive autonomy state machine for an entity.
   *
   * @param entityName - Name or ID of the entity to attach autonomy to.
   * @param fn         - Callback receiving the autonomy builder.
   */
  setAutonomy: (
    entityName: string,
    fn: (builder: AutonomyBuilder) => void
  ) => void;
};
