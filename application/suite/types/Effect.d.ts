import {StringExpression, MutableStringExpression} from "./primitives/stringExpression";
import {ExpressionTypes} from "./primitives/expression";
import {MaybeExpression} from "./primitives/maybeExpression";
import {NumberExpression} from "./primitives/numberExpression";
import {ConditionExpression} from "./primitives/conditionExpression";
import {Entity, EntityCreationArguments} from "./Entity";
import {EntityFilter} from "./EntityFilter";
import {ListExpression} from "./primitives/ListExpression";

export type RegisterEffectFunction = <Input, Output>(argument: RegisterEventArgs<Input, Output>) => void;

/**
 * Context for the `prepare` phase.
 *
 * Read-only access to the entity repository, plus the ability to emit events
 * synchronously within the current prepare wave. Mutating the entity repository
 * is not possible from this phase.
 */
export type PrepareContext = {
  /**
   * Emit a named event synchronously within the current `prepare` wave.
   *
   * The emitted event enters the same synchronous prepare wave and follows the
   * same stage ordering (prepare → apply → commit). Must only be called from
   * `prepare`, not from `apply`.
   *
   * The runtime enforces a recursion guard to prevent infinite synchronous
   * emission chains.
   */
  emitEvent: <T>(eventName: MutableStringExpression | string, input: Record<string, any>) => T;

  /**
   * Query the entity repository read-only for entities matching the given filter.
   *
   * Returns a lazy ListExpression<Entity> that is bound to the read-buffer snapshot
   * (double-buffer semantics). The filter is applied deterministically at commit time
   * against the immutable snapshot of the repository state at the start of the current tick.
   */
  getEntityBy: (entityFilter: EntityFilter) => ListExpression<Entity>;
}

/**
 * Context for the `apply` phase.
 *
 * Mutable access to the entity repository. This is the only phase in which the
 * repository can be modified. Newly created entities are not queryable within the
 * same apply phase; they materialize at commit time.
 */
export type ApplyContext = {
  /**
   * Create a new entity during the effect's `apply` phase. Same shape as
   * `hostApi.runtime.setEntity`: an id plus an `EntityCreationArguments` payload
   * (textMap, numberMap, behavior).
   *
   * Entities are created as part of the effect's recorded mutations and are
   * committed atomically with other state changes. If the commit fails, the
   * entity creation is rolled back.
   *
   * The newly created entity is not available for querying or mutation within
   * the same apply phase; it materializes at commit time.
   */
  createEntity: (entityId: MutableStringExpression | string, arguments: EntityCreationArguments) => ApplyContext;

  /**
   * Query the entity repository for entities matching the given filter, yielding
   * entity views whose map mutations write back to the repository.
   */
  getEntityBy: (entityFilter: EntityFilter) => ListExpression<Entity>;
}

/**
 * Context for the scheduling phases (`reoccurAfterMs`, `isReoccuranceApplicable`).
 *
 * Read-only access to the entity repository. No event emission or entity creation.
 */
export type SchedulingContext = {
  getEntityBy: (entityFilter: EntityFilter) => ListExpression<Entity>;
}

export type RegisterEventArgs<Input, Output> = {
  name: string;
  description?: string;
  input?: Record<string, { type: ExpressionTypes; description?: string }>;
  output?: Record<string, { type: ExpressionTypes; description?: string }>;
  prepare?: (context: PrepareContext, input: Input /* structure declared in `this.input` */) => Output; /* returns structure declared in `this.output` */
  apply?: (context: ApplyContext, output: Output /* passed from result of `this.prepare` */) => void;

  // Optional repeat hooks for effects that reoccur
  /**
   * Called during `apply` to declare a delay until the next invocation.
   * Return a MaybeExpression<NumberExpression> evaluated at commit time. If the Maybe is empty (or the function is not provided), no scheduling will occur.
   * When present, the runtime computes nextScheduledTime = currentCommitTime + evaluatedDelay (values <= 0 schedule for next tick).
   */
  reoccurAfterMs?: (context: SchedulingContext, executionCount: number, input: Input /* structure declared in `this.input` */, output: Output /* passed from result of `this.prepare` */) => MaybeExpression<NumberExpression>;

  /**
   * Called when a scheduled delay elapses to determine whether the effect should re-run.
   * Returns a ConditionExpression evaluated in a fresh SchedulingContext. If true, the runtime re-enqueues the effect using the preserved input and increments executionCount.
   */
  isReoccuranceApplicable?: (context: SchedulingContext, executionCount: number, input: Input /* structure declared in `this.input` */, output: Output /* passed from result of `this.prepare` */) => ConditionExpression;
}
