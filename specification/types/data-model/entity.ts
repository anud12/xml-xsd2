import type { UniqueGlobalEntityId, UniqueGlobalContainerId, ContainerReference } from './ids';
import type { TextMap, NumberMap, TextMapExpression, NumberMapExpression } from './textMapNumberMap';
// Circular: entity ↔ container. TypeScript resolves type-only imports fine.
import type { ContainerExpression } from './container';

/**
 * Marker type for EntityExpression values on HostApi surfaces.
 *
 * Pass this as the `type` field in event/effect argument declarations to
 * signal that the argument carries an EntityExpression.
 *
 * @see EntityExpressionApi.type
 */
export type EntityExpressionType = {
  // marker for dynamic HostApi typing
};

/**
 * Membership entry that links an entity to one of its containers.
 *
 * Stored inside {@link Entity.containers} to express the set of containers
 * this entity currently belongs to.
 */
export type ContainerList = {
  /** Id of the container this entity is a member of. */
  containerIdReference: UniqueGlobalContainerId;
};

/**
 * Data-model snapshot of an Entity as it exists in the world_step.
 *
 * This is the persistent representation read from the world state. Module
 * code interacts with entities via {@link EntityExpression} builders rather
 * than mutating this structure directly.
 *
 * @see entities.md
 */
export type Entity = {
  /** Globally unique identifier for this entity within the world_step. */
  id: UniqueGlobalEntityId;
  /** Keyed string attributes (e.g. name, title, description). */
  textMap: TextMap;
  /** Keyed numeric attributes (e.g. hp, strength, level). */
  numberMap: NumberMap;
  /** Containers this entity is currently a member of. */
  containers: ContainerList[];
};

/**
 * An immutable, lazily-evaluated builder for constructing or mutating Entity
 * instances.
 *
 * All methods return a **new** EntityExpression (immutable). Evaluation is
 * deferred and performed by the runtime at commit time.
 *
 * @see EntityExpressionApi
 * @see entities.md
 */
export type EntityExpression = {
  /**
   * Replace this entity's text_map with the evaluated result of `textMap`.
   *
   * Returns a new EntityExpression with the updated map.
   *
   * @param textMap - A TextMapExpression to evaluate and assign.
   */
  withTextMap: (textMap: TextMapExpression) => EntityExpression;

  /**
   * Replace this entity's number_map with the evaluated result of `numberMap`.
   *
   * Returns a new EntityExpression with the updated map.
   *
   * @param numberMap - A NumberMapExpression to evaluate and assign.
   */
  withNumberMap: (numberMap: NumberMapExpression) => EntityExpression;

  /**
   * Append a container membership to this entity.
   *
   * Multiple calls append in declaration order. Accepts either an inline
   * {@link ContainerExpression} or a {@link ContainerReference} (id-only ref).
   *
   * @param container - Container to append.
   */
  withContainer: (container: ContainerExpression | ContainerReference) => EntityExpression;
};

/**
 * HostApi surface for constructing {@link EntityExpression} builders and
 * registering named entity templates.
 *
 * Exposed as part of `hostApi.entity` inside module scripts (combined with
 * `EntityApi` from filters).
 *
 * @example
 * ```ts
 * const stats = hostApi.numberMap.create().put("hp", hostApi.number.of(12));
 * const goblin = hostApi.entity.create().withNumberMap(stats);
 * ```
 *
 * @see EntityExpression
 * @see entities.md
 */
export type EntityExpressionApi = {
  /**
   * Create an empty EntityExpression builder.
   */
  create: () => EntityExpression;

  /**
   * Register or replace a named EntityExpression rule in the repository.
   *
   * Optional; follow the standard repository/indexing pattern if implemented.
   *
   * @param ruleName - Unique rule identifier.
   * @param expr     - The EntityExpression to register.
   */
  asRule?: (ruleName: string, expr: EntityExpression) => EntityExpressionApi;

  /**
   * Return an EntityExpression that resolves the named rule at evaluation time.
   *
   * Optional; follow the standard repository/indexing pattern if implemented.
   *
   * @param ruleName - Rule identifier to look up.
   */
  getRule?: (ruleName: string) => EntityExpression;

  /**
   * Type marker for HostApi surfaces.
   *
   * Pass `hostApi.entity.type` as the `type` field when declaring event or
   * effect arguments dynamically. No runtime behavior.
   */
  type: EntityExpressionType;
};
