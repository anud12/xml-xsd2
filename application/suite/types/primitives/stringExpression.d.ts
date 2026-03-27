import {ConditionExpression} from "./conditionExpression";

export type StringExpressionApi = {
  /** Create a literal */
  of: (s: string) => StringExpression,
  /** Register an expression under a named rule for later getRule(ref) lookups */
  asRule: (ruleName: string, expr: StringExpression) => StringExpressionApi,
  /** Retrieve an API scoped to a previously registered rule */
  getRule: (ruleName: string) => StringExpressionApi,
  /** Marker for HostApi surfaces */
  type: unknown,
}

export type StringExpression = {
  /** Convenience to create a literal */
  of: (s: string) => StringExpression,
  /** Concatenate two expressions */
  concat: (other: StringExpression) => StringExpression,
  /** Join multiple expressions using an optional separator */
  join: (parts: StringExpression[], separator?: StringExpression) => StringExpression,
  /** Convenience: prefix a literal string to this expression */
  prefix: (s: string) => StringExpression,
  /** Convenience: suffix a literal string to this expression */
  suffix: (s: string) => StringExpression,
  /** Grouping node to control evaluation order */
  group: (expr: StringExpression) => StringExpression,
  /** Deterministic choice among alternatives */
  oneOf: (choices: StringExpression[]) => StringExpression,
  /** Reference another rule by id (resolved at evaluation time) */
  ref: (ruleId: string) => StringExpression,
  /** Return index of first possible match of `other` inside this expression's language, or -1 if none */
  indexOfExpression: (other: StringExpression, fromInclusive?: any) => any,
  /** Check whether this expression MAY produce a string that contains any possible evaluation of `other`.
   *  Existential semantics: returns a ConditionExpression that is true if there exists s in L(this) and t in L(other) where t is a substring of s.
   *  Use isContainingExactly for a strict universal check.
   */
  isContaining: (other: StringExpression) => ConditionExpression,
  /** Strict universal check: returns true only if for every expansion s in L(this) and every expansion t in L(other), t is a substring of s.
   *  This is more expensive to compute; implementations MAY fall back to conservative results or timeouts.
   */
  isContainingExactly: (other: StringExpression) => ConditionExpression,
  /** Optional simple transforms (implementation may provide) */
  upper?: () => StringExpression,
  lower?: () => StringExpression,
  trim?: () => StringExpression,
}