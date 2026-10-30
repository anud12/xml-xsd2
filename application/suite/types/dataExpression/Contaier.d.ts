import {EntityReadOnlyExpression} from "./Entity";
import {NumberReadOnlyExpression} from "../primitives/numberExpression";
import {StringReadOnlyExpression} from "../primitives/stringExpression";
import {ArrayReadOnlyExpression} from "../primitives/arrayExpression";
import {Expression} from "../Expression";

export type ContainerCreationArguments = {
  /** Keyed string metadata for the container */
  textMap?: Record<string, StringReadOnlyExpression>
  /** Keyed numeric metadata for the container */
  numberMap?: Record<string, NumberReadOnlyExpression>
  /** Maps a member entity to its x-coordinate as a NumberReadOnlyExpression */
  getX?: (entity: EntityReadOnlyExpression) => NumberReadOnlyExpression
  /** Maps a member entity to its y-coordinate as a NumberReadOnlyExpression */
  getY?: (entity: EntityReadOnlyExpression) => NumberReadOnlyExpression
  /** Maps a member entity to the number of cells it occupies along the x-axis. Defaults to 1 when not set */
  getSpanX?: (entity: EntityReadOnlyExpression) => NumberReadOnlyExpression
  /** Maps a member entity to the number of cells it occupies along the y-axis. Defaults to 1 when not set */
  getSpanY?: (entity: EntityReadOnlyExpression) => NumberReadOnlyExpression
  /** Optional size bounds along the x-axis */
  sizeX?: { value: NumberReadOnlyExpression, outOfBounds: OutOfBoundsRule }
  /** Optional size bounds along the y-axis */
  sizeY?: { value: NumberReadOnlyExpression, outOfBounds: OutOfBoundsRule }
  /**
   * How each entity's marker is placed over its span.
   * - `"center"`: center the marker on the span midpoint.
   * - `"top-left"` (default): pin the marker's top-left corner to (x, y).
   */
  alignment?: ContainerAlignment

  /** Optional sector/portal attachment on a named grid (see WorldApi). */
  sector?: SectorDeclaration

  /**
   * Member entities. Each entry is an {@link EntityReadOnlyExpression} reference
   * (the value returned by `hostApi.runtime.setEntity`, or an entity from
   * `getEntityBy`), resolved to its id by the runtime.
   */
  entities: Array<EntityReadOnlyExpression>
}

/** A grid square coordinate [x, y]. y increases downward. */
export type SectorCoord = [number, number]

/** A footprint side, clockwise from north. */
export type SectorSide = "N" | "E" | "S" | "W"

/** An opening declared on a boundary edge {cell, side}. */
export type SectorOpening = {
  /** The footprint edge square (relative to the sector's footprint origin). */
  cell: SectorCoord,
  side: SectorSide,
  /** First interior cell along the edge. */
  start: NumberReadOnlyExpression,
  /** Number of interior cells the opening spans. */
  length: NumberReadOnlyExpression,
}

/**
 * A read-only reference to a named sector grid, returned by
 * `hostApi.world.sectorGrid`. Use it anywhere a grid is referenced.
 */
export type GridReadOnlyExpression = Expression & {
  /** The grid's id. */
  id: string;
}

/** A polyomino footprint placed on a named grid; shapes topology only. */
export type SectorDeclaration = {
  /** The named grid this footprint is placed on (a {@link GridReadOnlyExpression} reference). */
  grid: GridReadOnlyExpression,
  /** Footprint square offsets, normalized to min x/y = 0. */
  footprint: Array<SectorCoord>,
  /** Grid coordinate the whole footprint is placed at. */
  at: SectorCoord,
  /** Openings declared on boundary edges (at most one per edge). */
  openings: Array<SectorOpening>,
}

export type ContainerAlignment = "center" | "top-left"

export type ContainerExpressionApi = {
  /** Create an empty container builder */
  create: () => ContainerReadOnlyExpression,
  /** Register and retrieve named container templates */
  asRule?: (ruleName: string, expr: ContainerReadOnlyExpression) => ContainerExpressionApi,
  getRule?: (ruleName: string) => ContainerReadOnlyExpression,
  type: ContainerExpressionType,
}

export type ContainerExpressionType = {
  // marker for dynamic HostApi typing
}

export type OutOfBoundsRule = "unbound" | "clamp" | "wrap"

export type ContainerReadOnlyExpression = Expression & {
  /** The container's id. */
  id: StringReadOnlyExpression;
  /** Member entities (an {@link ArrayReadOnlyExpression} of {@link EntityReadOnlyExpression} references). */
  entities: ArrayReadOnlyExpression<EntityReadOnlyExpression>;
  /** Keyed string metadata. */
  textMap?: Record<string, StringReadOnlyExpression>;
  /** Keyed numeric metadata. */
  numberMap?: Record<string, NumberReadOnlyExpression>;
  /** The x-coordinate function. */
  getX?: (entity: EntityReadOnlyExpression) => NumberReadOnlyExpression;
  /** The y-coordinate function. */
  getY?: (entity: EntityReadOnlyExpression) => NumberReadOnlyExpression;
  /** The x-span function. */
  getSpanX?: (entity: EntityReadOnlyExpression) => NumberReadOnlyExpression;
  /** The y-span function. */
  getSpanY?: (entity: EntityReadOnlyExpression) => NumberReadOnlyExpression;
  /** The x-axis size bounds. */
  sizeX?: { value: NumberReadOnlyExpression, outOfBounds: OutOfBoundsRule };
  /** The y-axis size bounds. */
  sizeY?: { value: NumberReadOnlyExpression, outOfBounds: OutOfBoundsRule };
  /** Marker placement over each entity's span. */
  alignment?: ContainerAlignment;
  /** Optional sector/portal attachment. */
  sector?: SectorDeclaration;
}

/**
 * A mutable reference to a container, returned by `EventApplyContext.getMutableContainersBy`.
 * Extends the read-only expression with member mutation. Available only during
 * the `apply` phase; mutations are committed atomically with the tick.
 */
export type ContainerMutableExpression = ContainerReadOnlyExpression & {
  /** Add an entity to this container's member list. */
  addEntity: (entity: EntityReadOnlyExpression) => ContainerMutableExpression;
  /** Remove an entity from this container's member list by id. */
  removeEntity: (entityId: StringReadOnlyExpression) => ContainerMutableExpression;
}