import {ConditionExpressionApi} from "./primitives/conditionExpression";
import {NumberExpressionApi} from "./primitives/numberExpression";
import {StringExpression, StringExpressionApi} from "./primitives/stringExpression";
import {MaybeExpressionApi} from "./primitives/maybeExpression";
import {TemporalExpressionApi} from "./primitives/temporalExpression";
import {SetEffectFunction} from "./Effect";
import {EntityExpressionApi} from "./Entity";
import {ContainerExpressionApi, ContainerCreationArguments, SectorSide} from "./Contaier";
import {NumberMapExpressionApi} from "./numberMap";
import {TextMapExpressionApi} from "./textMap";
import {SetActionFunction} from "./action";
import {RegisterPanelFunction} from "./ui/Panel";
import {UiApi} from "./ui/Ui";
import {SpriteResource} from "./texture/SpriteResource";
import {SpriteMap, MapLayerBinding} from "./texture/SpriteMap";
import {EntityCreationArguments} from "./Entity";
import {SetAnimationFunction, GetAnimationFunction} from "./animation/AnimationRegistration";
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

    /** Sets (creates or updates) an entity by id, including its field maps. */
    setEntity: (arguments: { id: string | any } & EntityCreationArguments & { behavior?: any }) => void;

    /** Sets (creates or updates) a container by id, including its geometry and members. */
    setContainer: (arguments: { id: string | any } & ContainerCreationArguments) => void;

    /**
     * Links two sector openings into an explicit portal. Each opening names its
     * container, its local cell `[x, y]`, and the boundary side (`"N"|"E"|"S"|"W"`).
     */
    linkOpening: (
      a: { container: string | any, cell: [number, number], side: SectorSide },
      b: { container: string | any, cell: [number, number], side: SectorSide },
    ) => void;

    /** Sets (creates or updates) a named behavior (priority/utility policy) for entities to attach. */
    setBehavior: (arguments: {
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

    /** Sets (creates or updates) an effect handler in the runtime. */
    setEffect: SetEffectFunction,
    /** Sets (creates or updates) an action handler in the runtime. */
    setAction: SetActionFunction,
    /** Sets (creates or updates) an animation with the given name and frame definitions. */
    setAnimation: SetAnimationFunction,
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