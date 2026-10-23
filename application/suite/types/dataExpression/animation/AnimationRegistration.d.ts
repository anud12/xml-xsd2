import {StringReadOnlyExpression} from "../../primitives/stringExpression";
import {NumberReadOnlyExpression} from "../../primitives/numberExpression";
import {Expression} from "../../Expression";
import {SpriteResource} from "../../texture/SpriteResource";

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
  /** The duration of the animation in game time units. */
  duration?: NumberReadOnlyExpression;
  /** Whether the animation loops when the duration is exceeded. */
  loop?: boolean;
}

/**
 * A read-only reference to a registered animation, returned by
 * `hostApi.runtime.setAnimation`. Use it anywhere an animation is referenced.
 */
export type AnimationReadOnlyExpression = Expression & {
  /** The animation's name. */
  name: string;
};

/**
 * Callback signature for registering a named animation with frame definitions.
 * Returns a reference to the registered animation.
 */
export type SetAnimationFunction = (name: StringReadOnlyExpression, arguments: AnimationRegistrationArguments) => AnimationReadOnlyExpression;

/**
 * Callback signature for retrieving a registered animation by name.
 * Returns the same {@link AnimationReadOnlyExpression} handle the animation's
 * `setAnimation` call returned (null when the name is not registered).
 * Pass that handle anywhere an animation is referenced (a panel background,
 * a progress fill, a hover/border texture) — the engine resolves the
 * registration (frames/duration/loop) live every tick.
 */
export type GetAnimationFunction = (name: StringReadOnlyExpression) => AnimationReadOnlyExpression;
