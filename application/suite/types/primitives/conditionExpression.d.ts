import {MaybeReadOnlyExpression} from "./maybeExpression";
import {PrimitiveReadOnlyExpression} from "./primitiveExpression";

export type ConditionReadOnlyExpression = PrimitiveReadOnlyExpression & {
  /** Short-circuiting combinators. Immutable. */
  and: (other: ConditionReadOnlyExpression) => ConditionReadOnlyExpression;
  or:  (other: ConditionReadOnlyExpression) => ConditionReadOnlyExpression;

  /** Logical inversion. */
  negate: () => ConditionReadOnlyExpression;

  /** Convenience combinators that accept callbacks producing ConditionReadOnlyExpression values lazily. Returns self. */
  ifTrue:  (cb: () => void) => ConditionReadOnlyExpression; // invoke cb only when receiver is true
  ifFalse: (cb: () => void) => ConditionReadOnlyExpression; // invoke cb only when receiver is false
  /** Marker for HostApi surfaces */
} & {
  
};

export type ConditionReadOnlyExpressionApi = {
  /** Factory function */
  of: (value: boolean) => ConditionReadOnlyExpression;

  /** Register and retrieve named condition rules. */
  asRule: (ruleName: string, expr: ConditionReadOnlyExpression) => ConditionReadOnlyExpressionApi;
  getRule: (ruleName: string) => ConditionReadOnlyExpression;

  /** Marker for HostApi surfaces */
  type: unknown;
};