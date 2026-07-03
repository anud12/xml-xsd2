import type { UniqueGlobalContainerId } from './ids';
import type { TextMap, NumberMap, TextMapExpression, NumberMapExpression } from './textMapNumberMap';
import type { NumberExpression } from '../primitives/numberExpression';
// Circular: container ↔ entity. TypeScript resolves type-only imports fine.
import type { Entity, EntityExpression } from './entity';

/**
 * Marker type for ContainerExpression values on HostApi surfaces.
 *
 * Pass this as the `type` field in event/effect argument declarations to
 * signal that the argument carries a ContainerExpression.
 *
 * @see ContainerExpressionApi.type
 */
export type ContainerExpressionType = {
  // marker for dynamic HostApi typing
};

/**
 * Controls what happens when an entity's position falls outside
 * the declared size bounds.
 *
 * - `"unbound"` — no bounds enforcement; out-of-range positions are allowed.
 * - `"clamp"`   — positions are clamped to [0, size).
 * - `"wrap"`    — positions wrap modulo size.
 *
 * @see containers.md — Size section
 */
export type OutOfBoundsRule = 'unbound' | 'clamp' | 'wrap';

/**
 * Optional size bounds for a container.
 *
 * When present, defines the valid position range and how out-of-range positions
 * are handled.
 */
export type DimensionSize = {
  /**
   * Number of valid positions (e.g. `10` for a 10-slot bag).
   */
  value: NumberExpression;
  /** Policy applied when an entity's position falls outside [0, value). */
  outOfBounds: OutOfBoundsRule;
};

/**
 * Reference structure listing the entities currently held by a container.
 */
export type EntityReference = {
  entity?: { entityIdReference: string }[];
};



/**
 * Data-model snapshot of a Container as it exists in the world_step.
 *
 * Module code interacts with containers via {@link ContainerExpression}
 * builders rather than mutating this structure directly.
 *
 * @see containers.md
 */
export type Container = {
  /** Globally unique identifier for this container within the world_step. */
  id: UniqueGlobalContainerId;
  /** Optional keyed string metadata. */
  textMap?: TextMap;
  /** Optional keyed numeric metadata. */
  numberMap?: NumberMap;
  /** The set of entities currently held by this container. */
  entities: EntityReference;
  /**
   * Maps a member entity to its x-coordinate as a NumberExpression.
   *
   * @param entity - The member entity.
   */
  getX: (entity: Entity) => NumberExpression;
  /**
   * Maps a member entity to its y-coordinate as a NumberExpression.
   *
   * @param entity - The member entity.
   */
  getY: (entity: Entity) => NumberExpression;
  /**
   * Maps a member entity to the number of cells it occupies as a
   * NumberExpression. Defaults to 1 when not overridden.
   *
   * @param entity - The member entity.
   */
  getSpan: (entity: Entity) => NumberExpression;
  /**
   * Optional size bounds and out-of-bounds policy.
   */
  size?: DimensionSize;
};

/**
 * An immutable, lazily-evaluated builder for constructing Container instances.
 *
 * All methods return a new ContainerExpression (immutable). Evaluation is
 * deferred and performed by the runtime at commit time.
 *
 * @see ContainerExpressionApi
 * @see containers.md
 */
export type ContainerExpression = {
  /**
   * Append an inline member entity built via EntityExpression.
   *
   * The runtime evaluates `entity` when materializing the container.
   * Members are unordered; duplicates are allowed.
   *
   * @param entity - EntityExpression to add as a member.
   */
  withEntity: (entity: EntityExpression) => ContainerExpression;

  /**
   * Replace this container's text_map with the evaluated result of `textMap`.
   *
   * @param textMap - TextMapExpression to evaluate and assign.
   */
  withTextMap: (textMap: TextMapExpression) => ContainerExpression;

  /**
   * Replace this container's number_map with the evaluated result of
   * `numberMap`.
   *
   * @param numberMap - NumberMapExpression to evaluate and assign.
   */
  withNumberMap: (numberMap: NumberMapExpression) => ContainerExpression;

  /**
   * Declare the position function.
   *
   * @param getX - Callback receiving an {@link EntityExpression} and
   * returning a {@link NumberExpression} for the entity's x-coordinate.
   */
  withGetX: (getX: (entity: EntityExpression) => NumberExpression) => ContainerExpression;

  /**
   * Declare the y-coordinate function.
   *
   * @param getY - Callback receiving an {@link EntityExpression} and
   * returning a {@link NumberExpression} for the entity's y-coordinate.
   */
  withGetY: (getY: (entity: EntityExpression) => NumberExpression) => ContainerExpression;

  /**
   * Declare the span function.
   *
   * @param getSpan - Callback receiving an {@link EntityExpression} and
   * returning a {@link NumberExpression} for the entity's occupied cells.
   */
  withGetSpan: (getSpan: (entity: EntityExpression) => NumberExpression) => ContainerExpression;

  /**
   * Declare optional size bounds and out-of-bounds policy.
   *
   * @param value - Number of valid positions (e.g. `hostApi.number.of(20)`).
   * @param outOfBounds - Policy when position exceeds bounds.
   */
  withSize: (value: NumberExpression, outOfBounds: OutOfBoundsRule) => ContainerExpression;
};

/**
 * HostApi surface for constructing {@link ContainerExpression} builders and
 * registering named container templates.
 *
 * Exposed as part of `hostApi.container` inside module scripts (combined with
 * `ContainerApi` from filters).
 *
 * @example
 * ```ts
 * // Slot-based inventory
 * const inv = hostApi.container.create()
 *   .withGetX(e => e.number_map.get("slotIndex"))
 *   .withGetY(e => hostApi.number.of(0))
 *   .withGetSpan(e => e.number_map.get("slotSpan").orElse(hostApi.number.of(1)))
 *   .withSize(hostApi.number.of(20), "clamp");
 * hostApi.container.asRule?.("basic_inventory", inv);
 * ```
 *
 * @see ContainerExpression
 * @see containers.md
 */
export type ContainerExpressionApi = {
  /** Create an empty ContainerExpression builder. */
  create: () => ContainerExpression;

  /**
   * Register or replace a named ContainerExpression rule in the repository.
   * Optional; follow the standard repository/indexing pattern if implemented.
   */
  asRule?: (ruleName: string, expr: ContainerExpression) => ContainerExpressionApi;

  /**
   * Return a ContainerExpression that resolves the named rule at evaluation
   * time.
   * Optional; follow the standard repository/indexing pattern if implemented.
   */
  getRule?: (ruleName: string) => ContainerExpression;

  /**
   * Type marker for HostApi surfaces.
   *
   * Pass `hostApi.container.type` as the `type` field when declaring event or
   * effect arguments dynamically. No runtime behavior.
   */
  type: ContainerExpressionType;
};
