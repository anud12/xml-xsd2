import {NumberExpression} from "../primitives/numberExpression";
import {StringExpression} from "../primitives/stringExpression";
import {Entity, EntityExpression} from "../Entity";
import {AnimationRegistrationArguments, GetAnimationFunction} from "../animation/AnimationRegistration";
import {SpriteResource} from "../texture/SpriteResource";
import {SpriteMap, MapLayerBinding} from "../texture/SpriteMap";

/**
 * A single node id returned by the ui factories. Factories return the id that
 * was registered so callers can nest nodes and hand them to a parent.
 */
export type UiNodeId = string;

/**
 * A panel background: an animation registration (with a name/duration/loop
 * resolved by the host) or a raw texture reference.
 */
export type UiBackground = AnimationRegistrationArguments | SpriteResource;

/**
 * Grid layout tracks. Modules use either weighted columns or explicit scale
 * tracks; the host consumes whatever shape the renderer supports.
 */
export type UiTrack = { scale?: number; weight?: number; min?: number; max?: number };

export type UiLayout = {
  columns?: UiTrack[];
  rows?: UiTrack[];
  rowFirst?: boolean;
  reverse?: boolean;
  gap?: { row?: number; column?: number };
} | "column" | "row";

/**
 * Options accepted by the positioned/decorated ui panel surface (window or
 * division). All fields are optional; a panel is "positioned" when it carries
 * any of x/y/width/height/background/onHover/onClick/anchor.
 */
export type UiPanelOptions = {
  x?: number;
  y?: number;
  width?: number;
  height?: number;
  anchor?: "top-left" | "top" | "top-right" | "center-left" | "center" | "center-right" | "bottom-left" | "bottom" | "bottom-right" | { x?: number; y?: number };
  align?: "top" | "top-left" | "top-right" | "center" | "center-left" | "center-right" | "bottom" | "bottom-left" | "bottom-right";
  background?: UiBackground;
  border?: { texture?: UiBackground; thickness?: number; width?: number; height?: number };
   onHover?: { texture?: UiBackground; background?: UiBackground; thickness?: number; emitAction?: string; stopPropagation?: boolean };
   onClick?: (ctx: UiClickContext) => void;
   container?: string | any;
   layout?: UiLayout;
    /**
     * Enables user edge/corner drag-resize. A bare `true` allows free resizing
     * on all four edges and corners. An object form accepts options:
     * `keepAspectRatio` locks the width:height ratio to the declared size, so
     * a drag grows/shrinks both axes in proportion (the dominant edge/corner
     * wins). Only honored for windows (explicitly sized panels).
     */
    resizable?: boolean | { keepAspectRatio?: boolean };
  [key: string]: unknown;
};

/**
 * Click context handed to a panel's onClick handler.
 */
export type UiClickContext = {
  emitAction: (actionName: string, args?: Record<string, unknown>) => void;
  cursor?: { getX: () => number; getY: () => number };
  [key: string]: unknown;
};

/**
 * Binding for a ui.field node: reads a single field of an entity and renders
 * it as the field's text/number value.
 */
export type UiFieldBinding = {
  entity: string;
  map: "number" | "text";
  name: string;
  fallback?: string;
  align?: string;
};

/**
 * Arguments for ui.containerView: a sized view of a container where each
 * entity is placed by its container coordinates (getX/getY/span) and the view
 * is re-laid-out every tick so panels track movement.
 */
export type UiContainerViewOptions = UiPanelOptions & {
  container: string;
  /** Total view width in logical units (the whole view, not per-cell). */
  width: number;
  /** Total view height in logical units (the whole view, not per-cell). */
  height: number;
};

/**
 * Arguments for ui.sectorGrid: a sized top-down view of a runtime sector grid.
 * The engine materializes one cell window per footprint square (positioned by
 * cellSize, rendered by `render`) and one headless arrow per portal (drawn by
 * the engine as a straight shaft, width portalThickness), re-resolved every
 * tick. Portals are engine-owned; the render is never invoked for them.
 */
export type UiSectorGridOptions = UiPanelOptions & {
  /** The runtime sector grid id (declared via world.sectorGrid). */
  grid: string;
  /** Edge length of one grid cell in logical units (default 32). */
  cellSize?: number;
  /** Gap between adjacent cells in logical units: each cell is inset by half
   * this on every side, so the space between two neighbors is `cellGap`
   * (default 0 = cells are flush). */
  cellGap?: number;
  /** Shaft width of the engine-drawn portal headless arrows (default 6). */
  portalThickness?: number;
};

/**
 * One item a sector grid view materializes: a footprint cell. The render
 * receives this descriptor and returns the node id to place; the engine sets
 * its position/size from the grid. Portals are not passed to the render.
 */
export type UiSectorGridItem = {
  /** Stable id for the item node (e.g. "<view>-cell-<x>-<y>"). */
  id: string;
  /** Ordinal index within the view's materialized items. */
  index: number;
  /** Always "cell": a footprint square of the sector grid. */
  sector: "cell";
  /** The grid square's x. */
  x?: number;
  /** The grid square's y. */
  y?: number;
  /** The owning container id. */
  container?: string;
};

/**
 * Arguments for ui.entityList: a flow list that materializes one child (from
 * the render lambda) per entity in the named container.
 */
export type UiEntityListOptions = {
  container: string;
  vertical?: boolean;
};

/**
 * The module-facing ui surface: node factories plus texture/animation helpers.
 */
export type UiApi = {
  registerPanel: (panelOptions: import("./Panel").PanelOptions) => {};

  /** Positional/sized/decorated panel: a window when positioned, a division otherwise. */
  panel: (id: string, options?: UiPanelOptions, children?: UiNodeId[]) => UiNodeId;
  window: (id: string, options?: UiPanelOptions, children?: UiNodeId[]) => UiNodeId;
  div: (id: string, options?: UiPanelOptions, children?: UiNodeId[]) => UiNodeId;
  canvas: (id: string, options?: UiPanelOptions, children?: UiNodeId[]) => UiNodeId;
  text: (id: string, value: string | number) => UiNodeId;
  image: (id: string, src: string) => UiNodeId;
  field: (id: string, binding: UiFieldBinding) => UiNodeId;

  /** A flow list: one child per entity in the container (render returns an array of node ids). */
  entityList: (name: string, args: UiEntityListOptions, render: (entity: Entity) => UiNodeId[]) => UiNodeId;

  /** A sized view of a container: one panel per entity placed on its cell, re-resolved each tick. */
  containerView: (name: string, args: UiContainerViewOptions, render: (entity: Entity) => UiNodeId) => UiNodeId;

  /** A top-down view of a sector grid: one cell per footprint square (rendered by `render`) + one engine-drawn headless arrow per portal, all positioned by the engine each tick. */
  sectorGrid: (name: string, args: UiSectorGridOptions, render: (item: UiSectorGridItem) => UiNodeId) => UiNodeId;

  spriteMapTIFF: (mapPath: string, layers: MapLayerBinding[]) => SpriteMap;
  getSpritePNG: (path: string) => SpriteResource;
  getAnimation: GetAnimationFunction;
};
