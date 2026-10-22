import type { ConditionExpression } from '../primitives/conditionExpression';
import type { StringExpression } from '../primitives/stringExpression';
import type { NumberExpression } from '../primitives/numberExpression';
import type { MaybeExpression } from '../primitives/maybeExpression';
import type { ListExpression } from '../primitives/listExpression';

/** A minimal entity reference returned by `getEntitiesInsideArea`. */
export type EntityInsideAreaReference = {
  /** The contained entity's id. */
  getId: () => string;
};

/**
 * Predicate invoked once per candidate entity. Must return a
 * {@link ConditionExpression}.
 *
 * The lambda receives the per-entity query surface
 * ({@link EntityFilterExpression}): an `id` StringExpression plus `number_map`
 * / `text_map` lookups that return {@link MaybeExpression}s. Absent map keys
 * yield an empty Maybe, so a predicate extracts its condition through the
 * Maybe and falls back to a falsy condition:
 *
 * ```ts
 * (e) => e.number_map.get("hp")
 *   .map(n => n.isLessOrEqualTo(number.of(5)))
 *   .orElse(condition.of(false))
 * ```
 */
export type FilterLambda = (e: EntityFilterExpression) => ConditionExpression;

/**
 * Leaf variant: the entity matches iff every lambda yields a truthy
 * condition. An empty array matches everything (vacuous truth).
 */
export type FilterPredicates = { filters: FilterLambda[] };

/**
 * The entity matches iff every child FilterArg matches.
 * An empty array matches everything (vacuous truth).
 */
export type FilterAnd = { and: FilterArg[] };

/**
 * The entity matches iff at least one child FilterArg matches.
 * An empty array matches everything (vacuous truth).
 */
export type FilterOr = { or: FilterArg[] };

/**
 * The entity matches iff no child FilterArg matches.
 * An empty array matches everything (vacuous truth).
 */
export type FilterNot = { not: FilterArg[] };

/**
 * A data-driven, recursive filter argument.
 *
 * A plain object with **exactly one** of the four keys; anything else is
 * malformed (deterministic log line, no matches — never a partial result or a
 * fallback entity).
 *
 * @see entityFilter.md
 */
export type FilterArg = FilterPredicates | FilterAnd | FilterOr | FilterNot;

/**
 * Everything `getEntityBy` accepts: a plain {@link FilterArg} object.
 *
 * Legacy inputs (a plain id string, a bare id predicate, or an `{ id: fn }`
 * object) are no longer accepted — they normalize to a malformed filter.
 */
export type EntityFilter = FilterArg;

/**
 * The per-entity surface handed to {@link FilterLambda} predicates and the
 * list element accessors.
 *
 * Map lookups return {@link MaybeExpression}s rather than values directly: an
 * absent key is an empty Maybe, so predicates and mutations flow through
 * `map` / `ifPresent` and a missing key never matches (and never throws).
 */
export type EntityFilterExpression = {
  /** The entity's id as a StringExpression (isContainingExactly, isContaining, ...). */
  id: StringExpression;

  /**
   * number_map lookup. Returns the entity's number value for `key` wrapped in
   * a MaybeExpression (empty when the key is absent).
   *
   * Compare through the contained NumberExpression:
   * `e.number_map.get("hp").map(n => n.isLessOrEqualTo(number.of(5)))`.
   */
  number_map: {
    get: (key: StringExpression) => MaybeExpression<NumberExpression>;
  };

  /**
   * text_map lookup. Returns the entity's text value for `key` wrapped in a
   * MaybeExpression (empty when the key is absent).
   *
   * Compare through the contained StringExpression:
   * `e.text_map.get("job").map(t => t.isContainingExactly(string.of("smith")))`.
   * Mutate with `e.text_map.get("flag").ifPresent(v => v.set(string.of("x")))`.
   */
  text_map: {
    get: (key: StringExpression) => MaybeExpression<StringExpression>;
  };

  /** Container memberships (currently empty at runtime). */
  containers: ListExpression;

  /** The entities inside one named area of this entity (empty when absent). */
  getEntitiesInsideArea: (areaName: StringExpression) => ListExpression;
};
