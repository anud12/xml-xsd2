export type ConditionExpression = {
  /** Short-circuiting combinators. Immutable. */
  and: (other: ConditionExpression) => ConditionExpression;
  or:  (other: ConditionExpression) => ConditionExpression;

  /** Logical inversion. */
  negate: () => ConditionExpression;

  /** Convenience combinators that accept callbacks producing ConditionExpression values lazily. Returns self. */
  ifTrue:  (cb: () => void) => ConditionExpression; // invoke cb only when receiver is true
  ifFalse: (cb: () => void) => ConditionExpression; // invoke cb only when receiver is false
  /** Marker for HostApi surfaces */
} & {
  
};

export type MutableConditionExpression = ConditionExpression & {
  /** Overwrite the condition value in place. Returns self. */
  set: (value: boolean) => MutableConditionExpression;
}

export type ConditionExpressionApi = {
  /** Factory function */
  of: (value: boolean) => MutableConditionExpression;

  /** Marker for HostApi surfaces */
  type: unknown;
};
