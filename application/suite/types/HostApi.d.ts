import {ConditionReadOnlyExpressionApi} from "./primitives/conditionExpression";
import {NumberReadOnlyExpressionApi} from "./primitives/numberExpression";
import {StringReadOnlyExpression, StringReadOnlyExpressionApi} from "./primitives/stringExpression";
import {StringBlueprintApi} from "./primitives/stringBlueprint";
import {NumberBlueprintApi} from "./primitives/numberBlueprint";
import {EntityBlueprintApi} from "./primitives/entityBlueprint";
import {MaybeReadOnlyExpressionApi} from "./primitives/maybeExpression";
import {TemporalExpressionApi} from "./primitives/temporalExpression";
import {SetEffectFunction, EffectReadOnlyExpression} from "./dataExpression/Effect";
import {EntityDeclaration, EntityId, EntityReadOnlyExpression} from "./dataExpression/Entity";
import {SectorSide, GridReadOnlyExpression} from "./dataExpression/Sector";
import {NumberMapReadOnlyExpressionApi} from "./numberMap";
import {TextMapReadOnlyExpressionApi} from "./textMap";
import {SetActionFunction} from "./dataExpression/action";
import {UiApi} from "./ui/Ui";
import {SpriteResource} from "./texture/SpriteResource";
import {SpriteMap, MapLayerBinding} from "./texture/SpriteMap";
import {SetAnimationFunction, GetAnimationFunction} from "./dataExpression/animation/AnimationRegistration";
import {AutonomyApi} from "./autonomy";
import {BehaviorReadOnlyExpression} from "./dataExpression/behavior";

/**
 * World/sector namespace: `world.sectorGrid(id)` declares a named grid.
 * Sectors attach to a grid via an entity's optional `sector` field.
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
  /** Runtime APIs for entities, effects, actions, and events. */
  runtime: {
    condition: ConditionReadOnlyExpressionApi,
    number: NumberReadOnlyExpressionApi,
    string: StringReadOnlyExpressionApi,
    stringBlueprint: StringBlueprintApi,
    numberBlueprint: NumberBlueprintApi,
    entityBlueprint: EntityBlueprintApi,
    maybe: MaybeReadOnlyExpressionApi,
    temporal: TemporalExpressionApi,

    numberMap: NumberMapReadOnlyExpressionApi,
    textMap: TextMapReadOnlyExpressionApi,

    /** Declare an entity (or group, when entityListMap is present). */
    setEntity: (declaration: EntityDeclaration) => EntityReadOnlyExpression;

    /**
     * Links two sector openings into an explicit portal. Each opening names its
     * sector entity, its local cell `[x, y]`, and the boundary side (`"N"|"E"|"S"|"W"`).
     */
    linkOpening: (
      a: { sector: EntityId, cell: [number, number], side: SectorSide },
      b: { sector: EntityId, cell: [number, number], side: SectorSide },
    ) => void;

    /** Sets (creates or updates) a named behavior (priority/utility policy) for entities to attach. */
    setBehavior: (arguments: {
      name: StringReadOnlyExpression;
      priority?: Array<{
        label?: string;
        condition?: (entity: EntityReadOnlyExpression) => any;
        utility?: Array<{
          label?: string;
          score?: (entity: EntityReadOnlyExpression) => any;
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

    /** Emits a registered event (an {@link EffectReadOnlyExpression} reference). The `arguments` must match the effect's declared `prepare` input. */
    emitEvent: <Input, Output>(effect: EffectReadOnlyExpression<Input, Output>, arguments: Input) => void
    /** Logs a string message to the runtime log. */
    log:(string:string) => void;

    /** Registers a reactive autonomy state machine on an entity. */
    setAutonomy: AutonomyApi["setAutonomy"];
  }
}
