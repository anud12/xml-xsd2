import {EntityExpression} from "./Entity";
import {NumberExpression} from "../primitives/numberExpression";
import {StringExpression} from "../primitives/stringExpression";
import {Expression} from "../Expression";

export type ContainerCreationArguments = {
  /** Keyed string metadata for the container */
  textMap?: Record<string, StringExpression>
  /** Keyed numeric metadata for the container */
  numberMap?: Record<string, NumberExpression>
  /** Maps a member entity to its x-coordinate as a NumberExpression */
  getX?: (entity: EntityExpression) => NumberExpression
  /** Maps a member entity to its y-coordinate as a NumberExpression */
  getY?: (entity: EntityExpression) => NumberExpression
  /** Maps a member entity to the number of cells it occupies along the x-axis. Defaults to 1 when not set */
  getSpanX?: (entity: EntityExpression) => NumberExpression
  /** Maps a member entity to the number of cells it occupies along the y-axis. Defaults to 1 when not set */
  getSpanY?: (entity: EntityExpression) => NumberExpression
  /** Optional size bounds along the x-axis */
  sizeX?: { value: NumberExpression, outOfBounds: OutOfBoundsRule }
  /** Optional size bounds along the y-axis */
  sizeY?: { value: NumberExpression, outOfBounds: OutOfBoundsRule }
  /**
   * How each entity's marker is placed over its span.
   * - `"center"`: center the marker on the span midpoint.
   * - `"top-left"` (default): pin the marker's top-left corner to (x, y).
   */
  alignment?: ContainerAlignment

  /** Optional sector/portal attachment on a named grid (see WorldApi). */
  sector?: SectorDeclaration

  /**
   * Member entities. Each entry is an {@link EntityExpression} reference
   * (the value returned by `hostApi.runtime.setEntity`, or an entity from
   * `getEntityBy`), resolved to its id by the runtime.
   */
  entities: Array<EntityExpression>
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
  start: NumberExpression,
  /** Number of interior cells the opening spans. */
  length: NumberExpression,
}

/**
 * A read-only reference to a named sector grid, returned by
 * `hostApi.world.sectorGrid`. Use it anywhere a grid is referenced.
 */
export type GridExpression = Expression & {
  /** The grid's id. */
  id: string;
}

/** A polyomino footprint placed on a named grid; shapes topology only. */
export type SectorDeclaration = {
  /** The named grid this footprint is placed on (a {@link GridExpression} reference). */
  grid: GridExpression,
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
  create: () => ContainerExpression,
  /** Register and retrieve named container templates */
  asRule?: (ruleName: string, expr: ContainerExpression) => ContainerExpressionApi,
  getRule?: (ruleName: string) => ContainerExpression,
  type: ContainerExpressionType,
}

export type ContainerExpressionType = {
  // marker for dynamic HostApi typing
}

export type OutOfBoundsRule = "unbound" | "clamp" | "wrap"

export type ContainerExpression = Expression & {
  /** The container's id. */
  id: string;
  /** Member entities (an {@link EntityExpression} reference). */
  entities: Array<EntityExpression>;
  /** Keyed string metadata. */
  textMap?: Record<string, StringExpression>;
  /** Keyed numeric metadata. */
  numberMap?: Record<string, NumberExpression>;
  /** The x-coordinate function. */
  getX?: (entity: EntityExpression) => NumberExpression;
  /** The y-coordinate function. */
  getY?: (entity: EntityExpression) => NumberExpression;
  /** The x-span function. */
  getSpanX?: (entity: EntityExpression) => NumberExpression;
  /** The y-span function. */
  getSpanY?: (entity: EntityExpression) => NumberExpression;
  /** The x-axis size bounds. */
  sizeX?: { value: NumberExpression, outOfBounds: OutOfBoundsRule };
  /** The y-axis size bounds. */
  sizeY?: { value: NumberExpression, outOfBounds: OutOfBoundsRule };
  /** Marker placement over each entity's span. */
  alignment?: ContainerAlignment;
  /** Optional sector/portal attachment. */
  sector?: SectorDeclaration;
}