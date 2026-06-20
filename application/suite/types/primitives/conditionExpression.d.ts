import {MaybeExpression} from "./maybeExpression";

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

export type ConditionExpressionApi = {
  /** Factory function */
  of: (value: boolean) => ConditionExpression;

  /** Register and retrieve named condition rules. */
  asRule: (ruleName: string, expr: ConditionExpression) => ConditionExpressionApi;
  getRule: (ruleName: string) => ConditionExpression;

  /** Marker for HostApi surfaces */
  type: unknown;
};