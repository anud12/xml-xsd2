import { ConditionReadOnlyExpression } from "./primitives/conditionExpression";
import { EntityReadOnlyExpression } from "./dataExpression/Entity";

/**
 * Per-entity predicate for {@link FilterPredicates}.
 *
 * Invoked with the entity's {@link EntityReadOnlyExpression} query surface and must
 * yield a {@link ConditionReadOnlyExpression}. Absent map keys never match: extract
 * the condition through the MaybeExpression and fall back to a falsy
 * condition, e.g.
 *
 * ```ts
 * (e) => e.number_map.get("hp")
 *   .map(n => n.isLessOrEqualTo(number.of(5)))
 *   .orElse(condition.of(false))
 * ```
 */
export type FilterLambda = (e: EntityReadOnlyExpression) => ConditionReadOnlyExpression;

/** Leaf variant: the entity matches iff every lambda yields a truthy condition. Empty array matches everything. */
export type FilterPredicates = { filters: FilterLambda[] };

/** The entity matches iff every child FilterArg matches. Empty array matches everything. */
export type FilterAnd = { and: FilterArg[] };

/** The entity matches iff at least one child FilterArg matches. Empty array matches everything. */
export type FilterOr = { or: FilterArg[] };

/** The entity matches iff no child FilterArg matches. Empty array matches everything. */
export type FilterNot = { not: FilterArg[] };

/**
 * A data-driven, recursive filter argument. A plain object with exactly one
 * of the four keys; anything else (plain strings, bare functions, legacy
 * `{ id: fn }` objects, null) is malformed (logged, no matches — never a
 * fallback entity).
 */
export type FilterArg = FilterPredicates | FilterAnd | FilterOr | FilterNot;

/** Everything `getEntityBy` accepts: a plain FilterArg object. */
export type EntityFilter = FilterArg;
