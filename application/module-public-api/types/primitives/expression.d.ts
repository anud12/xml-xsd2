import {ConditionExpressionApi} from "./conditionExpression";
import {StringExpressionApi} from "./stringExpression";
import {NumberExpressionApi} from "./numberExpression";
import {MaybeExpressionApi} from "./maybeExpression";

/**
 * Union of all `type` markers across the primitive expression APIs.
 *
 * Used by the HostApi runtime to declare the expected argument type for
 * events, effects, and actions.  Each primitive API exposes a `type` field
 * (e.g., `hostApi.number.type`) that is a marker token — passing this token
 * signals to the runtime which expression type the argument carries.
 *
 * ## Included markers
 *
 * | Marker                  | Expression type              |
 * |-------------------------|------------------------------|
 * | `ConditionExpressionApi` | lazy boolean tree            |
 * | `StringExpressionApi`    | lazy string tree             |
 * | `NumberExpressionApi`    | lazy 64-bit signed integer   |
 * | `MaybeExpressionApi`     | optional / nullable wrapper  |
 *
 * @example
 * ```ts
 * hostApi.registerEffect({
 *   name: "myEffect",
 *   args: [
 *     { name: "amount", type: hostApi.number.type },
 *     { name: "target", type: hostApi.string.type },
 *   ],
 *   // ...
 * });
 * ```
 */
export type ExpressionTypes = ConditionExpressionApi["type"]
    | StringExpressionApi["type"]
    | NumberExpressionApi["type"]
    | MaybeExpressionApi["type"]