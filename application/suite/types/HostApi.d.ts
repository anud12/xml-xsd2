import {ConditionExpressionApi} from "./primitives/conditionExpression";
import {NumberExpressionApi} from "./primitives/numberExpression";
import {StringExpression} from "./primitives/stringExpression";
import {MaybeExpressionApi} from "./primitives/maybeExpression";
import {RegisterEffectFunction} from "./Effect";
import {EntityExpressionApi} from "./Entity";
import {NumberMapExpressionApi} from "./numberMap";
import {TextMapExpressionApi} from "./textMap";

export type HostApi = {
  condition: ConditionExpressionApi,
  number: NumberExpressionApi,
  string: StringExpression,
  maybe: MaybeExpressionApi,

  numberMap: NumberMapExpressionApi,
  textMap: TextMapExpressionApi,

  entity: EntityExpressionApi,
  container: ConditionExpressionApi,

  registerEvent: RegisterEffectFunction,
  emitEvent: <T>(eventName: string, arguments: T) => void
}