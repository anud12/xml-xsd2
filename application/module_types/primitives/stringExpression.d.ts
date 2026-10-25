import {ConditionReadOnlyExpression} from "./conditionExpression";
import {PrimitiveReadOnlyExpression} from "./primitiveExpression";

export type StringReadOnlyExpressionApi = {
  /** Create a literal */
  of: (s: string) => StringReadOnlyExpression,
  /** Register an expression under a named rule for later getRule(ref) lookups */
  asRule: (ruleName: string, expr: StringReadOnlyExpression) => StringReadOnlyExpressionApi,
  /** Retrieve an API scoped to a previously registered rule */
  getRule: (ruleName: string) => StringReadOnlyExpressionApi,
  /** Marker for HostApi surfaces */
  type: unknown,
}

export type StringReadOnlyExpression = PrimitiveReadOnlyExpression & {
  /** Returns a new expression with the replaced string value (non-mutating) */
  map: (s: StringReadOnlyExpression) => StringReadOnlyExpression,
  /** Concatenate two expressions */
  concat: (other: StringReadOnlyExpression) => StringReadOnlyExpression,
  /** Join multiple expressions using an optional separator */
  join: (parts: StringReadOnlyExpression[], separator?: StringReadOnlyExpression) => StringReadOnlyExpression,
  /** Convenience: prefix a literal string to this expression */
  prefix: (s: string) => StringReadOnlyExpression,
  /** Convenience: suffix a literal string to this expression */
  suffix: (s: string) => StringReadOnlyExpression,
  /** Grouping node to control evaluation order */
  group: (expr: StringReadOnlyExpression) => StringReadOnlyExpression,
  /** Deterministic choice among alternatives */
  oneOf: (choices: StringReadOnlyExpression[]) => StringReadOnlyExpression,
  /** Reference another rule by id (resolved at evaluation time) */
  ref: (ruleId: string) => StringReadOnlyExpression,
  /** Return index of first possible match of `other` inside this expression's language, or -1 if none */
  indexOfExpression: (other: StringReadOnlyExpression, fromInclusive?: any) => any,
  /** Check whether this expression MAY produce a string that contains any possible evaluation of `other`.
   *  Existential semantics: returns a ConditionReadOnlyExpression that is true if there exists s in L(this) and t in L(other) where t is a substring of s.
   *  Use isContainingExactly for a strict universal check.
   */
  isContaining: (other: StringReadOnlyExpression) => ConditionReadOnlyExpression<StringReadOnlyExpression>,
  /** Strict universal check: returns true only if for every expansion s in L(this) and every expansion t in L(other), t is a substring of s.
    *  This is more expensive to compute; implementations MAY fall back to conservative results or timeouts.
    */
  isContainingExactly: (other: StringReadOnlyExpression) => ConditionReadOnlyExpression<StringReadOnlyExpression>,
  /** Optional simple transforms (implementation may provide) */
  upper?: () => StringReadOnlyExpression,
  lower?: () => StringReadOnlyExpression,
  trim?: () => StringReadOnlyExpression,
}