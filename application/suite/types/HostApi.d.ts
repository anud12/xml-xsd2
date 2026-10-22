import {ConditionExpressionApi} from "./primitives/conditionExpression";
import {NumberExpressionApi} from "./primitives/numberExpression";
import {StringExpression, StringExpressionApi} from "./primitives/stringExpression";
import {StringGeneratorApi} from "./primitives/stringGenerator";
import {MaybeExpressionApi} from "./primitives/maybeExpression";
import {TemporalExpressionApi} from "./primitives/temporalExpression";
import {SetEffectFunction, EffectExpression} from "./dataExpression/Effect";
import {EntityExpressionApi, EntityExpression} from "./dataExpression/Entity";
import {ContainerExpressionApi, ContainerExpression, ContainerCreationArguments, SectorSide, GridExpression} from "./dataExpression/Contaier";
import {NumberMapExpressionApi} from "./numberMap";
import {TextMapExpressionApi} from "./textMap";
import {SetActionFunction} from "./dataExpression/action";
import {UiApi} from "./ui/Ui";
import {SpriteResource} from "./texture/SpriteResource";
import {SpriteMap, MapLayerBinding} from "./texture/SpriteMap";
import {EntityCreationArguments} from "./dataExpression/Entity";
import {SetAnimationFunction, GetAnimationFunction} from "./dataExpression/animation/AnimationRegistration";
import {AutonomyApi} from "./autonomy";
import {BehaviorExpression} from "./dataExpression/behavior";

/**
 * World/sector namespace: `world.sectorGrid(id)` declares a named grid.
 * Sectors attach to a grid via a container's optional `sector` field.
 */
export type WorldApi = {
  /** Declares a named sector grid and returns a {@link GridExpression} reference to it. */
  sectorGrid: (id: StringExpression) => GridExpression,
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
    stringGenerator: StringGeneratorApi,
    maybe: MaybeExpressionApi,
    temporal: TemporalExpressionApi,

    numberMap: NumberMapExpressionApi,
    textMap: TextMapExpressionApi,

    entity: EntityExpressionApi,
    container: ContainerExpressionApi,

    /**
     * Sets (creates or updates) an entity by id, including its field maps.
     * Returns an {@link EntityExpression} reference to the entity, usable
     * anywhere an entity id is expected (e.g. `setContainer` `entities`).
     */
    setEntity: (arguments: { id: string | any } & EntityCreationArguments & { behavior?: BehaviorExpression }) => EntityExpression;

    /**
     * Sets (creates or updates) a container by id, including its geometry and
     * members. Returns a {@link ContainerExpression} reference to the container.
     */
    setContainer: (arguments: { id: string | any } & ContainerCreationArguments) => ContainerExpression;

    /**
     * Links two sector openings into an explicit portal. Each opening names its
     * container, its local cell `[x, y]`, and the boundary side (`"N"|"E"|"S"|"W"`).
     */
    linkOpening: (
      a: { container: ContainerExpression, cell: [number, number], side: SectorSide },
      b: { container: ContainerExpression, cell: [number, number], side: SectorSide },
    ) => void;

    /** Sets (creates or updates) a named behavior (priority/utility policy) for entities to attach. */
    setBehavior: (arguments: {
      name: StringExpression;
      priority?: Array<{
        label?: string;
        condition?: (ctx?: any) => any;
        utility?: Array<{
          label?: string;
          score?: (ctx?: any) => any;
          do?: (ctx: any) => any;
        }>;
      }>;
    }) => BehaviorExpression;

    /** Sets (creates or updates) an effect handler in the runtime. */
    setEffect: SetEffectFunction,
    /** Sets (creates or updates) an action handler in the runtime. */
    setAction: SetActionFunction,
    /** Sets (creates or updates) an animation with the given name and frame definitions. */
    setAnimation: SetAnimationFunction,
    /** Returns the animation registration for the given name and duration configuration. */
    getAnimation: GetAnimationFunction,

    /** Emits a registered event (an {@link EffectExpression} reference) with arbitrary payload. */
    emitEvent: <T>(effect: EffectExpression, arguments: T) => void
    /** Logs a string message to the runtime log. */
    log:(string:string) => void;

    /** Registers a reactive autonomy state machine on an entity. */
    setAutonomy: AutonomyApi["setAutonomy"];
  }
}