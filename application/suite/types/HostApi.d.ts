import {ConditionExpressionApi} from "./primitives/conditionExpression";
import {NumberExpressionApi} from "./primitives/numberExpression";
import {StringExpression, StringExpressionApi} from "./primitives/stringExpression";
import {MaybeExpressionApi} from "./primitives/maybeExpression";
import {TemporalExpressionApi} from "./primitives/temporalExpression";
import {RegisterEffectFunction} from "./Effect";
import {EntityExpressionApi} from "./Entity";
import {ContainerExpressionApi, ContainerCreationArguments} from "./Contaier";
import {NumberMapExpressionApi} from "./numberMap";
import {TextMapExpressionApi} from "./textMap";
import {RegisterActionFunction} from "./action";
import {RegisterPanelFunction} from "./ui/Panel";
import {SpriteResource} from "./texture/SpriteResource";
import {SpriteMap, MapLayerBinding} from "./texture/SpriteMap";
import {EntityCreationArguments} from "./Entity";
import {RegisterAnimationFunction, GetAnimationFunction, AnimationRegistrationArguments} from "./animation/AnimationRegistration";
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
    /** Returns the animation registration for the given name and duration configuration. */
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

    numberMap: NumberMapExpressionApi,
    textMap: TextMapExpressionApi,

    entity: EntityExpressionApi,
    container: ContainerExpressionApi,

    /** Sets entity fields (numberMap, textMap) by entity ID. */
    setEntity: (entityId: StringExpression, arguments: EntityCreationArguments) => void;

    /** Sets container fields by container ID. */
    setContainer: (containerId: StringExpression, arguments: ContainerCreationArguments) => void;

    /** Registers an effect handler in the runtime. */
    registerEffect: RegisterEffectFunction,
    /** Registers an action handler in the runtime. */
    registerAction: RegisterActionFunction,
    /** Registers an animation with the given name and frame definitions. */
    registerAnimation: RegisterAnimationFunction,
    /** Returns the animation registration for the given name and duration configuration. */
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
  background?: string | AnimationRegistrationArguments | { name: string; duration?: number; loop?: boolean };
  align?: AlignOption;
  onHover?: {
    texture?: string;
    background?: string;
    thickness?: number;
    emitAction?: string;
    stopPropagation?: boolean;
  };
  onClick?: string;
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
        columns?: Array<{ min?: number; max?: number; weight?: number; align?: "start" | "end" }>;
        rows?: Array<{ min?: number; max?: number; weight?: number; align?: "start" | "end" }>;
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