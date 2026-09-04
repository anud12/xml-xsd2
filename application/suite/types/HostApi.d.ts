import {ConditionExpressionApi} from "./primitives/conditionExpression";
import {NumberExpressionApi} from "./primitives/numberExpression";
import {StringExpression, MutableStringExpression, StringExpressionApi} from "./primitives/stringExpression";

import {MaybeExpressionApi} from "./primitives/maybeExpression";
import {TemporalExpressionApi} from "./primitives/temporalExpression";
import {RegisterEffectFunction} from "./Effect";
import {EntityExpressionApi} from "./Entity";
import {ContainerExpressionApi, ContainerCreationArguments} from "./Contaier";
import {NumberMapApi} from "./numberMap";
import {TextMapApi} from "./textMap";
import {RegisterActionFunction} from "./action";
import {RegisterPanelFunction} from "./ui/Panel";
import {SpriteResource} from "./texture/SpriteResource";
import {SpriteMap, MapLayerBinding} from "./texture/SpriteMap";
import {EntityCreationArguments} from "./Entity";
import {RegisterAnimationFunction, GetAnimationFunction, AnimationRegistrationArguments, AnimationBackground} from "./animation/AnimationRegistration";
import {AutonomyApi} from "./autonomy";
import {BehaviorApi, BehaviorReference} from "./behavior";
import {AlignOption} from "./ui/Panel";

/**
 * The top-level host API surface exposed to modules.
 */
export type HostApi = {
  /** UI-related APIs for panels, textures, and animations. */
  ui: {
    /**
     * Legacy full-options panel registration. Not provided by the C# Jint
     * host (which exposes panel/text/field/container instead, with window
     * and div as aliases of panel); the declaration stays because the
     * restricted suite fixture
     * features/stage2/panel_initialization/offset/index.js still calls it.
     */
    registerPanel: RegisterPanelFunction,
    /** Creates a sprite map from a TIFF file with layer-to-texture bindings. */
    spriteMapTIFF: (mapPath: string, layers: MapLayerBinding[]) => SpriteMap,
    /** Returns a sprite resource reference for the given PNG file path. */
    getSpritePNG: (path: string) => SpriteResource,
    /** Returns the animation registration for the given name. */
    getAnimation: GetAnimationFunction,
    /**
     * Creates a panel node: a positioned, backgrounded, interactive surface
     * and/or a flow layout container for its children. Returns its id.
     * A panel with neither a surface option (x/y/width/height/background/
     * onHover/onClick/anchor) nor a layout is a bare grouping node.
     */
    panel: (id: string, options: UiPanelOptions, children?: any[]) => string,
    /** Alias of `panel` that always declares a surface; returns its id. */
    window: (id: string, options: UiWindowOptions, children?: any[]) => string,
    /** Creates a constant text node; returns its id. */
    text: (id: string, content: string) => string,
    /** Creates an entity-bound field node; returns its id. */
    field: (id: string, options: UiFieldOptions) => string,
    /** Alias of `panel` limited to a layout container; returns its id. */
    div: (id: string, options: UiDivOptions, children?: any[]) => string,
    /** Creates a container list-view node rendered from a per-entity template. */
    container: (id: string, options: UiContainerOptions, template: (entity: any, index: number) => any[]) => any,
  },
  /** Runtime APIs for entities, containers, effects, actions, and events. */
  runtime: {
    condition: ConditionExpressionApi,
    number: NumberExpressionApi,
    string: StringExpressionApi,
    maybe: MaybeExpressionApi,
    temporal: TemporalExpressionApi,

    numberMap: NumberMapApi,
    textMap: TextMapApi,

    entity: EntityExpressionApi,
    container: ContainerExpressionApi,

    /** Sets entity fields (numberMap, textMap) by entity ID. Same payload shape as `ApplyContext.createEntity`. */
    setEntity: (entityId: MutableStringExpression | string, arguments: EntityCreationArguments) => void;

    /** Sets container fields by container ID. */
    setContainer: (containerId: StringExpression, arguments: ContainerCreationArguments) => void;

    /** Registers an entity (id plus optional field maps) with the runtime. */
    registerEntity: (entity: { id: string } & EntityCreationArguments) => void;

    /** Registers a container (id plus optional fields) with the runtime. */
    registerContainer: (container: { id: string } & ContainerCreationArguments) => void;

    /** Registers an effect handler in the runtime. */
    registerEffect: RegisterEffectFunction,
    /** Registers an action handler in the runtime. */
    registerAction: RegisterActionFunction,
    /** Registers an animation with the given name and frame definitions. */
    registerAnimation: RegisterAnimationFunction,
    /** Returns the animation registration for the given name. */
    getAnimation: GetAnimationFunction,

    /** Emits a named event with arbitrary payload. */
    emitEvent: <T>(eventName: string, arguments: T) => void
    /** Logs a string message to the runtime log. */
    log:(string:string) => void;

    /** Registers a reactive autonomy state machine on an entity. */
    setAutonomy: AutonomyApi["setAutonomy"];

    /** Registers a behavior graph and returns a reference attachable via setEntity. */
    registerBehavior: BehaviorApi["registerBehavior"];
  }
}

/** Options accepted by the high-level `hostApi.ui.window` builder. */
export type UiWindowOptions = {
  width?: number;
  height?: number;
  x?: number;
  y?: number;
  anchor?: string | { x: number; y: number };
  /** Background animation; obtain via `hostApi.ui.getAnimation`. */
  background?: AnimationBackground;
  align?: AlignOption;
  onHover?: {
    /** Hover outline animation; obtain via `hostApi.ui.getAnimation`. */
    texture?: AnimationRegistrationArguments;
    /** Hover background-swap animation; obtain via `hostApi.ui.getAnimation`. */
    background?: AnimationBackground;
    thickness?: number;
    emitAction?: string;
    stopPropagation?: boolean;
  };
  /**
   * Click handler. The callback runs once at panel-definition time and
   * aggregates an execution plan onto `ctx`; on click each step's action is
   * emitted with its args (cursor symbols resolved to the local grid cell).
   */
  onClick?: (ctx: UiClickContext) => void;
  /** Nine-patch border frame drawn around the panel; the center is hidden. */
  border?: UiBorderOptions;
};

/**
 * Context handed to a panel's `onClick` callback. Call `emitAction` to append
 * a step to the plan; `cursor` symbols resolve at click time to the local
 * grid cell (column/row), or 0 for non-grid panels.
 */
export interface UiClickContext {
  /** Appends an action step. `args` may include `cursor` numberExpressions. */
  emitAction: (name: string, args?: Record<string, unknown>) => void;
  cursor: {
    /** The clicked cell's column index (0 for non-grid panels). */
    getX: () => number;
    /** The clicked cell's row index (0 for non-grid panels). */
    getY: () => number;
  };
}

/** Nine-patch border options: the center region is never drawn. */
export type UiBorderOptions = {
  /** Patch margin (border thickness) in px, applied to all four sides. Defaults to 1. */
  width?: number;
  /** Border animation (first frame is used); obtain via `hostApi.ui.getAnimation`. */
  texture: AnimationRegistrationArguments;
};

/** Options accepted by the high-level `hostApi.ui.field` builder. */
export type UiFieldOptions = {
  entity: string;
  map: "text" | "number";
  name: string;
  fallback?: string;
  align?: AlignOption;
};

/**
 * Options accepted by the unified `hostApi.ui.panel` builder: the surface
 * options of `window` and/or the layout of `div`. A panel declares both to
 * be a backgrounded, positioned container that also flows its children.
 */
export type UiPanelOptions = UiWindowOptions & {
  layout?:
    | "row"
    | "column"
    | number
    | {
        rowFirst?: boolean;
        reverse?: boolean;
        columns?: readonly { min?: number; max?: number; weight?: number; scale?: number; align?: "start" | "end" }[];
        rows?: readonly { min?: number; max?: number; weight?: number; scale?: number; align?: "start" | "end" }[];
        gap?: { row?: number; column?: number };
      };
};

/** Options accepted by the high-level `hostApi.ui.div` builder (alias of `panel`). */
export type UiDivOptions = {
  layout: "row" | "column";
};

/** Options accepted by the high-level `hostApi.ui.container` builder. */
export type UiContainerOptions = {
  container: string;
  vertical?: boolean;
};