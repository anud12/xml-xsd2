import {NumberReadOnlyExpression} from "../primitives/numberExpression";
import {Expression} from "../Expression";

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
