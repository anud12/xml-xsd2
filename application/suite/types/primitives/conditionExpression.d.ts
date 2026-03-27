export type ConditionExpression = {
  /** Short-circuiting combinators. Immutable. */
  and: (other: ConditionExpression) => ConditionExpression;
  or:  (other: ConditionExpression) => ConditionExpression;

  /** Logical inversion. */
  negate: () => ConditionExpression;

  /** Convenience combinators that accept callbacks producing ConditionExpression values lazily. */
  ifTrue:  (cb: () => ConditionExpression) => ConditionExpression; // invoke cb only when receiver is true
  ifFalse: (cb: () => ConditionExpression) => ConditionExpression; // invoke cb only when receiver is false
  /** Marker for HostApi surfaces */
};

export type ConditionExpressionApi = {
  /** Factory function */
  of: (value: boolean) => ConditionExpression;

  /** Register and retrieve named condition rules. */
  asRule: (ruleName: string, expr: ConditionExpression) => ConditionExpressionApi;
  getRule: (ruleName: string) => ConditionExpression;

  /** Marker for HostApi surfaces */
  type: unknown;
};