import {StringExpression} from "../primitives/stringExpression";
import {SpriteResource} from "../texture/SpriteResource";

export type AnimationFrame = {
  sprite: SpriteResource;
}

export type AnimationRegistrationArguments = {
  frames: AnimationFrame[];
}

export type RegisterAnimationFunction = (name: StringExpression, arguments: AnimationRegistrationArguments) => void;

export type GetAnimationFunction = (name: StringExpression) => AnimationRegistrationArguments;
