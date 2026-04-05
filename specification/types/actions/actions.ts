import type { UniqueGlobalEntityId, UniqueGlobalContainerId } from '../data-model/ids';
import type { NumberExpression } from '../primitives/numberExpression';
import type { ConditionExpression } from '../primitives/conditionExpression';
import type { TemporalExpression } from '../primitives/temporalExpression';
import type { EntityExpression } from '../data-model/entity';

/**
 * 1D or 2D position within a container.
 *
 * The number of declared fields must match the target container's declared
 * dimension count. The runtime validates this at step [4] before any module
 * code runs.
 *
 * @see actions.md — Wire Message section
 * @see containers.md — Dimensions section
 */
export type ContainerPoint =
  | { dimension1: NumberExpression }
  | { dimension1: NumberExpression; dimension2: NumberExpression };

/**
 * Discriminated union describing what the action is targeting.
 *
 * Each action targets a single entity, container, or point. Bulk operations
 * are handled by either:
 * - The client sending multiple ActionMessages (one per target)
 * - The action's apply function emitting multiple events
 *
 * The runtime validates that the chosen variant matches the registered action's `targetType`.
 *
 * @see RegisterActionArgs.targetType
 * @see actions.md — Wire Message section
 */
export type ActionTarget =
  | { type: 'entity';    entityId: UniqueGlobalEntityId }
  | { type: 'container'; containerId: UniqueGlobalContainerId }
  | { type: 'point';     containerId: UniqueGlobalContainerId; position: ContainerPoint };

/**
 * WebSocket wire message sent by a client to trigger an action.
 *
 * The runtime processes this message through a 9-step flow (see
 * actions.md — Runtime Processing Flow). The `actorEntityId` must correspond
 * to an entity owned by the sending session; otherwise the message is rejected
 * with an auth error.
 *
 * For no-input actions registered via `registerAction`, the `target` field is omitted.
 *
 * @see actions.md
 */
export type ActionMessage =
  | {
      /** Name of the registered action to invoke. */
      actionName: string;
      /**
       * Id of the entity performing the action.
       *
       * Must be an entity owned by the session that sent this message.
       * Validated at step [2] before any module code runs.
       */
      actorEntityId: UniqueGlobalEntityId;
      /**
       * The target of the action (entity, container, or coordinate within a
       * container). Omitted for no-input actions.
       */
      target: ActionTarget;
    }
  | {
      /** Name of the registered no-input action to invoke. */
      actionName: string;
      /**
       * Id of the entity performing the action.
       *
       * Must be an entity owned by the session that sent this message.
       */
      actorEntityId: UniqueGlobalEntityId;
      /** Target is omitted for no-input actions. */
      target?: never;
    };

/**
 * Execution context available to `guard`, `cooldown`, and `apply` callbacks
 * within an action declaration.
 *
 * Provides read-only access to the actor. The target is passed separately as
 * the second parameter to allow for proper type narrowing.
 *
 * All properties are read-only against the current read-buffer snapshot.
 *
 * @see RegisterActionArgs
 * @see actions.md — ActionContext section
 */
export type ActionContext = {
  /** The entity performing the action, resolved from `actorEntityId`. */
  actor: EntityExpression;
  /**
   * Emit an event to trigger further side effects.
   * 
   * Events are emitted synchronously during the action execution phase.
   * The event's `prepare` phase runs immediately against the current read-buffer.
   * Multiple events may be emitted from a single `apply` function.
   * 
   * @param eventName - Name of the registered event to emit
   * @param input - Input payload for the event (structure depends on the event's input schema)
   */
  emitEvent: (eventName: string, input: any) => any;
};

/**
 * Discriminated union of target specifications passed to guard/cooldown/apply callbacks.
 * 
 * Each variant represents a single target (not arrays). Bulk operations are handled
 * by multiple ActionMessages or via emitting multiple events.
 */
export type ActionTargetSpec =
  | { type: 'entity'; id: UniqueGlobalEntityId }
  | { type: 'container'; id: UniqueGlobalContainerId }
  | { type: 'point'; containerId: UniqueGlobalContainerId; position: ContainerPoint };

/**
 * Map of target spec variants keyed by target type.
 * 
 * Used by RegisterActionArgsFor<T> generic to properly type callbacks for each action type.
 */
export type ActionTargetSpecMap = {
  entity:    { type: 'entity'; id: UniqueGlobalEntityId };
  container: { type: 'container'; id: UniqueGlobalContainerId };
  point:     { type: 'point'; containerId: UniqueGlobalContainerId; position: ContainerPoint };
};

/**
 * Union of all possible single-target types.
 * 
 * Each action registers for exactly one of these types.
 */
export type ActionTargetType = 'entity' | 'container' | 'point';

/**
 * Base properties shared by all action configurations.
 */
type RegisterActionArgsBase = {
  name: string;
  description?: string;
  cooldownGroup?: string;
};

/**
 * Generic action configuration bound to a specific target type.
 *
 * The generic parameter T ensures that:
 * - Guard/cooldown/apply callbacks receive properly typed target specs
 * - The target spec discriminant matches the declared target type
 * - TypeScript enforces correct usage via discriminated union narrowing
 *
 * @see actions.md
 */
type RegisterActionArgsFor<T extends ActionTargetType> = {
  guard?: (context: ActionContext, target: ActionTargetSpecMap[T]) => ConditionExpression;
  cooldown?: (context: ActionContext, target: ActionTargetSpecMap[T]) => TemporalExpression;
  apply: (context: ActionContext, target: ActionTargetSpecMap[T]) => void;
} & RegisterActionArgsBase;

/**
 * Entity-targeted action registration.
 */
export type RegisterEntityActionArgs = RegisterActionArgsFor<'entity'>;

/**
 * Container-targeted action registration.
 */
export type RegisterContainerActionArgs = RegisterActionArgsFor<'container'>;

/**
 * Point-targeted action registration.
 */
export type RegisterPointActionArgs = RegisterActionArgsFor<'point'>;

/**
 * No-input action registration.
 * 
 * Actions that do not interact with a specific target and only affect the actor.
 * The apply callback receives only the ActionContext (no target parameter).
 * 
 * @example
 * ```ts
 * hostApi.registerAction({
 *   name: "rest",
 *   cooldown: (_ctx) => hostApi.temporal.seconds(hostApi.number.of(5)),
 *   apply: (ctx) => {
 *     ctx.emitEvent("restActor", { actorId: ctx.actor.id });
 *   }
 * });
 * ```
 */
export type RegisterActionArgs = {
  name: string;
  description?: string;
  cooldownGroup?: string;
  guard?: (context: ActionContext) => ConditionExpression;
  cooldown?: (context: ActionContext) => TemporalExpression;
  apply: (context: ActionContext) => void;
};
