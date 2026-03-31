import type { StringExpression } from '../primitives/stringExpression';
import type { NumberExpression } from '../primitives/numberExpression';
import type { ConditionExpression } from '../primitives/conditionExpression';
// Circular: containerFilter ↔ entityFilter. TypeScript resolves type-only imports fine.
import type { EntityFilter } from './entityFilter';

/**
 * Marker type for ContainerFilter values on HostApi surfaces.
 *
 * @see ContainerFilterApi.type
 */
export type ContainerFilterType = {
  // marker for HostApi typing
};

/**
 * An immutable, lazily-evaluated expression that extracts a subset of
 * containers from the global container repository or a caller-supplied source
 * list.
 *
 * Filters compose with `and`, `or`, and `not` combinators. Each predicate
 * method returns a new ContainerFilter (immutable). Evaluation is deferred and
 * performed by the runtime against the current read-buffer snapshot.
 *
 * Filters are pure, side-effect-free, and deterministic when evaluated against
 * the same {@link ExecutionContext}.
 *
 * When predicates match `members` (entity lists), a container matches if **any**
 * member satisfies the predicate (existential semantics).
 *
 * @see ContainerFilterApi
 * @see containerFilter.md
 */
export type ContainerFilter = {
  /**
   * Narrow by container id.
   *
   * Includes containers where `fn(id)` evaluates to true.
   *
   * @param fn - Predicate receiving the container's id as a StringExpression.
   */
  byId: (fn: (id: StringExpression) => ConditionExpression) => ContainerFilter;

  /**
   * Narrow by container semantic type (e.g. `"region"`, `"zone"`, `"room"`).
   *
   * Includes containers whose `type` field equals `typeExpr`.
   *
   * @param typeExpr - The semantic type to match.
   */
  byType: (typeExpr: StringExpression) => ContainerFilter;

  /**
   * Narrow to containers that have the given classification / tag.
   *
   * @param classification - The classification string to look for.
   */
  byClassification: (classification: StringExpression) => ContainerFilter;

  /**
   * Narrow to containers whose `text_map` entry for `key` satisfies `fn`.
   *
   * Absent keys produce no match. Multiple values for the same key are tested
   * with existential semantics (match if any value satisfies `fn`).
   *
   * @param key - The text_map key to inspect.
   * @param fn  - Predicate over the text value.
   */
  hasTextValue: (
    key: StringExpression,
    fn: (value: StringExpression) => ConditionExpression
  ) => ContainerFilter;

  /**
   * Narrow to containers whose `number_map` entry for `key` satisfies `fn`.
   *
   * Absent keys produce no match.
   *
   * @param key - The number_map key to inspect.
   * @param fn  - Predicate over the numeric value.
   */
  hasNumberValue: (
    key: StringExpression,
    fn: (value: NumberExpression) => ConditionExpression
  ) => ContainerFilter;

  /**
   * Narrow to containers that hold at least one entity whose id satisfies `fn`.
   *
   * @param fn - Predicate receiving the entity id as a StringExpression.
   */
  containsEntityById: (fn: (id: StringExpression) => ConditionExpression) => ContainerFilter;

  /**
   * Narrow to containers that hold at least one entity matching `entityFilter`.
   *
   * Composes lazily with the EntityFilter API. Implementations must resolve
   * cross-repository predicates at evaluation time.
   *
   * @param entityFilter - An EntityFilter describing the required member.
   */
  containsEntityMatching: (entityFilter: EntityFilter) => ContainerFilter;

  /**
   * Invert another filter (complement relative to the chosen source).
   *
   * @param containerFilter - Filter to negate.
   */
  not: (containerFilter: ContainerFilter) => ContainerFilter;

  /**
   * Intersection: include only containers that satisfy all of `others` as well.
   *
   * @param others - One or more ContainerFilter values to intersect with.
   */
  and: (...others: ContainerFilter[]) => ContainerFilter;

  /**
   * Union: include containers that satisfy this filter or any of `others`.
   *
   * @param others - One or more ContainerFilter values to union with.
   */
  or: (...others: ContainerFilter[]) => ContainerFilter;
};

/**
 * HostApi surface for constructing and registering {@link ContainerFilter}
 * values.
 *
 * Accessible via `hostApi.container.filter` inside module scripts.
 *
 * @example
 * ```ts
 * const regions = hostApi.container.filter.create()
 *   .byType(hostApi.string.of("region"));
 *
 * const large = hostApi.container.filter.create()
 *   .hasNumberValue(hostApi.string.of("capacity"),
 *     n => n.isGreaterOrEqualTo(hostApi.number.of(50))
 *   );
 * ```
 *
 * @see ContainerFilter
 * @see containerFilter.md
 */
export type ContainerFilterApi = {
  /** Create a new ContainerFilter builder. */
  create: () => ContainerFilter;

  /**
   * Register a named ContainerFilter for reuse in modules and tests.
   *
   * @param ruleName - Unique rule identifier.
   * @param filter   - The ContainerFilter to register.
   */
  asRule: (ruleName: string, filter: ContainerFilter) => ContainerFilterApi;

  /**
   * Return a previously registered ContainerFilter by rule name.
   *
   * @param ruleName - Rule identifier to look up.
   */
  getRule: (ruleName: string) => ContainerFilter;

  /**
   * Type marker for HostApi surfaces.
   *
   * No runtime behavior.
   */
  type: ContainerFilterType;
};

/**
 * The HostApi sub-object for container operations exposed at `hostApi.container`.
 *
 * Combined with {@link ContainerExpressionApi} in the master
 * {@link HostApi} type to form the full container surface.
 *
 * @see containerFilter.md
 */
export type ContainerApi = {
  /** Container filter builder and registry. */
  filter: ContainerFilterApi;
};
