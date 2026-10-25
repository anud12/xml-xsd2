import {ConditionReadOnlyExpressionApi} from "./primitives/conditionExpression";
import {NumberReadOnlyExpressionApi} from "./primitives/numberExpression";
import {StringReadOnlyExpression, StringReadOnlyExpressionApi} from "./primitives/stringExpression";
import {StringGeneratorApi} from "./primitives/stringGenerator";
import {MaybeReadOnlyExpressionApi} from "./primitives/maybeExpression";
import {TemporalExpressionApi} from "./primitives/temporalExpression";
import {SetEffectFunction, EffectReadOnlyExpression} from "./dataExpression/Effect";
import {EntityReadOnlyExpression} from "./dataExpression/Entity";
import {ContainerExpressionApi, ContainerReadOnlyExpression, ContainerCreationArguments, SectorSide, GridReadOnlyExpression} from "./dataExpression/Contaier";
import {NumberMapReadOnlyExpressionApi} from "./numberMap";
import {TextMapReadOnlyExpressionApi} from "./textMap";
import {SetActionFunction} from "./dataExpression/action";
import {UiApi} from "./ui/Ui";
import {SpriteResource} from "./texture/SpriteResource";
import {SpriteMap, MapLayerBinding} from "./texture/SpriteMap";
import {EntityCreationArguments} from "./dataExpression/Entity";
import {SetAnimationFunction, GetAnimationFunction} from "./dataExpression/animation/AnimationRegistration";
import {AutonomyApi} from "./autonomy";
import {BehaviorReadOnlyExpression} from "./dataExpression/behavior";

/**
 * World/sector namespace: `world.sectorGrid(id)` declares a named grid.
 * Sectors attach to a grid via a container's optional `sector` field.
 */
export type WorldApi = {
  /** Declares a named sector grid and returns a {@link GridReadOnlyExpression} reference to it. */
  sectorGrid: (id: StringReadOnlyExpression) => GridReadOnlyExpression,
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
    condition: ConditionReadOnlyExpressionApi,
    number: NumberReadOnlyExpressionApi,
    string: StringReadOnlyExpressionApi,
    stringGenerator: StringGeneratorApi,
    maybe: MaybeReadOnlyExpressionApi,
    temporal: TemporalExpressionApi,

    numberMap: NumberMapReadOnlyExpressionApi,
    textMap: TextMapReadOnlyExpressionApi,

    container: ContainerExpressionApi,

    /**
     * Sets (creates or updates) an entity by id, including its field maps.
     * Returns an {@link EntityReadOnlyExpression} reference to the entity, usable
     * anywhere an entity id is expected (e.g. `setContainer` `entities`).
     */
    setEntity: (arguments: { id: string | any } & EntityCreationArguments & { behavior?: BehaviorReadOnlyExpression }) => EntityReadOnlyExpression;

    /**
     * Sets (creates or updates) a container by id, including its geometry and
     * members. Returns a {@link ContainerReadOnlyExpression} reference to the container.
     */
    setContainer: (arguments: { id: string | any } & ContainerCreationArguments) => ContainerReadOnlyExpression;

    /**
     * Links two sector openings into an explicit portal. Each opening names its
     * container, its local cell `[x, y]`, and the boundary side (`"N"|"E"|"S"|"W"`).
     */
    linkOpening: (
      a: { container: ContainerReadOnlyExpression, cell: [number, number], side: SectorSide },
      b: { container: ContainerReadOnlyExpression, cell: [number, number], side: SectorSide },
    ) => void;

    /** Sets (creates or updates) a named behavior (priority/utility policy) for entities to attach. */
    setBehavior: (arguments: {
      name: StringReadOnlyExpression;
      priority?: Array<{
        label?: string;
        condition?: (ctx?: any) => any;
        utility?: Array<{
          label?: string;
          score?: (ctx?: any) => any;
          do?: (ctx: any) => any;
        }>;
      }>;
    }) => BehaviorReadOnlyExpression;

    /** Sets (creates or updates) an effect handler in the runtime. */
    setEffect: SetEffectFunction,
    /** Sets (creates or updates) an action handler in the runtime. */
    setAction: SetActionFunction,
    /** Sets (creates or updates) an animation with the given name and frame definitions. */
    setAnimation: SetAnimationFunction,
    /** Returns the animation registration for the given name and duration configuration. */
    getAnimation: GetAnimationFunction,

    /** Emits a registered event (an {@link EffectReadOnlyExpression} reference) with arbitrary payload. */
    emitEvent: <T>(effect: EffectReadOnlyExpression, arguments: T) => void
    /** Logs a string message to the runtime log. */
    log:(string:string) => void;

    /** Registers a reactive autonomy state machine on an entity. */
    setAutonomy: AutonomyApi["setAutonomy"];
  }
}