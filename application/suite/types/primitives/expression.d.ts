import {ConditionExpressionApi} from "./conditionExpression";
import {StringExpressionApi} from "./stringExpression";
import {NumberExpressionApi} from "./numberExpression";
import {MaybeExpressionApi} from "./maybeExpression";

export type ExpressionTypes = ConditionExpressionApi["type"]
    | StringExpressionApi["type"]
    | NumberExpressionApi["type"]
    | MaybeExpressionApi["type"]