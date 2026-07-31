import {StringExpression} from "../primitives/stringExpression";
import {NumberExpression} from "../primitives/numberExpression";
import {SpriteResource} from "../texture/SpriteResource";

/**
 * A single frame in an animation, referencing a sprite resource.
 */
export type AnimationFrame = {
  /** The sprite image for this frame. */
  sprite: SpriteResource;
}

/**
 * Arguments passed when registering an animation.
 */
export type AnimationRegistrationArguments = {
  /** The ordered list of frames that make up the animation. */
  frames: AnimationFrame[];
}

/**
 * Callback signature for registering a named animation with frame definitions.
 */
export type RegisterAnimationFunction = (name: StringExpression, arguments: AnimationRegistrationArguments) => void;

/**
 * Options configuration for retrieving an animation.
 */
export type AnimationOptions = {
  /** The duration of the animation in game time units. */
  duration: NumberExpression;
  /** Whether the animation should loop when the duration is exceeded. */
  loop?: boolean;
};

/**
 * Callback signature for retrieving a registered animation by name and duration.
 */
export type GetAnimationFunction = (name: StringExpression, options: AnimationOptions) => AnimationRegistrationArguments;
