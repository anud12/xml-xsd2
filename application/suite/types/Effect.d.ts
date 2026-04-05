import {StringExpression} from "./primitives/stringExpression";
import {ExpressionTypes} from "./primitives/expression";
import {MaybeExpression} from "./primitives/maybeExpression";
import {NumberExpression} from "./primitives/numberExpression";
import {ConditionExpression} from "./primitives/conditionExpression";
import {EntityExpression} from "./Entity";

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
   */
  createEntity: (entity: EntityExpression) => EventContext;
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