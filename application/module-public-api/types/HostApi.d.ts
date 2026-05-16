import {ConditionExpressionApi} from "./primitives/conditionExpression";
import {NumberExpressionApi} from "./primitives/numberExpression";
import {StringExpression} from "./primitives/stringExpression";
import {MaybeExpressionApi} from "./primitives/maybeExpression";
import {TemporalExpressionApi} from "./primitives/temporalExpression";
import {RegisterEffectFunction} from "./Effect";
import {EntityExpressionApi} from "./Entity";
import {NumberMapExpressionApi} from "./numberMap";
import {TextMapExpressionApi} from "./textMap";
import {RegisterActionFunction} from "./action";
import {RegisterPanelFunction} from "./ui/Panel";
import {TextureApi} from "./texture/TextureApi";
import {EntityCreationArguments} from "./Entity";

export type HostApi = {
  condition: ConditionExpressionApi,
  number: NumberExpressionApi,
  string: StringExpression,
  maybe: MaybeExpressionApi,
  temporal: TemporalExpressionApi,

  numberMap: NumberMapExpressionApi,
  textMap: TextMapExpressionApi,

  entity: EntityExpressionApi,
  container: ConditionExpressionApi,

  setEntity: (entityId: StringExpression, arguments: EntityCreationArguments) => void;

  registerEffect: RegisterEffectFunction,
  registerAction: RegisterActionFunction,

  registerPanel: RegisterPanelFunction,

  texture: TextureApi,

  emitEvent: <T>(eventName: string, arguments: T) => void
  log:(string:string) => void;
}