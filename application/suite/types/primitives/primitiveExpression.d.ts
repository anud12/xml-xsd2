import {Expression} from "../Expression";
import {ConditionExpressionApi} from "./conditionExpression";
import {StringExpressionApi} from "./stringExpression";
import {NumberExpressionApi} from "./numberExpression";
import {MaybeExpressionApi} from "./maybeExpression";

/** Common base of all primitive expression types. */
export type PrimitiveExpression = Expression;

export type ExpressionTypes = ConditionExpressionApi["type"]
    | StringExpressionApi["type"]
    | NumberExpressionApi["type"]
    | MaybeExpressionApi["type"]
