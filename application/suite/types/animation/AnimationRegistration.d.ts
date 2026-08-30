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
 * An inline animation object usable as a panel background definition.
 * Unlike {@link AnimationRegistrationArguments}, the duration is optional
 * and defaults to 1 when omitted.
 */
export type AnimationBackground = {
  /** The ordered list of frames that make up the animation. */
  frames: AnimationFrame[];
  /** The duration of the full frame sequence in game time units. Defaults to 1. */
  duration?: NumberExpression;
  /** Whether the animation should loop when the duration is exceeded. */
  loop?: boolean;
}

/**
 * Arguments passed when registering an animation.
 */
export type AnimationRegistrationArguments = {
  /** The ordered list of frames that make up the animation. */
  frames: AnimationFrame[];
  /** The duration of the full frame sequence in game time units. Required. */
  duration: NumberExpression;
  /** Whether the animation should loop when the duration is exceeded. */
  loop?: boolean;
}

/**
 * Callback signature for registering a named animation with frame definitions.
 */
export type RegisterAnimationFunction = (name: StringExpression, arguments: AnimationRegistrationArguments) => void;

/**
 * Callback signature for retrieving a registered animation by name.
 */
export type GetAnimationFunction = (name: StringExpression) => AnimationRegistrationArguments;
