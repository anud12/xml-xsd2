import {Expression} from "../Expression";
import {MaybeReadOnlyExpression} from "./maybeExpression";
import {PrimitiveReadOnlyExpression} from "./primitiveExpression";

/**
 * Branch handle returned by `ifTrue` / `ifFalse`.
 * Call `.or()` to register the fallback branch (executed when the condition
 * took the opposite path). Both branches receive the asserted value and must
 * return the same type R. `.or()` returns the result of the taken branch.
 */
export type ConditionBranch<T extends Expression, R extends Expression> = {
  /** Fallback branch: invoked when the condition did NOT take the primary path. Returns the branch result. */
  or: (cb: (value: T) => R) => R;
};

/**
 * A lazy boolean condition that carries the asserted value of type T.
 * Use `ifTrue`/`ifFalse` to branch on the condition while accessing the value.
 */
export type ConditionReadOnlyExpression<T extends Expression> = PrimitiveReadOnlyExpression & {
  /** Short-circuiting combinators. Immutable. */
  and: (other: ConditionReadOnlyExpression<T>) => ConditionReadOnlyExpression<T>;
  or:  (other: ConditionReadOnlyExpression<T>) => ConditionReadOnlyExpression<T>;

  /** Logical inversion. */
  negate: () => ConditionReadOnlyExpression<T>;

  /** Invoke cb(value) when the condition is true. Returns a branch; call .or() for the false path. */
  ifTrue:  <R extends Expression>(cb: (value: T) => R) => ConditionBranch<T, R>;
  /** Invoke cb(value) when the condition is false. Returns a branch; call .or() for the true path. */
  ifFalse: <R extends Expression>(cb: (value: T) => R) => ConditionBranch<T, R>;
  /** Marker for HostApi surfaces */
} & {

};

export type ConditionReadOnlyExpressionApi = {
  /** Factory function */
  of: <T extends Expression>(value: boolean) => ConditionReadOnlyExpression<T>;

  /** Register and retrieve named condition rules. */
  asRule: <T extends Expression>(ruleName: string, expr: ConditionReadOnlyExpression<T>) => ConditionReadOnlyExpressionApi;
  getRule: <T extends Expression>(ruleName: string) => ConditionReadOnlyExpression<T>;

  /** Marker for HostApi surfaces */
  type: unknown;
};