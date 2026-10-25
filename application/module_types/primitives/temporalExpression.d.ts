import { Expression } from "../Expression";

/**
 * A lazy in-game duration expression.
 * 
 * TemporalReadOnlyExpressions represent durations in module-defined units
 * (e.g., "round", "day") mapped to an internal GTU (game time unit) clock.
 * Used for cooldowns and effect reoccurrence scheduling.
 */
export type TemporalReadOnlyExpression = Expression & (
  {
    /**
     * Evaluate to a fixed number of ticks in the GTU clock.
     * Used for testing and simple fixed delays.
     */
    type: 'ticks';
    ticks: number;
  } | {
    /**
     * Evaluate to a named in-game unit (e.g., "round", "day").
     * The unit must be registered via hostApi.temporal.defineUnit().
     */
    type: 'unit';
    unit: string;
    count: number;
  }
);

/**
 * Factory API for creating TemporalReadOnlyExpressions.
 */
export type TemporalExpressionApi = {
  /**
   * Create a temporal expression from a fixed number of ticks.
   */
  ofTicks: (ticks: number) => TemporalReadOnlyExpression;

  /**
   * Create a temporal expression from a named unit and count.
   */
  of: (unit: string, count: number) => TemporalReadOnlyExpression;

  /**
   * Define a named in-game unit and its GTU multiplier.
   * 
   * Example: defineUnit('round', 10) means 1 round = 10 GTU.
   */
  defineUnit: (unitName: string, gtuMultiplier: number) => TemporalExpressionApi;

  /**
   * Register a temporal expression under a named rule for later getRule(ref) lookups.
   */
  asRule: (ruleName: string, expr: TemporalReadOnlyExpression) => TemporalExpressionApi;

  /**
   * Retrieve a previously registered temporal expression by rule name.
   */
  getRule: (ruleName: string) => TemporalReadOnlyExpression;

  /**
   * Marker for HostApi surfaces.
   */
  type: unknown;
};
