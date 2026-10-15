import {NumberExpression} from "../primitives/numberExpression";
import {Entity, EntityExpression} from "../dataExpression/Entity";
import {AnimationRegistrationArguments, AnimationExpression, GetAnimationFunction} from "../dataExpression/animation/AnimationRegistration";
import {SpriteResource} from "../texture/SpriteResource";
import {SpriteMap, MapLayerBinding} from "../texture/SpriteMap";
import {ContainerExpression, GridExpression} from "../dataExpression/Contaier";
import {ActionExpression} from "../dataExpression/action/no-input-action";
import {AlignOption} from "./Panel";

/**
 * A single node id returned by the ui factories. Factories return the id that
 * was registered so callers can nest nodes and hand them to a parent.
 */
export type UiNodeId = string;

/**
 * A panel background: an animation registration (with a name/duration/loop
 * resolved by the host) or a raw texture reference.
 */
export type UiBackground = AnimationExpression | AnimationRegistrationArguments | SpriteResource;

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
  /**
   * The sim-container this layout's cell grid maps to for click->cursor
   * resolution (the "board" pattern): a click resolves to a container cell
   * from the container's sizeX/sizeY, independent of the layout tracks.
   */
  container?: ContainerExpression;
} | "column" | "row";

/**
 * Options for the positioned/decorated ui panel surface (window or division).
 * The panel configuration is nested under a required `panel` key; all of its
 * fields are optional, and a panel is "positioned" when it carries any of
 * x/y/width/height/background/onHover/onClick/anchor. The host passes it
 * through as the node's `options.panel` (the renderer reads `panel.*`).
 */
export type UiPanelOptions = {
  panel: {
    x?: number;
    y?: number;
    width?: number;
    height?: number;
    anchor?: "top-left" | "top" | "top-right" | "center-left" | "center" | "center-right" | "bottom-left" | "bottom" | "bottom-right" | { x?: number; y?: number };
    align?: AlignOption;
    background?: UiBackground;
    border?: { texture?: UiBackground; thickness?: number; width?: number; height?: number };
    onHover?: { texture?: UiBackground; background?: UiBackground; thickness?: number; emitAction?: ActionExpression; stopPropagation?: boolean };
    onClick?: (ctx: UiClickContext) => void;
    layout?: UiLayout;
    /**
     * Enables user edge/corner drag-resize. A bare `true` allows free resizing
     * on all four edges and corners. An object form accepts options:
     * `keepAspectRatio` locks the width:height ratio to the declared size, so
     * a drag grows/shrinks both axes in proportion (the dominant edge/corner
     * wins). Only honored for windows (explicitly sized panels).
     */
    resizable?: boolean | { keepAspectRatio?: boolean };
    /**
     * The entity's declared area polygons, drawn over the item sprite as a
     * solid outline (line color + thickness) and a translucent rgba body each.
     * Engine-owned for container-view items: the engine stamps `polygons`
     * (view-local logical units) from the entity's declared areas every tick
     * (the areas selected by the view's `area` option, or the entity's first
     * declared area with default styling when omitted). An entity with no
     * matching area gets a 1x1 pixel/rectangle fallback polygon. Modules may
     * also set this explicitly on any panel to draw arbitrary polygons.
     */
    areaOutline?: {
      /** The polygons to draw; each carries its own vertices and outline style. */
      polygons: {
        /** Polygon vertices in view-local logical units, implicitly closed. */
        points: [number, number][];
        /** Outline line color as [r, g, b, a] in 0..1 (default red). */
        color?: [number, number, number, number];
        /** Body fill color as [r, g, b, a] in 0..1 (default green). */
        bodyColor?: [number, number, number, number];
        /** Outline line thickness in logical units (default 2). */
        thickness?: number;
      }[];
    };
    [key: string]: unknown;
  }
};

/**
 * Click context handed to a panel's onClick handler.
 */
export type UiClickContext = {
  emitAction: (action: ActionExpression, args?: Record<string, unknown>) => void;
  cursor?: { getX: () => number; getY: () => number };
  [key: string]: unknown;
};

/**
 * Binding for a ui.field node: reads a single field of an entity and renders
 * it as the field's text/number value.
 */
export type UiFieldBinding = {
  entity: EntityExpression | Entity;
  map: "number" | "text";
  name: string;
  fallback?: string;
  align?: AlignOption;
};

/**
 * Options for a ui.progress node: a progress bar that paints the
 * `progress.fill` texture tiled along the fill orientation, clipped to the
 * current value fraction (the last tile is cropped at the fill boundary). The
 * value is always entity-bound: a `progress.value` lambda selects the
 * entity's number field, normalized across the [min, max] range and quantized
 * into `steps` (the node stores an integer 0..steps). Each of fill/track is a
 * static archive PNG path or an animation reference (the panel-background
 * shape) whose frames advance per runtime elapsed time unit. The progress data
 * binding and presentation live under the `progress` key.
 */
export type UiProgressOptions = UiPanelOptions & {
  /** The progress bar's data binding and presentation. */
  progress: {
    /** Tiled fill texture: an archive PNG path or an animation reference. Required. */
    fill: UiBackground;
    /** Tiled track texture behind the fill (same resolution as fill). Optional. */
    track?: UiBackground;
    /** Fill orientation: "horizontal" fills left-to-right (default), "vertical" fills top-to-bottom. */
    orientation?: "horizontal" | "vertical";
    /**
     * Number of discrete fill steps the bar is quantized into (default 100). The
     * engine stores `round(fraction * steps)` as the node value and the renderer
     * divides by `steps` to recover the fill fraction.
     */
    steps?: number;
    /**
     * Lower bound of the value's range (default 0 when omitted). A lambda that
     * selects a field from the bound entity's number map (re-read every tick):
     * `(entity) => entity.number_map.get(key).orElse(...)`. The `.orElse` default
     * is used when the field is absent on the entity.
     */
    min?: (entity: EntityExpression) => NumberExpression;
    /**
     * Upper bound of the value's range. A lambda that selects a field from the
     * bound entity's number map (re-read every tick):
     * `(entity) => entity.number_map.get(key).orElse(...)`. The `.orElse` default
     * is used when the field is absent on the entity. Required.
     */
    max: (entity: EntityExpression) => NumberExpression;
    /** Entity to bind. Required. */
    entity: EntityExpression;
    /**
     * Selects the entity's number field to read. A lambda that uses the
     * EntityExpression api to pick the field from the number map and returns a
     * `NumberExpression`, e.g.
     * `(entity) => entity.number_map.get("hp").orElse(...)`. Returning a
     * `NumberExpression` (rather than the raw `MaybeExpression`) forces the
     * `Maybe` to be resolved via `orElse`. The selected field is re-read from
     * the entity's number data every tick; its `.orElse` default is the raw
     * value used when the field is absent. Required.
     */
    value: (entity: EntityExpression) => NumberExpression;
  };
};

/**
 * Arguments for ui.containerView: a sized view of a container where each
 * entity is placed by its container coordinates (getX/getY/span) and the view
 * is re-laid-out every tick so panels track movement. The view's total
 * width/height live in `panel` (host-validated as positive).
 */
export type UiContainerViewOptions = UiPanelOptions & {
  container: ContainerExpression;
  /**
   * The declared areas to draw as outlines, one entry per area. Each entry
   * selects a named area and the outline style for it. When omitted, the
   * entity's first declared area (id ascending) is drawn with default styling
   * (red outline / green body / thickness 2).
   */
  area?: {
    /** The entity's declared area name (a key in the entity's areaMap). */
    name: string;
    /** Outline line color as [r, g, b, a] in 0..1. */
    color: [number, number, number, number];
    /** Body fill color as [r, g, b, a] in 0..1. */
    bodyColor: [number, number, number, number];
    /** Outline line thickness in logical units. */
    thickness: number;
  }[];
};

/**
 * Arguments for ui.sectorGrid: a sized top-down view of a runtime sector grid.
 * The engine materializes one cell window per footprint square (positioned by
 * cellSize, rendered by `render`) and one headless arrow per portal (drawn by
 * the engine as a straight shaft, width portalThickness), re-resolved every
 * tick. Portals are engine-owned; the render is never invoked for them.
 */
export type UiSectorGridOptions = UiPanelOptions & {
  /** The runtime sector grid (a {@link GridExpression} reference, declared via world.sectorGrid). */
  grid: GridExpression;
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
  container: ContainerExpression;
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
  /** A progress bar: tiled fill clipped to a 0..1 fraction (static or entity-bound). */
  progress: (id: string, options: UiProgressOptions) => UiNodeId;

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
