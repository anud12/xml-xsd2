import {StringExpression} from "../primitives/stringExpression";
import {NumberExpression} from "../primitives/numberExpression";
import {SpriteResource} from "../texture/SpriteResource";

export type AnimationFrame = {
  sprite: SpriteResource;
}

export type AnimationRegistrationArguments = {
  frames: AnimationFrame[];
}

export type RegisterAnimationFunction = (name: StringExpression, arguments: AnimationRegistrationArguments) => void;

export type GetAnimationFunction = (name: StringExpression, animationDurationInGameTimeUnits: NumberExpression) => AnimationRegistrationArguments;
