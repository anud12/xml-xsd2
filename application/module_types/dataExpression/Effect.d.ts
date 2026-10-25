import {StringReadOnlyExpression} from "../primitives/stringExpression";
import {Expression} from "../Expression";
import {ExpressionTypes} from "../primitives/primitiveExpression";
import {MaybeReadOnlyExpression} from "../primitives/maybeExpression";
import {NumberReadOnlyExpression} from "../primitives/numberExpression";
import {ConditionReadOnlyExpression} from "../primitives/conditionExpression";
import {EntityReadOnlyExpression, EntityMutableExpression} from "./Entity";
import {ContainerMutableExpression} from "./Contaier";
import {EntityFilter} from "../EntityFilter";
import {ListReadOnlyExpression} from "../primitives/ListExpression";

/**
 * A read-only reference to a registered effect, returned by
 * `hostApi.runtime.setEffect`. Use it anywhere an effect is referenced.
 */
export type EffectReadOnlyExpression = Expression & {
  /** The effect's name. */
  name: string;
}

export type SetEffectFunction = <Input, Output>(argument: SetEventArgs<Input, Output>) => EffectReadOnlyExpression;

export type EventPrepareContext = {
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
  emitEvent: <T>(effect: EffectReadOnlyExpression, input: Record<string, any>) => T;

  /**
   * Query the entity repository for entities matching the given filter.
   *
   * Returns a lazy ListReadOnlyExpression<EntityReadOnlyExpression> that is bound to the read-buffer snapshot
   * (double-buffer semantics). The filter is applied deterministically at commit time
   * against the immutable snapshot of the repository state at the start of the current tick.
   *
   * Available in `prepare` phase. Multiple calls
   * with the same filter produce semantically identical results (deterministic), but each
   * call returns a freshly-constructed expression node.
   *
   * The returned ListReadOnlyExpression can be composed with other list operations:
   * - map, filter, length, forEach, randomElement, etc.
   * - Composition is lazy; evaluation occurs only at commit time.
   *
   * If the filter matches no entities, the expression evaluates to an empty list (not an error).
   *
   * @param entityFilter - The EntityFilter (a FilterArg object) to apply against the global repository
   * @returns A lazy ListReadOnlyExpression<EntityReadOnlyExpression> representing matching entities
   */
  getEntityBy: (entityFilter: EntityFilter) => ListReadOnlyExpression<EntityReadOnlyExpression>;
}

export type EventApplyContext = {
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
   * @param entity - An EntityReadOnlyExpression built via hostApi.entity.create()...
   * @returns This EventApplyContext for method chaining.
   */
  createEntity: (entity: EntityReadOnlyExpression) => EventApplyContext;

  /**
   * Query the entity repository for entities matching the given filter.
   *
   * Returns a lazy ListReadOnlyExpression<EntityReadOnlyExpression> that is bound to the read-buffer snapshot
   * (double-buffer semantics). The filter is applied deterministically at commit time
   * against the immutable snapshot of the repository state at the start of the current tick.
   *
   * Available in `apply` phase. Multiple calls
   * with the same filter produce semantically identical results (deterministic), but each
   * call returns a freshly-constructed expression node.
   *
   * The returned ListReadOnlyExpression can be composed with other list operations:
   * - map, filter, length, forEach, randomElement, etc.
   * - Composition is lazy; evaluation occurs only at commit time.
   *
   * If the filter matches no entities, the expression evaluates to an empty list (not an error).
   *
   * @param entityFilter - The EntityFilter (a FilterArg object) to apply against the global repository
   * @returns A lazy ListReadOnlyExpression<EntityReadOnlyExpression> representing matching entities
   */
    getMutableEntityBy: (entityFilter: EntityFilter) => ListReadOnlyExpression<EntityMutableExpression>;

  /**
   * Query the container repository for containers matching the given filter.
   *
   * Returns a lazy ListReadOnlyExpression<ContainerMutableExpression> that is
   * bound to the read-buffer snapshot. Available in `apply` phase only.
   * Mutations (addEntity, removeEntity) are committed atomically with the tick.
   *
   * @param containerFilter - A predicate that receives a ContainerReadOnlyExpression and returns a condition
   * @returns A lazy ListReadOnlyExpression<ContainerMutableExpression> representing matching containers
   */
  getMutableContainersBy: (containerFilter: { filters: ((c: import("./Contaier").ContainerReadOnlyExpression) => ConditionReadOnlyExpression<Expression>)[] }) => ListReadOnlyExpression<ContainerMutableExpression>;
}

export type SetEventArgs<Input, Output> = {
  name: StringReadOnlyExpression;
  description?: string;
  input?: Record<string, { type: ExpressionTypes; description?: string }>;
  output?: Record<string, { type: ExpressionTypes; description?: string }>;
  prepare?: (context: EventPrepareContext, input: Input /* structure declared in `this.input` */) => Output; /* returns structure declared in `this.output` */
  apply?: (context: EventApplyContext, output: Output /* passed from result of `this.prepare` */) => void;

  // Optional repeat hooks for effects that reoccur
  /**
   * Called during `apply` to declare a delay until the next invocation.
   * Return a MaybeReadOnlyExpression<NumberReadOnlyExpression> evaluated at commit time. If the Maybe is empty (or the function is not provided), no scheduling will occur.
   * When present, the runtime computes nextScheduledTime = currentCommitTime + evaluatedDelay (values <= 0 schedule for next tick).
   */
  reoccurAfterMs?: (context: EventPrepareContext, executionCount: number, input: Input /* structure declared in `this.input` */, output: Output /* passed from result of `this.prepare` */) => MaybeReadOnlyExpression<NumberReadOnlyExpression>;

  /**
   * Called when a scheduled delay elapses to determine whether the effect should re-run.
   * Returns a ConditionReadOnlyExpression evaluated in a fresh ExecutionContext. If true, the runtime re-enqueues the effect using the preserved input and increments executionCount.
   */
  isReoccuranceApplicable?: (context: EventPrepareContext, executionCount: number, input: Input /* structure declared in `this.input` */, output: Output /* passed from result of `this.prepare` */) => ConditionReadOnlyExpression<Expression>;
}