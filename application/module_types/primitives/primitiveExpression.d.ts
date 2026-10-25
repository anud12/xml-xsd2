import {Expression} from "../Expression";
import {ConditionReadOnlyExpressionApi} from "./conditionExpression";
import {StringReadOnlyExpressionApi} from "./stringExpression";
import {NumberReadOnlyExpressionApi} from "./numberExpression";
import {MaybeReadOnlyExpressionApi} from "./maybeExpression";

/** Common base of all primitive expression types. */
export type PrimitiveReadOnlyExpression = Expression;

export type ExpressionTypes = ConditionReadOnlyExpressionApi["type"]
    | StringReadOnlyExpressionApi["type"]
    | NumberReadOnlyExpressionApi["type"]
    | MaybeReadOnlyExpressionApi["type"]
