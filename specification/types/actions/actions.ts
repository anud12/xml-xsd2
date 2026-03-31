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
 * Exactly one of `entity`, `container`, or `point` must be specified per
 * wire message. The runtime validates that the chosen variant matches the
 * registered action's `targetType`.
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
 * The runtime processes this message through a 9-step pipeline (see
 * actions.md — Runtime Processing Flow). The `actorEntityId` must correspond
 * to an entity owned by the sending session; otherwise the message is rejected
 * with an auth error.
 *
 * @see actions.md
 */
export type ActionMessage = {
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
   * container).
   */
  target: ActionTarget;
};

/**
 * Execution context available to `guard`, `cooldown`, and `input` mapper
 * callbacks within an action declaration.
 *
 * Provides typed access to the actor and the resolved wire target. All
 * properties are read-only against the current read-buffer snapshot.
 *
 * @see RegisterActionArgs
 * @see actions.md — ActionContext section
 */
export type ActionContext = {
  /** The entity performing the action, resolved from `actorEntityId`. */
  actor: EntityExpression;
  /** The resolved target from the wire message. */
  target: ActionTarget;
};

/**
 * A single node in an action's effect pipeline DAG.
 *
 * Nodes with no `after` dependencies are root nodes and run concurrently.
 * A node begins its `prepare()` only after all nodes listed in `after` have
 * completed their `prepare()`. The pipeline commits atomically in a single
 * commit once all nodes have prepared.
 *
 * @see RegisterActionArgs.pipeline
 * @see actions.md — Pipeline Execution Semantics
 */
export type PipelineNode = {
  /** Name of the registered Effect/Event to invoke. */
  effect: string;

  /**
   * Effect names this node must wait for before its own `prepare()` begins.
   *
   * Those effects' `prepare()` outputs are passed to the `input` mapper via
   * the `upstream` argument. Omit or leave empty for root nodes.
   */
  after?: string[];

  /**
   * Maps `ActionContext` and upstream `prepare()` outputs to this effect's
   * declared input shape.
   *
   * Only needed when this node depends on upstream outputs or must inject
   * action context into the effect. Omit when the effect constructs its own
   * input independently.
   *
   * @param context  - The shared ActionContext for this pipeline run.
   * @param upstream - Map of `{ [effectName]: prepareOutput }` for each effect
   *                   declared in `after`.
   */
  input?: (context: ActionContext, upstream: Record<string, any>) => any;
};

/**
 * Arguments for registering a named Action via `hostApi.registerAction`.
 *
 * An action is the sole external entrypoint into the runtime. Clients send
 * actions over WebSocket; modules declare them and bind them to effect
 * pipelines.
 *
 * @example
 * ```ts
 * hostApi.registerAction({
 *   name: "pickUp",
 *   targetType: "entity",
 *   guard: ctx => ctx.actor.withTextMap(hostApi.textMap.create())
 *                         .withNumberMap(hostApi.numberMap.create()),
 *   cooldown: _ctx => hostApi.temporal.of(hostApi.number.of(1), "round"),
 *   pipeline: [{ effect: "transferEntityToActorInventory" }],
 * });
 * ```
 *
 * @see actions.md
 */
export type RegisterActionArgs = {
  /**
   * Unique action name; clients identify this action by name on the wire.
   *
   * Must be unique across all loaded modules.
   */
  name: string;

  /** Optional human-readable description (for tooling and documentation). */
  description?: string;

  /**
   * The kind of target this action accepts.
   *
   * Must match the `type` field in the wire message's {@link ActionTarget}.
   * Validated at step [4] before any module code runs.
   */
  targetType: 'entity' | 'container' | 'point';

  /**
   * Optional eligibility guard evaluated against the read-buffer before the
   * pipeline runs.
   *
   * If the guard returns false or throws, the action is rejected and the client
   * receives a structured error response plus a corrective state delta.
   * Use this for actor and target eligibility checks (role, classification,
   * proximity, etc.).
   */
  guard?: (context: ActionContext) => ConditionExpression;

  /**
   * Minimum in-game time between invocations of `cooldownGroup` for the same
   * actor.
   *
   * Evaluated with the current ActionContext. Uses {@link TemporalExpression}
   * semantics — see temporalExpression.md for unit registration and GTU
   * semantics.
   *
   * @see cooldownGroup
   * @see temporalExpression.md
   */
  cooldown?: (context: ActionContext) => TemporalExpression;

  /**
   * Name of the shared per-actor cooldown group.
   *
   * Actions sharing the same `cooldownGroup` share a single per-actor timer.
   * If omitted, the action's `name` is used as its own group (independent
   * cooldown).
   *
   * @example `"melee"` — shared by "attack" and "heavyAttack"
   */
  cooldownGroup?: string;

  /**
   * DAG of Effects to execute when this action is dispatched.
   *
   * Nodes with no `after` dependencies run `prepare()` concurrently. All
   * `prepare()` calls complete before any `apply()` is invoked. The entire
   * pipeline commits atomically in a single commit.
   *
   * DAG cycles are detected at module load time and cause
   * `E_PIPELINE_CYCLE`.
   */
  pipeline: PipelineNode[];
};
