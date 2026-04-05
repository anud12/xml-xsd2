import type { ConditionExpression } from '../primitives/conditionExpression';
import type { ConditionExpressionType } from '../primitives/conditionExpression';
import type { StringExpressionType } from '../primitives/stringExpression';
import type { NumberExpressionType } from '../primitives/numberExpression';
import type { TemporalExpression, TemporalExpressionType } from '../primitives/temporalExpression';
import type { MaybeExpression } from '../primitives/maybeExpression';
import type { EntityExpression, EntityExpressionType } from '../data-model/entity';
import type { ContainerExpressionType } from '../data-model/container';

/**
 * Union of all expression type markers accepted as `type` declarations in
 * event/effect input and output schemas.
 *
 * These markers correspond to the `type` property on each expression Api
 * surface (e.g. `hostApi.number.type`, `hostApi.temporal.type`).
 *
 * @see RegisterEventArgs.input
 * @see RegisterEventArgs.output
 * @see effects.md
 */
export type EventArgType =
  | ConditionExpressionType
  | StringExpressionType
  | NumberExpressionType
  | EntityExpressionType
  | ContainerExpressionType
  | TemporalExpressionType;

/**
 * Execution context provided to `prepare` and `apply` callbacks of an effect.
 *
 * Available during the effect's execution phase only. Provides the ability to
 * emit further events synchronously (during `prepare`) and create entities
 * during `apply`. The runtime exposes any other services via this context.
 *
 * @see effects.md — Stage ordering
 */
export type EventContext = {
  /**
   * Emit a named event synchronously within the current `prepare` wave.
   *
   * The emitted event enters the same synchronous prepare wave and follows the
   * same stage ordering (prepare → apply → commit). Must only be called from
   * `prepare`, not from `apply`.
   *
   * The runtime enforces a recursion guard to prevent infinite synchronous
   * emission chains.
   *
   * @param eventName - Name of the registered effect/event to emit.
   * @param input     - Input payload matching the target event's declared input
   *                    schema.
   * @see effects.md — Cross-event emission and recursion guard
   */
  emitEvent: (eventName: string, input: Record<string, any>) => void;

  /**
   * Create a new entity during the effect's `apply` phase.
   *
   * Entities are created as part of the effect's recorded mutations and are
   * committed atomically with other state changes. If the commit fails, the
   * entity creation is rolled back.
   *
   * Must only be called from `apply`, not from `prepare`.
   *
   * The newly created entity is not available for querying or mutation within
   * the same apply phase; it materializes at commit time.
   *
   * @param entity - An EntityExpression built via hostApi.entity.create()...
   * @returns This EventContext for method chaining.
   *
   * @see effects.md — Entity creation during apply phase
   */
  createEntity: (entity: EntityExpression) => EventContext;
};

/**
 * Arguments for registering a named Effect / Event via
 * `hostApi.registerEvent`.
 *
 * An effect is a declarative, pure state transition split into a `prepare`
 * phase (read-only, returns output) and an `apply` phase (records mutations).
 * The runtime executes effects deterministically and commits all mutations
 * atomically.
 *
 * ## Execution lifecycle
 * 1. `prepare` runs synchronously against the read-buffer; may emit further
 *    events. Returns `Output`.
 * 2. `apply` runs synchronously; records mutation intents as expression
 *    wrappers (must not write state directly). May call `reoccurAfter` to
 *    schedule a future re-invocation.
 * 3. Commit: runtime evaluates all recorded expression wrappers against the
 *    read-buffer and writes to the write-buffer atomically.
 * 4. Swap: write-buffer becomes the new read-buffer.
 *
 * @template Input  - Shape of the effect's input payload (must match `input`
 *                    schema).
 * @template Output - Shape of the effect's output payload (must match `output`
 *                    schema).
 *
 * @example
 * ```ts
 * hostApi.registerEvent({
 *   name: "appendNumber",
 *   input:  { value: { type: hostApi.number.type } },
 *   output: { result: { type: hostApi.number.type } },
 *   prepare: (ctx, input) => ({
 *     result: input.value.sum(hostApi.number.of(2)),
 *   }),
 *   apply: (ctx, output) => {
 *     // record mutation intent via expression wrappers
 *   },
 * });
 * ```
 *
 * @see effects.md
 */
export type RegisterEventArgs<
  Input  = Record<string, any>,
  Output = Record<string, any>
> = {
  /** Unique effect name; used by `emitEvent` and pipeline node references. */
  name: string;

  /** Optional human-readable description (for tooling and documentation). */
  description?: string;

  /**
   * Declared input schema.
   *
   * Each key maps to an `EventArgType` marker and an optional description.
   * Used by tooling, validation, and the module sandbox to type-check inputs.
   */
  input?: Record<string, { type: EventArgType; description?: string }>;

  /**
   * Declared output schema.
   *
   * Each key maps to an `EventArgType` marker and an optional description.
   * Must match the shape returned by `prepare`.
   */
  output?: Record<string, { type: EventArgType; description?: string }>;

  /**
   * Read-only preparation phase.
   *
   * Runs synchronously against the current read-buffer snapshot. May call
   * `context.emitEvent` to emit further events. Must not mutate state. Returns
   * `Output` which is passed to `apply`.
   *
   * Expression wrappers returned from `prepare` are evaluated exactly once at
   * commit time.
   *
   * @param context - Execution context (emitEvent, etc.).
   * @param input   - Resolved input payload (structure declared in `this.input`).
   */
  prepare?: (context: EventContext, input: Input) => Output;

  /**
   * Mutation-recording phase.
   *
   * Runs synchronously after `prepare`. Must record intended mutations as
   * expression wrappers by calling runtime-provided expression APIs. Must not
   * write to state directly or read from the write-buffer.
   *
   * @param context - Execution context.
   * @param output  - The value returned by `prepare`.
   */
  apply?: (context: EventContext, output: Output) => void;

  /**
   * Optional: declare a delay until the next invocation of this effect.
   *
   * Called during `apply`. Returns a `MaybeExpression<TemporalExpression>`
   * evaluated at commit time. If the Maybe is empty (or not provided), no
   * scheduling occurs.
   *
   * When present, the runtime computes:
   * `nextScheduledGTU = currentGTU + resolvedGTU`
   *
   * A TemporalExpression resolving to 0 GTU schedules for the next available
   * tick. See temporalExpression.md for unit registration and GTU semantics.
   *
   * @param context        - Execution context.
   * @param executionCount - 0-based count of how many times this effect has
   *                         executed (0 on the first run; increments on each
   *                         re-run).
   * @param input          - The input payload for this invocation.
   * @param output         - The output returned by `prepare` for this invocation.
   * @see temporalExpression.md
   * @see effects.md — Reoccurrence section
   */
  reoccurAfter?: (
    context: EventContext,
    executionCount: number,
    input: Input,
    output: Output
  ) => MaybeExpression<TemporalExpression>;

  /**
   * Optional: decide whether a scheduled re-invocation should proceed.
   *
   * Called when a previously scheduled delay elapses. Returns a
   * `ConditionExpression` evaluated in a fresh ExecutionContext. If true, the
   * runtime re-enqueues the effect using the preserved `input` and increments
   * `executionCount`.
   *
   * @param context        - Execution context for the scheduled check.
   * @param executionCount - Number of times the effect has already executed.
   * @param input          - The preserved input from the original invocation.
   * @param output         - The preserved output from the previous `prepare`.
   * @see effects.md — Reoccurrence section
   */
  isReoccuranceApplicable?: (
    context: EventContext,
    executionCount: number,
    input: Input,
    output: Output
  ) => ConditionExpression;
};
