# Effects — Concepts

This document describes the core `Effect` concept.

## Summary

An `Effect` is used to declare changes.

## Purpose

The `Effect` system provides a formal, declarative mechanism for defining **pure** state transitions that the runtime can optimize and execute deterministically. Its primary goals are:
- **Enforce Declarative Logic**: To maintain a pure AST-based execution model, Effects discourage imperative control flow (like standard JavaScript `if` or `switch` statements). Instead, they require the use of lazy `ConditionExpression` trees to declare all logical branches upfront, allowing the runtime to "crunch" transitions as flat data.
- **Phase Separation for Performance**: By splitting logic into a `prepare` phase (resolving targets and evaluating expressions) and an `apply` phase (committing mutations), the runtime can maximize parallel execution and SIMD optimization without global state locks.
- **Guarantee Determinism**: Every state change must be derived from the `ExecutionContext`. This ensures that effects remain perfectly reproducible across different threads and between the runtime and client.
- **Transactional Integrity**: Provides a safe boundary where game state is only modified if the preparation phase successfully resolves all required logic, preventing partial or corrupted state updates.

## Notes
The reason for splitting into `prepare` and `apply` is that when an event is emitted, it will return its *prepared* payload, so the origin can have the computation result. Think about first `A` executing `B` and if `B` value is greater than 10, then `C` is called with `B`'s result.

## Runtime responsibilities

### Double-buffer commit semantics and atomic commit
  - The runtime keeps two state buffers: a read-buffer (current visible state) and a write-buffer (where committed updates are staged).
  - All evaluation of expression wrappers and application of recorded mutations happens during the commit phase and writes into the write-buffer.
  - After a successful commit, the runtime atomically swaps the write-buffer to become the new read-buffer.
  - `prepare` must be strictly read-only: it may only inspect the read-buffer via provided `EventContext` . It does NOT mutate state.

---

### Stage ordering (synchronous, deterministic)
  - emitEvent → prepare (read-only): When an event is emitted, the runtime synchronously runs its `prepare` stage using the current read-buffer.
  - enqueue apply: When `prepare` returns outputs, the runtime enqueues a corresponding `apply` invocation.
  - apply (record mutations): `apply` runs synchronously; it must not write to state immediately but must call use `NumberExpressions`/`StringExpressions`/etc., api to record intended changes.
  - commit (evaluate expressions and write to write-buffer): After all pending `apply` calls for the current emission wave complete, the runtime evaluates the recorded expression wrappers in the context of the read-buffer and records concrete write operations in the write-buffer in the order recorded.
  - swap buffers: On successful commit, swap the buffers so the writes become visible.
  - Important: expression wrappers returned from `prepare` are evaluated only once at commit time (or at a defined time by the runtime), never during `prepare`.

---

### Cross-event emission and recursion guard
  - `emitEvent` may be used from `prepare` only. Emissions follow the stage ordering above.
  - To prevent infinite synchronous recursion, the runtime must implement a recursion guard:
    - e.g., a per-tick depth limit, per-event stack detection, or cycle detection across the current call-stack of events.
    - If the guard triggers (cycle or depth exceeded), further synchronous emits should be rejected or queued for later microtask processing according to runtime policy. The runtime must not loop indefinitely.
  - The guard should produce a deterministic, debuggable failure mode (log/error and aborting the offending emission).

---

## EventContext Type

`EventContext` is the execution context passed to both `prepare` and `apply` callbacks. It provides the runtime services available during effect execution.

**Type Definition**:

```typescript
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
};
```

---

### Entity Creation

Effects may create entities during the `apply` phase as part of their recorded mutations. Created entities are:

- **Transactional**: Entity creation is recorded as a mutation intent and is committed atomically with other state changes. If the commit fails, the entity creation is rolled back.
- **Only in apply**: The `createEntity` method is available only in the `apply` phase. Calling it from `prepare` will raise an error.
- **Deferred materialization**: The newly created entity does not exist until the commit completes. It cannot be queried or mutated within the same `apply` phase.
- **Chainable**: The method returns `EventContext` for fluent method chaining, allowing multiple entities to be created in a single expression.
- **Reusable expressions**: The same `EntityExpression` can be passed to `createEntity` multiple times to create multiple distinct entities.

**Example**:
```typescript
apply: (context, output) => {
  const newEntity = hostApi.entity.create()
    .withTextMap(hostApi.textMap.create().put("name", hostApi.string.of("goblin")))
    .withNumberMap(hostApi.numberMap.create().put("health", hostApi.number.of(100)));
  
  context.createEntity(newEntity);
  
  // Entity does not exist yet — cannot query or mutate
}
```

**Chaining multiple creations**:
```typescript
apply: (context, output) => {
  const template = hostApi.entity.create()
    .withNumberMap(hostApi.numberMap.create().put("health", hostApi.number.of(50)));
  
  context
    .createEntity(template)
    .createEntity(template)
    .createEntity(template);
  
  // Three entities created from the same template, materialized at commit
}
```

---

### Error handling
  - If an exception is thrown during `prepare` or `apply`:
    - Any recorded mutations from that event's `apply` (or from the current wave) must be discarded.
    - The runtime must not perform a commit that includes partially recorded mutations from a failing event.
    - The runtime should surface the error to a host-provided logger/observer and continue processing subsequent independent events (per policy).
  - Commit errors (evaluation-time errors when evaluating expression wrappers) should abort that commit and discard recorded mutations; the runtime may optionally attempt a rollback to the prior read-buffer state (which is already preserved by double-buffering).
  - If entity creation fails during the commit phase, the entire commit is aborted and all entities (and other mutations) from that commit wave are rolled back.

---

### Randomness context
  - When an event chain is started and no randomness context is present, the runtime creates a deterministic randomness context (according to [Randomness Specification](../runtime/randomness.md)) and attaches it to the chain. 
  This randomness context is propagated to all subsequent synchronous `emitEvent` calls within the chain so that `oneOf` / `random` operations evaluate deterministically and consistently acrossnested emissions.

---

### Recursive Validation
  - When an event is loaded, it must be proven that every synchronous prepare → emitEvent chain is finite, failure to do so should result in:
    - World registration MUST be rejected with error code E_RECURSION_UNPROVEN_VALIDATION_FAIL.

---

### Reoccurrence (repeatable effects)

To support effects that can schedule future re-occurrences, effects MAY include two optional declarative callbacks (expressed as expression-producing functions) to control repeating behavior. The execution flow is:

1. The effect is emitted with `input` and runs `prepare` to compute `output`.
2. `apply` runs and records mutation intents. During `apply` the effect MAY call `reoccurAfter` to declare a delay until the next invocation.
3. The runtime records the scheduled entry atomically with the commit (including preserved `input`/`output` refs and current `executionCount`).
4. When the scheduled delay elapses, the runtime evaluates `isReoccuranceApplicable` for that scheduled entry to decide whether to re-run the effect. If `isReoccuranceApplicable` evaluates to true the runtime enqueues a fresh invocation of the effect using the preserved `input` (and increments `executionCount`).

Callbacks

- `reoccurAfter(context, executionCount, input, output): MaybeExpression<TemporalExpression>` — invoked during `apply` (and evaluated at commit time) to produce an optional in-game duration until the next invocation. If the result is empty (the Maybe is empty) or the function is not present, the runtime will not schedule a repeat. When present, the runtime computes `nextScheduledGTU = currentGTU + resolvedGTU`. A `TemporalExpression` that resolves to 0 GTU schedules for the next available tick. See [`temporalExpression.md`](../expressions/temporalExpression.md) for unit registration and GTU semantics.

- `isReoccuranceApplicable(context, executionCount, input, output): ConditionExpression` — invoked when the scheduled delay elapses (at scheduled time). This function receives the preserved previous `input` and `output` and must return a ConditionExpression. The runtime evaluates this expression in a fresh `ExecutionContext` for the scheduled check; if it evaluates to true, the runtime re-enqueues the effect (which will run `prepare` → `apply` again and may call `reoccurAfterMs` for further repeats).

Notes & semantics:
- `executionCount` is the 0-based count of how many times the effect has executed, including the current execution (first execution => executionCount=0). On each re-run, `executionCount` increments.
- `reoccurAfter` is evaluated at commit time and recorded as part of the commit writes so scheduling is atomic with other state changes.
- `isReoccuranceApplicable` is evaluated at scheduled time in a new `ExecutionContext`. It may reference the previous `input` and `output` and the current runtime state (via expression wrappers) as needed.
- Both callbacks must be pure, deterministic, and side-effect free; they should produce expression wrappers evaluated by the runtime.
- Atomicity: if the commit that records the schedule aborts, no scheduled entry is recorded.
- Persistence: scheduled entries are part of persisted scheduling state (if persistence is enabled) and must survive restarts if persisted snapshots include scheduler state.
- Multiple occurrences: if multiple scheduled occurrences for the same effect fall within the same tick, the runtime must process them in chronological order deterministically. Tie-breaking for identical timestamps should be deterministic (e.g., module id, effect id, executionCount).
- Cancellation: the effect stops repeating when `isReoccuranceApplicable` evaluates to false. Explicit in-flight cancellation APIs are optional and may be added separately.


Implementation notes:
- The runtime MUST provide an internal scheduler (persisted or in-memory) to track scheduled entries. Scheduling entries are materialized at commit time and included in commit writes for atomicity.
- The runtime should enforce per-module schedule quotas (max scheduled effects, invocations per tick) to avoid resource exhaustion.
- When persisting scheduled entries, include enough data (module id, effect id, executionCount, input/output snapshot or stable refs) to reconstruct the eventual `ExecutionContext` for the re-invocation.

## Example
```typescript


/**
 * Allowed argument types for event inputs. This mirrors the HostApi "type" markers
 * used by expression APIs so callers may declare expression-typed args explicitly.
 */
type EventArgType = ConditionExpressionType
    | StringExpressionType 
    | NumberExpressionType
    | EntityExpressionType
    | ContainerExpressionType
    | TemporalExpressionType;

type RegisterEventArgs<Input, Output> = {
  name: string;
  description?: string;
  input?: Record<string, { type: EventArgType; description?: string }>;
  output?: Record<string, { type: EventArgType; description?: string }>;
  prepare?: (context: EventContext, input: Input /* structure declared in `this.input` */) => Output; /* returns structure declared in `this.output` */
  apply?: (context: EventContext, output: Output /* passed from result of `this.prepare` */) => void;

  // Optional repeat hooks for effects that reoccur
  /**
   * Called during `apply` to declare a delay until the next invocation.
   * Return a MaybeExpression<TemporalExpression> evaluated at commit time. If the Maybe is empty (or the function is not provided), no scheduling will occur.
   * When present, the runtime computes nextScheduledGTU = currentGTU + resolvedGTU.
   * A TemporalExpression resolving to 0 GTU schedules for the next available tick.
   * See temporalExpression.md for unit registration and GTU semantics.
   */
  reoccurAfter?: (context: EventContext, executionCount: number, input: Input /* structure declared in `this.input` */, output: Output /* passed from result of `this.prepare` */) => MaybeExpression<TemporalExpression>;

  /**
   * Called when a scheduled delay elapses to determine whether the effect should re-run.
   * Returns a ConditionExpression evaluated in a fresh ExecutionContext. If true, the runtime re-enqueues the effect using the preserved input and increments executionCount.
   */
  isReoccuranceApplicable?: (context: EventContext, executionCount: number, input: Input /* structure declared in `this.input` */, output: Output /* passed from result of `this.prepare` */) => ConditionExpression;
}

const appendNumberEvent: RegisterEventArgs = {
  name: "appendNumberToEntity",
  description: "Take number, apply + 2 and then add it to entity",
  input: {
    numberToBeAdded: {
      type: host.number.type,
      description: "Numeric to add"
    },
  },
  output: {
    originEntity: {
      type: host.entity.type,
      description: "Origin target"
    },
    numberToBeAdded: {
      type: host.number.type,
      description: "Total to add"
    },
  },
  /**
   * On emit it executes the prepare and adds apply on queue,
   */
  prepare: (context, input) => {
    context.emitEvent("eventName", {});
    return {
      originEntity: context.entity.getById("1"),
      numberToBeAdded: input.numberToBeAdded.add(host.type.number.of(2)),
    };
  },
  apply: (context, output): void => {
    output.originEntity.setProperty("evil", output.numberToBeAdded);
    
    // Create a new entity during the effect
    const newEntity = hostApi.entity.create()
      .withNumberMap(hostApi.numberMap.create().put("health", output.numberToBeAdded));
    
    context.createEntity(newEntity);
  },

  // Example reoccurrence hooks
  reoccurAfter: (ctx, execCount, input, output) => host.maybe.some(host.temporal.of(host.number.of(1), "round")),
  isReoccuranceApplicable: (ctx, execCount, input, output) => host.condition.isLessThan(host.number.of(execCount), host.number.of(3)),
};

registerEvent(appendNumberEvent);


```