import type { StringExpression } from '../primitives/stringExpression';
import type { NumberExpression } from '../primitives/numberExpression';
import type { ConditionExpression } from '../primitives/conditionExpression';
// Circular: entityFilter ↔ containerFilter. TypeScript resolves type-only imports fine.
import type { ContainerFilter } from './containerFilter';

/**
 * Marker type for EntityFilter values on HostApi surfaces.
 *
 * @see EntityFilterApi.type
 */
export type EntityFilterType = {
  // marker for HostApi typing
};

/**
 * An immutable, lazily-evaluated expression that extracts a subset of entities
 * from the global entity repository or a caller-supplied source list.
 *
 * Filters compose with `and`, `or`, and `not` combinators. Each predicate
 * method returns a new EntityFilter (immutable). Evaluation is deferred and
 * performed by the runtime against the current read-buffer snapshot.
 *
 * Filters are pure, side-effect-free, and deterministic when evaluated against
 * the same {@link ExecutionContext}.
 *
 * When matching `text_map` or `number_map` keys with multiple values, a match
 * occurs if **any** value satisfies the predicate (existential semantics).
 * Absent keys produce no match.
 *
 * @see EntityFilterApi
 * @see entityFilter.md
 */
export type EntityFilter = {
  /**
   * Narrow by entity id.
   *
   * Includes entities where `fn(id)` evaluates to true.
   *
   * @param fn - Predicate receiving the entity's id as a StringExpression.
   */
  byId: (fn: (id: StringExpression) => ConditionExpression) => EntityFilter;

  /**
   * Narrow to entities that have a `text_map` entry for `key` where at least
   * one value satisfies `fn`.
   *
   * Absent keys produce no match.
   *
   * @param key - The text_map key to inspect.
   * @param fn  - Predicate over each text value.
   */
  hasTextValue: (
    key: StringExpression,
    fn: (value: StringExpression) => ConditionExpression
  ) => EntityFilter;

  /**
   * Narrow to entities that have a `number_map` entry for `key` where at least
   * one value satisfies `fn`.
   *
   * Absent keys produce no match.
   *
   * @param key - The number_map key to inspect.
   * @param fn  - Predicate over each numeric value.
   */
  hasNumberValue: (
    key: StringExpression,
    fn: (value: NumberExpression) => ConditionExpression
  ) => EntityFilter;

  /**
   * Narrow to entities that are members of at least one container matched by
   * `containerFilter`.
   *
   * Composes lazily with the ContainerFilter API.
   *
   * @param containerFilter - A ContainerFilter describing the required container.
   */
  hasContainer: (containerFilter: ContainerFilter) => EntityFilter;

  /**
   * Invert another filter (complement relative to the chosen source).
   *
   * @param entityFilter - Filter to negate.
   */
  not: (entityFilter: EntityFilter) => EntityFilter;

  /**
   * Intersection: include only entities that satisfy all of `others` as well.
   *
   * @param others - One or more EntityFilter values to intersect with.
   */
  and: (...others: EntityFilter[]) => EntityFilter;

  /**
   * Union: include entities that satisfy this filter or any of `others`.
   *
   * @param others - One or more EntityFilter values to union with.
   */
  or: (...others: EntityFilter[]) => EntityFilter;
};

/**
 * HostApi surface for constructing and registering {@link EntityFilter} values.
 *
 * Accessible via `hostApi.entity.filter` inside module scripts.
 *
 * @example
 * ```ts
 * const blacksmiths = hostApi.entity.filter.create()
 *   .hasTextValue(
 *     hostApi.string.of("job"),
 *     v => v.isContaining(hostApi.string.of("blacksmith"))
 *   );
 *
 * const highLevel = hostApi.entity.filter.create()
 *   .hasNumberValue(
 *     hostApi.string.of("level"),
 *     n => n.isGreaterOrEqualTo(hostApi.number.of(10))
 *   );
 * ```
 *
 * @see EntityFilter
 * @see entityFilter.md
 */
export type EntityFilterApi = {
  /** Create a new EntityFilter builder. */
  create: () => EntityFilter;

  /**
   * Register a named EntityFilter for reuse in modules and tests.
   *
   * @param ruleName - Unique rule identifier.
   * @param filter   - The EntityFilter to register.
   */
  asRule: (ruleName: string, filter: EntityFilter) => EntityFilterApi;

  /**
   * Return a previously registered EntityFilter by rule name.
   *
   * @param ruleName - Rule identifier to look up.
   */
  getRule: (ruleName: string) => EntityFilter;

  /**
   * Type marker for HostApi surfaces.
   *
   * No runtime behavior.
   */
  type: EntityFilterType;
};

/**
 * The HostApi sub-object for entity operations exposed at `hostApi.entity`.
 *
 * Combined with {@link EntityExpressionApi} in the master {@link HostApi} type
 * to form the full entity surface.
 *
 * @see entityFilter.md
 */
export type EntityApi = {
  /** Entity filter builder and registry. */
  filter: EntityFilterApi;
};
