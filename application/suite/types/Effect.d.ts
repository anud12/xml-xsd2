import {StringExpression} from "./primitives/stringExpression";
import {ExpressionTypes} from "./primitives/expression";
import {MaybeExpression} from "./primitives/maybeExpression";
import {NumberExpression} from "./primitives/numberExpression";
import {ConditionExpression} from "./primitives/conditionExpression";
import {Entity, EntityExpression} from "./Entity";
import {EntityFilter} from "./EntityFilter";
import {ListExpression} from "./primitives/ListExpression";

export type RegisterEffectFunction = <Input, Output>(argument: RegisterEventArgs<Input, Output>) => void;

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
   */
  emitEvent: <T>(eventName: StringExpression, input: Record<string, any>) => T;

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
   */
  createEntity: (entity: EntityExpression) => EventContext;

  /**
   * Query the entity repository for entities matching the given filter.
   *
   * Returns a lazy ListExpression<Entity> that is bound to the read-buffer snapshot
   * (double-buffer semantics). The filter is applied deterministically at commit time
   * against the immutable snapshot of the repository state at the start of the current tick.
   *
   * Available in `prepare`, `apply`, and `isReoccuranceApplicable` phases. Multiple calls
   * with the same filter produce semantically identical results (deterministic), but each
   * call returns a freshly-constructed expression node.
   *
   * The returned ListExpression can be composed with other list operations:
   * - map, filter, length, forEach, randomElement, etc.
   * - Composition is lazy; evaluation occurs only at commit time.
   *
   * If the filter matches no entities, the expression evaluates to an empty list (not an error).
   *
   * @param entityFilter - The EntityFilter to apply against the global repository
   * @returns A lazy ListExpression<Entity> representing matching entities
   */
  getEntityBy: (entityFilter: EntityFilter) => ListExpression<Entity>;
}

export type RegisterEventArgs<Input, Output> = {
  name: string;
  description?: string;
  input?: Record<string, { type: ExpressionTypes; description?: string }>;
  output?: Record<string, { type: ExpressionTypes; description?: string }>;
  prepare?: (context: EventContext, input: Input /* structure declared in `this.input` */) => Output; /* returns structure declared in `this.output` */
  apply?: (context: EventContext, output: Output /* passed from result of `this.prepare` */) => void;

  // Optional repeat hooks for effects that reoccur
  /**
   * Called during `apply` to declare a delay until the next invocation.
   * Return a MaybeExpression<NumberExpression> evaluated at commit time. If the Maybe is empty (or the function is not provided), no scheduling will occur.
   * When present, the runtime computes nextScheduledTime = currentCommitTime + evaluatedDelay (values <= 0 schedule for next tick).
   */
  reoccurAfterMs?: (context: EventContext, executionCount: number, input: Input /* structure declared in `this.input` */, output: Output /* passed from result of `this.prepare` */) => MaybeExpression<NumberExpression>;

  /**
   * Called when a scheduled delay elapses to determine whether the effect should re-run.
   * Returns a ConditionExpression evaluated in a fresh ExecutionContext. If true, the runtime re-enqueues the effect using the preserved input and increments executionCount.
   */
  isReoccuranceApplicable?: (context: EventContext, executionCount: number, input: Input /* structure declared in `this.input` */, output: Output /* passed from result of `this.prepare` */) => ConditionExpression;
}