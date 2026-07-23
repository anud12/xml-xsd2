import {SpriteResource} from "./SpriteResource";
import {StringExpression} from "../primitives/stringExpression";
import {AnimationRegistrationArguments} from "../animation/AnimationRegistration";

export type TextureApi = {
  getAnimation: (name: StringExpression) => AnimationRegistrationArguments;
}