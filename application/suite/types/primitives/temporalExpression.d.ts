/**
 * A lazy in-game duration expression.
 * 
 * TemporalExpressions represent durations in module-defined units
 * (e.g., "round", "day") mapped to an internal GTU (game time unit) clock.
 * Used for cooldowns and effect reoccurrence scheduling.
 */
export type TemporalExpression = {
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
};

/**
 * Factory API for creating TemporalExpressions.
 */
export type TemporalExpressionApi = {
  /**
   * Create a temporal expression from a fixed number of ticks.
   */
  ofTicks: (ticks: number) => TemporalExpression;

  /**
   * Create a temporal expression from a named unit and count.
   */
  of: (unit: string, count: number) => TemporalExpression;

  /**
   * Define a named in-game unit and its GTU multiplier.
   * 
   * Example: defineUnit('round', 10) means 1 round = 10 GTU.
   */
  defineUnit: (unitName: string, gtuMultiplier: number) => TemporalExpressionApi;

  /**
   * Marker for HostApi surfaces.
   */
  type: unknown;
};
