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
import {TextureApi} from "./texture/TextureApi";
import {SpriteResource} from "./texture/SpriteResource";
import {EntityCreationArguments} from "./Entity";
import {RegisterAnimationFunction, GetAnimationFunction} from "./animation/AnimationRegistration";

export type HostApi = {
  ui: {
    registerPanel: RegisterPanelFunction,
    texture: TextureApi,
    getSpritePNG: (path: string) => SpriteResource,
    getAnimation: GetAnimationFunction,
  },
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

    setEntity: (entityId: StringExpression, arguments: EntityCreationArguments) => void;

    setContainer: (containerId: StringExpression, arguments: ContainerCreationArguments) => void;

    registerEffect: RegisterEffectFunction,
    registerAction: RegisterActionFunction,
    registerAnimation: RegisterAnimationFunction,
    getAnimation: GetAnimationFunction,

    emitEvent: <T>(eventName: string, arguments: T) => void
    log:(string:string) => void;
  }
}