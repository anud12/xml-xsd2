import {ConditionExpressionApi} from "./primitives/conditionExpression";
import {NumberExpressionApi} from "./primitives/numberExpression";
import {StringExpression} from "./primitives/stringExpression";
import {MaybeExpressionApi} from "./primitives/maybeExpression";
import {RegisterEffectFunction} from "./Effect";

export type HostApi = {
  condition:ConditionExpressionApi,
  number:NumberExpressionApi,
  string:StringExpression,
  maybe: MaybeExpressionApi,
  registerEvent:RegisterEffectFunction,
  emitEvent:<T>(eventName:StringExpression, arguments:T) => void
}