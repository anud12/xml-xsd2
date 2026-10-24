import {NumberReadOnlyExpression} from "../primitives/numberExpression";
import {Entity, EntityReadOnlyExpression} from "../dataExpression/Entity";
import {AnimationReadOnlyExpression, GetAnimationFunction} from "../dataExpression/animation/AnimationRegistration";
import {SpriteResource} from "../texture/SpriteResource";
import {SpriteMap, MapLayerBinding} from "../texture/SpriteMap";
import {ContainerReadOnlyExpression, GridReadOnlyExpression} from "../dataExpression/Contaier";
import {ActionReadOnlyExpression} from "../dataExpression/action/no-input-action";

/**
 * A raw node id string.
 */
export type UiNodeId = string;

/**
 * Configuration for `ui.setFont`: registers a font file from the archive and
 * returns a reference handle usable on `text`/`field` nodes.
 */
export type UiFontConfig = {
  /** Archive-relative path to the .ttf file. */
  file: string;
  /** Font size in logical units. */
  size: number;
  /** Text color as a CSS hex string, e.g. "#FF0000". */
  color: string;
  /** Font style flags. */
  style: ("bold" | "italic" | "underline" | "line-through")[];
};

/**
 * The handle returned by `ui.setFont`. Pass it (or its name string) as the
 * `fontRef` argument to `ui.text` / `ui.field`.
 */
export type UiFontReference = {
  readonly name: string;
  readonly value: string;
  toString(): string;
  toJSON(): string;
};

/**
 * The *Expression handle a ui factory returns for the node it registered —
 * the same object shape as the handles returned by `setAnimation` or
 * `world.sectorGrid`. It carries the node id on `.id`/`.value` and
 * stringifies back to it (`toString`/`toJSON`), so it flows into any
 * id-accepting slot (a parent's `children`, `binding.entity`,
 * `args.container`) or is used where a plain id string is expected.
 */
export type UiNodeExpression = {
  readonly id: string;
  readonly value: string;
  toString(): string;
  toJSON(): string;
};

/**
 * A reference to a node usable anywhere a child is expected: a raw
 * {@link UiNodeId} string or the {@link UiNodeExpression} handle a factory
 * returned. The common child type shared by every parent component.
 */
export type UiChildRef = UiNodeId | UiNodeExpression;

/**
 * A list of children: the `children` parameter of panel/window, and the
 * render return of entityList.
 */
export type UiChildren = UiChildRef[];

/**
 * The *Expression handle returned by the child-list components
 * (`panel`/`window`): the common node handle plus `addChild`, which
 * appends a child to the node's children list after the node was created
 * (the child is a raw id or another expression handle). Returns the same
 * handle, so calls can be chained.
 */
export type UiParentExpression = UiNodeExpression & {
  addChild: (child: UiChildRef) => UiParentExpression;
};

/**
 * Per-axis paint rule: `"stretch"` scales the texture to fill that axis
 * (distorting when the two axes use different scales) or, with
 * `keepAspectRatio`, scales uniformly and clips; `"tile"` repeats the texture
 * at the scale set by the stretch axis (native pixel size when both axes tile).
 */
export type TextureFillAxis = "stretch" | "tile";

/**
 * The paint rule for a texture: one entry per axis. The stretch axis(es) set
 * the scale; the tile axis(es) repeat at that scale. Both tile = native pixel
 * size. `keepAspectRatio` makes the scale uniform (stretch axes clip, tile
 * axes may letterbox) instead of per-axis.
 */
export type TextureFill = {
  width: TextureFillAxis;
  height: TextureFillAxis;
  keepAspectRatio?: boolean;
};

/**
 * 9-slice scaling geometry (en.wikipedia.org/wiki/9-slice_scaling): the slice
 * margins in px. Purely additive to {@link TextureFill}, which remains the
 * paint rule for the edges and the center — `nineSlice` only adds the slice
 * boundaries (the four corners are fixed at `width`×`height`). A frame/border
 * is a nine-slice whose source center is transparent, so the center paints
 * nothing.
 */
export type NineSlice = {
  width: number;
  height: number;
};

/**
 * A texture: what to paint + how to paint it. The single shape every textured
 * ui slot accepts (background, border, hover outline/background, progress
 * fill/track, screen). Replaces the old `UiBackground = AnimationReadOnlyExpression |
 * SpriteResource` union — a still image is a one-frame animation registration
 * and sprite maps are reachable only through animation frames.
 */
export type UiTexture = {
  /** What to paint: a registered animation — the `setAnimation`/`getAnimation`
   * handle, or a `{name}` ref. The engine resolves the registration's
   * frames/duration/loop live every tick. */
  animation: AnimationReadOnlyExpression;
  /** How to paint: required, per axis. */
  fill: TextureFill;
  /** Optional 9-slice geometry. */
  nineSlice?: NineSlice;
};

/**
 * A panel background: the unified {@link UiTexture} shape.
 */
export type UiBackground = UiTexture;

/**
 * Grid layout tracks. Modules use either weighted columns or explicit scale
 * tracks; the host consumes whatever shape the renderer supports.
 */
export type UiTrack = { scale?: number; weight?: number; min?: number; max?: number };

/**
 * Alignment preset options for content inside a panel.
 * Determines where text or numbers are positioned within the panel bounds.
 */
export type AlignOption = "top"
  | "top-left"
  | "top-right"
  | "center"
  | "center-left"
  | "center-right"
  | "bottom"
  | "bottom-left"
  | "bottom-right";

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
  container?: ContainerReadOnlyExpression;
} | "column" | "row";

/**
 * Options for the positioned/decorated ui panel surface (window or division).
 * The panel configuration is nested under a required `panel` key; all of its
 * fields are optional. The positioning/size fields (x/y/width/height/anchor)
 * only take effect on window nodes — divisions are flow containers placed by
 * their parent's layout. The host passes it through as the node's
 * `options.panel` (the renderer reads `panel.*`).
 */
export type UiPanelOptions = {
  panel: {
    x?: number;
    y?: number;
    width?: number;
    height?: number;
    anchor?: "top-left" | "top" | "top-right" | "center-left" | "center" | "center-right" | "bottom-left" | "bottom" | "bottom-right" | { x?: number; y?: number };
    align?: AlignOption;
    background?: UiTexture;
    border?: { texture?: UiTexture; thickness?: number; width?: number; height?: number };
    onHover?: { texture?: UiTexture; background?: UiTexture; thickness?: number; emitAction?: ActionReadOnlyExpression; stopPropagation?: boolean };
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
     * Window title: drawn as a title strip across the top of the window with
     * the text centered, and makes the window draggable by the strip
     * (native-window style). Only honored on window nodes. On a `resizable`
     * window the edge/corner resize hit zones keep precedence over the strip.
     */
    title?: string;
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
  emitAction: (action: ActionReadOnlyExpression, args?: Record<string, unknown>) => void;
  cursor?: { getX: () => number; getY: () => number };
  [key: string]: unknown;
};

/**
 * Binding for a ui.field node: reads a single field of an entity and renders
 * it as the field's text/number value.
 */
export type UiFieldBinding = {
  entity: EntityReadOnlyExpression | Entity;
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
    fill: UiTexture;
    /** Tiled track texture behind the fill (same resolution as fill). Optional. */
    track?: UiTexture;
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
     * is used when the field is absent on the entity. Alternatively, a lambda
     * may return a constant NumberReadOnlyExpression (e.g. `(entity) => number.of(0)`),
     * which is used as-is with no per-tick re-read.
     */
    min?: (entity: EntityReadOnlyExpression) => NumberReadOnlyExpression;
    /**
     * Upper bound of the value's range. A lambda that selects a field from the
     * bound entity's number map (re-read every tick):
     * `(entity) => entity.number_map.get(key).orElse(...)`. The `.orElse` default
     * is used when the field is absent on the entity. Alternatively, a lambda
     * may return a constant NumberReadOnlyExpression (e.g. `(entity) => number.of(10)`),
     * which is used as-is with no per-tick re-read. Required.
     */
    max: (entity: EntityReadOnlyExpression) => NumberReadOnlyExpression;
    /** Entity to bind. Required. */
    entity: EntityReadOnlyExpression;
    /**
     * Selects the entity's number field to read. A lambda that uses the
     * EntityReadOnlyExpression api to pick the field from the number map and returns a
     * `NumberReadOnlyExpression`, e.g.
     * `(entity) => entity.number_map.get("hp").orElse(...)`. Returning a
     * `NumberReadOnlyExpression` (rather than the raw `MaybeReadOnlyExpression`) forces the
     * `Maybe` to be resolved via `orElse`. The selected field is re-read from
     * the entity's number data every tick; its `.orElse` default is the raw
     * value used when the field is absent. Alternatively, a lambda may return
     * a constant NumberReadOnlyExpression (e.g. `(entity) => number.of(3)`), which is
     * used as-is with no per-tick re-read. Required.
     */
    value: (entity: EntityReadOnlyExpression) => NumberReadOnlyExpression;
  };
};

/**
 * Arguments for ui.containerView: a sized view of a container where each
 * entity is placed by its container coordinates (getX/getY/span) and the view
 * is re-laid-out every tick so panels track movement. The view's total
 * width/height live in `panel` (host-validated as positive).
 */
export type UiContainerViewOptions = UiPanelOptions & {
  container: ContainerReadOnlyExpression;
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
  /** The runtime sector grid (a {@link GridReadOnlyExpression} reference, declared via world.sectorGrid). */
  grid: GridReadOnlyExpression;
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
  container: ContainerReadOnlyExpression;
  vertical?: boolean;
};

/**
 * Options for the global full-screen screen (ui.setScreen): the panel api
 * minus positioning/sizing — the renderer always fills the viewport, so the
 * sizing fields (x/y/width/height/anchor) do not exist here. The
 * presentation is nested under a required `screen` key. For now only
 * `background` is honored (the same {@link UiTexture} as a panel).
 */
export type UiScreenOptions = {
  screen: {
    background?: UiTexture;
  }
};

/**
 * The common parent type for every ui component that hosts children. It
 * defines the children channel they all share: the `children` parameter is
 * either an explicit {@link UiChildren} list (panel/window) or a render
 * lambda that materializes children per item (entityList: a list per entity;
 * containerView/sectorGrid: one per item). Every child-hosting factory in
 * {@link UiApi} extends it; all return the registered node's
 * {@link UiNodeExpression} handle (the child-list components return the
 * richer {@link UiParentExpression}).
 */
export interface UiParentComponent<Options, ChildSource, Result extends UiNodeExpression = UiNodeExpression> {
  (id: string, options: Options, children: ChildSource): Result;
}

/** A plain flow container (a division): laid out by its parent's box/grid. A standalone one is a no-op (never added to the view). */
export interface UiPanelComponent extends UiParentComponent<UiPanelOptions, UiChildren, UiParentExpression> {
  (id: string, options?: UiPanelOptions, children?: UiChildren): UiParentExpression;
}

/** The only factory that creates a window — the only top-level surface (positioned by x/y/anchor, explicitly sized or sized to content). */
export interface UiWindowComponent extends UiParentComponent<UiPanelOptions, UiChildren, UiParentExpression> {
  (id: string, options?: UiPanelOptions, children?: UiChildren): UiParentExpression;
}

/**
 * The global full-screen screen (a top-level surface like a window, always
 * filling the viewport). Each call replaces the current screen — there is at
 * most one visible screen at a time.
 */
export interface UiScreenComponent {
  (options?: UiScreenOptions, children?: UiChildren): UiParentExpression;
}

/** A flow list: one child per entity in the container (render returns an array of node ids or handles). Must be nested under a window. */
export interface UiEntityListComponent
  extends UiParentComponent<UiEntityListOptions, (entity: Entity) => UiChildren> {}

/** A sized view of a container (a window): one panel per entity placed on its cell, re-resolved each tick. */
export interface UiContainerViewComponent
  extends UiParentComponent<UiContainerViewOptions, (entity: Entity) => UiChildRef> {}

/** A top-down view of a sector grid (a window): one cell per footprint square (rendered by `render`) + one engine-drawn headless arrow per portal, all positioned by the engine each tick. */
export interface UiSectorGridComponent
  extends UiParentComponent<UiSectorGridOptions, (item: UiSectorGridItem) => UiChildRef> {}

/**
 * The module-facing ui surface: node factories plus texture/animation helpers.
 *
 * View invariant: only `window` nodes and the screen (`setScreen`) can be
 * top-level. Every other node (panel/division, text, field, progress, image,
 * entityList) must be nested under a window or the screen — directly or
 * through divisions. Creating a standalone node (one no surface ever
 * reaches) is a no-op: it registers, but it is never added to the view.
 *
 * Every factory returns the registered node's {@link UiNodeExpression} handle,
 * so a node can be built in any order and handed to a parent's `children`
 * (which also accept raw {@link UiNodeId} strings). The child-hosting
 * factories (panel, window, setScreen, entityList, containerView, sectorGrid)
 * all return a {@link UiParentExpression}, whose `addChild` appends a child
 * to the node's children list after the node was created.
 */
export type UiApi = {
  panel: UiPanelComponent;
  window: UiWindowComponent;
  /**
   * The global full-screen screen (always filling the viewport, below app
   * buttons and windows). Each call replaces the current screen. Unlike the
   * node factories it takes no id (the id is managed).
   */
  setScreen: UiScreenComponent;
  /** Register a font from an archive .ttf file; returns a reference handle. */
  setFont: (config: UiFontConfig) => UiFontReference;
  /** A text label: must be nested under a window. */
  text: (id: string, value: string | number, fontRef: UiFontReference) => UiNodeExpression;
  /** A bound field: must be nested under a window. */
  field: (id: string, binding: UiFieldBinding, fontRef: UiFontReference) => UiNodeExpression;
  /** A progress bar: tiled fill clipped to a 0..1 fraction (static or entity-bound). Must be nested under a window. */
  progress: (id: string, options: UiProgressOptions) => UiNodeExpression;

  entityList: UiEntityListComponent;

  containerView: UiContainerViewComponent;s

  sectorGrid: UiSectorGridComponent;

  spriteMapTIFF: (mapPath: string, layers: MapLayerBinding[]) => SpriteMap;
  getSpritePNG: (path: string) => SpriteResource;
  getAnimation: GetAnimationFunction;
};
