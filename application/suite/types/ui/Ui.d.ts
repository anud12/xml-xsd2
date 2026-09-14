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

  spriteMapTIFF: (mapPath: string, layers: MapLayerBinding[]) => SpriteMap;
  getSpritePNG: (path: string) => SpriteResource;
  getAnimation: GetAnimationFunction;
};
