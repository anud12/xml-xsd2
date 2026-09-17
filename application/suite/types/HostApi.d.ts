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
import {UiApi} from "./ui/Ui";
import {SpriteResource} from "./texture/SpriteResource";
import {SpriteMap, MapLayerBinding} from "./texture/SpriteMap";
import {EntityCreationArguments} from "./Entity";
import {RegisterAnimationFunction, GetAnimationFunction} from "./animation/AnimationRegistration";
import {AutonomyApi} from "./autonomy";

/**
 * World/sector namespace: `world.sectorGrid(id)` declares a named grid.
 * Sectors attach to a grid via a container's optional `sector` field.
 */
export type WorldApi = {
  /** Declares a named sector grid and returns a reference to it. */
  sectorGrid: (id: StringExpression) => { id: string },
}

/**
 * The top-level host API surface exposed to modules.
 */
export type HostApi = {
  /** UI-related APIs for panels, textures, and animations. */
  ui: UiApi,
  /** World/sector namespace for declaring named sector grids. */
  world: WorldApi,
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
    setEntity: (entityId: StringExpression, arguments: EntityCreationArguments & { behavior?: any }) => void;

    /** Sets container fields by container ID. */
    setContainer: (containerId: StringExpression, arguments: ContainerCreationArguments) => void;

    /**
     * Links two sector openings into an explicit portal. Each opening names its
     * container, its local cell `[x, y]`, and the boundary side (`"N"|"E"|"S"|"W"`).
     */
    linkOpening: (
      a: { container: string | any, cell: [number, number], side: string },
      b: { container: string | any, cell: [number, number], side: string },
    ) => void;

    /** Registers (creates or updates) an entity by id, including its field maps. */
    registerEntity: (arguments: { id: string | any } & EntityCreationArguments) => void;

    /** Registers (creates or updates) a container by id, including its geometry and members. */
    registerContainer: (arguments: { id: string | any } & ContainerCreationArguments) => void;

    /** Registers a named behavior (priority/utility policy) for entities to attach. */
    registerBehavior: (arguments: {
      name: string | any;
      priority?: Array<{
        label?: string;
        condition?: (ctx?: any) => any;
        utility?: Array<{
          label?: string;
          score?: (ctx?: any) => any;
          do?: (ctx: any) => any;
        }>;
      }>;
    }) => { name: string; [key: string]: any };

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
  }
}