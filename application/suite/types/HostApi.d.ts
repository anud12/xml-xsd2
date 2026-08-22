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
import {RegisterAnimationFunction, GetAnimationFunction} from "./animation/AnimationRegistration";
import {AutonomyApi} from "./autonomy";

/**
 * The top-level host API surface exposed to modules.
 */
export type HostApi = {
  /** UI-related APIs for panels, textures, and animations. */
  ui: {
    /** Registers a new panel with the UI host. */
    registerPanel: RegisterPanelFunction,
    /** Creates a sprite map from a TIFF file with layer-to-texture bindings. */
    spriteMapTIFF: (mapPath: string, layers: MapLayerBinding[]) => SpriteMap,
    /** Returns a sprite resource reference for the given PNG file path. */
    getSpritePNG: (path: string) => SpriteResource,
    /** Returns the animation registration for the given name and duration configuration. */
    getAnimation: GetAnimationFunction,
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

    /** Creates and registers a reactive autonomy state machine. */
    autonomy: AutonomyApi["autonomy"];
    /** Attaches an autonomy to an entity. */
    setAutonomy: AutonomyApi["setAutonomy"];
  }
}