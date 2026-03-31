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
 * Controls what happens when an entity's position in a dimension falls outside
 * the declared size bounds.
 *
 * - `"unbound"` — no bounds enforcement; out-of-range positions are allowed.
 * - `"clamp"`   — positions are clamped to [0, size).
 * - `"wrap"`    — positions wrap modulo size.
 *
 * @see containers.md — Dimensions section
 */
export type OutOfBoundsRule = 'unbound' | 'clamp' | 'wrap';

/**
 * Optional bounds for a container dimension.
 *
 * When present, defines the valid index range and how out-of-range positions
 * are handled.
 */
export type DimensionSize = {
  /**
   * Number of valid positions in this dimension (e.g. `10` for a 10-slot bag).
   */
  value: NumberExpression;
  /** Policy applied when an entity's position falls outside [0, value). */
  outOfBounds: OutOfBoundsRule;
};

/**
 * A single dimension of a Container.
 *
 * Each dimension is a mapping from a member entity to its coordinate along
 * that axis. The number of declared dimensions determines container arity
 * (1 → 1D, 2 → 2D).
 *
 * @see containers.md — Dimensions section
 */
export type Dimension = {
  /** Optional human-friendly name for this dimension (e.g. `"slot"`, `"row"`). */
  name?: string;
  /**
   * Maps a member entity to its position in this dimension.
   *
   * Evaluated by the runtime to produce integer coordinates. How non-integer
   * values are handled (floor/round/reject) is the container rule's
   * responsibility.
   */
  mapping: (entity: Entity) => NumberExpression;
  /** Optional bounds and out-of-bounds policy. */
  size?: DimensionSize;
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
   * Optional spatial dimensions. The number of declared dimensions determines
   * the container's arity: 0 = unstructured, 1 = 1D (slots), 2 = 2D (grid).
   * Only 1D and 2D containers are supported.
   */
  dimensions?: Dimension[];
};

/**
 * HostApi factory for creating {@link DimensionExpression} builders.
 *
 * Accessible via `hostApi.container.dimension` inside module scripts.
 *
 * @see DimensionExpression
 * @see containers.md — Dimensions section
 */
export type DimensionExpressionApi = {
  /** Create an empty DimensionExpression builder. */
  create: () => DimensionExpression;

  /**
   * Register or replace a named DimensionExpression rule.
   * Optional; follow the standard repository/indexing pattern if implemented.
   */
  asRule?: (ruleName: string, expr: DimensionExpression) => DimensionExpressionApi;

  /**
   * Return a DimensionExpression that resolves the named rule at evaluation
   * time.
   * Optional; follow the standard repository/indexing pattern if implemented.
   */
  getRule?: (ruleName: string) => DimensionExpression;
};

/**
 * An immutable, lazily-evaluated builder for declaring a container dimension.
 *
 * All methods return a new DimensionExpression (immutable).
 *
 * @see DimensionExpressionApi
 * @see containers.md — Dimensions section
 */
export type DimensionExpression = {
  /**
   * Set a human-friendly name for this dimension.
   *
   * @param name - e.g. `"slot"`, `"row"`, `"col"`.
   */
  withName: (name: string) => DimensionExpression;

  /**
   * Declare the mapping function from a member entity to its coordinate in
   * this dimension.
   *
   * The callback receives an {@link EntityExpression} and must return a
   * {@link NumberExpression} representing the entity's position.
   */
  withMapping: (mapping: (entity: EntityExpression) => NumberExpression) => DimensionExpression;

  /**
   * Declare optional size bounds and the out-of-bounds policy.
   *
   * @param value       - Number of valid positions (e.g. `hostApi.number.of(20)`).
   * @param outOfBounds - Policy when position exceeds bounds.
   */
  withSize: (value: NumberExpression, outOfBounds: OutOfBoundsRule) => DimensionExpression;
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
   * Add a dimension declaration to this container builder.
   *
   * Containers support at most two dimensions (1D or 2D).
   *
   * @param dimension - DimensionExpression to append.
   */
  withDimension: (dimension: DimensionExpression) => ContainerExpression;

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
 * const inv = hostApi.container.create()
 *   .withDimension(hostApi.container.dimension?.create()
 *     .withName("slot")
 *     .withMapping(e => e.withNumberMap(hostApi.numberMap.create()).withTextMap(hostApi.textMap.create()))
 *     .withSize(hostApi.number.of(20), "clamp")
 *   );
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

  /**
   * Dimension expression builder factory.
   *
   * Optional — implementations may omit if dimension support is not yet
   * implemented.
   */
  dimension?: DimensionExpressionApi;
};
