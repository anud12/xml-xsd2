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

### Error handling
  - If an exception is thrown during `prepare` or `apply`:
    - Any recorded mutations from that event's `apply` (or from the current wave) must be discarded.
    - The runtime must not perform a commit that includes partially recorded mutations from a failing event.
    - The runtime should surface the error to a host-provided logger/observer and continue processing subsequent independent events (per policy).
  - Commit errors (evaluation-time errors when evaluating expression wrappers) should abort that commit and discard recorded mutations; the runtime may optionally attempt a rollback to the prior read-buffer state (which is already preserved by double-buffering).

---

### Randomness context
  - When an event chain is started and no randomness context is present, the runtime creates a deterministic randomness context (according to [Randomness Specification](./randomness.md)) and attaches it to the chain. 
  This randomness context is propagated to all subsequent synchronous `emitEvent` calls within the chain so that `oneOf` / `random` operations evaluate deterministically and consistently acrossnested emissions.

---

### Recursive Validation
  - When an event is loaded, it must be proven that every synchronous prepare → emitEvent chain is finite, failure to do so should result in:
    - World registration MUST be rejected with error code E_RECURSION_UNPROVEN_VALIDATION_FAIL.

---

### Reoccurrence (repeatable effects)

To support effects that can schedule future re-occurrences, effects MAY include two optional declarative callbacks (expressed as expression-producing functions) to control repeating behavior:

- `shouldRepeat(context, executionCount, input, output): ConditionExpression` — called after `prepare` and before recording commit intents. The function must return a ConditionExpression (an expression wrapper, not a boolean) that the runtime will evaluate at commit time. If the evaluated condition is true, the runtime proceeds to call `updateInterval` to compute the next occurrence time.

- `updateInterval(context, executionCount, input, output): NumberExpression` — called only when `shouldRepeat` evaluated to true. Returns a NumberExpression that evaluates (at commit time) to a non-negative number of milliseconds representing a delay from the current commit time before the next invocation. The runtime computes the next scheduled time as currentCommitTime + evaluatedDelay. Values <= 0 are treated as "schedule for the next available tick" and will be executed on the next tick.

Notes & semantics:
- `executionCount` is the 0-based count of how many times the effect has executed including the current execution (first execution => executionCount=0).
- Both `shouldRepeat` and `updateInterval` must be pure, deterministic, and side-effect free; they may reference the `ExecutionContext`, `input`, and `output` via expression builders to form wrappers evaluated at commit time.
- Evaluation timing: both functions produce expression wrappers that are recorded and evaluated at commit time so they remain consistent with other expression evaluations and the deterministic randomness context.
- Atomicity: scheduling decisions are recorded alongside other commit-time writes. If the commit aborts, no schedule entry is applied.
- Persistence: scheduled occurrences are part of the world's persisted scheduling state (if persistence is enabled) and must survive restarts if persisted snapshots include scheduler state.
- Multiple occurrences: if multiple scheduled occurrences for the same effect fall within the same tick, the runtime must execute each occurrence in chronological order deterministically.
- Cancellation: an effect may stop repeating by returning a `shouldRepeat` that evaluates to `false`. (Explicit in-flight cancellation APIs are not required but may be introduced separately.)

Implementation notes:
- The runtime MUST provide an internal scheduler (persisted or in-memory) to track effect reoccurrences. Scheduling entries are materialized at commit time and must be included in commit writes for atomicity.
- The runtime should enforce per-module schedule quotas (max scheduled effects, invocations per tick) to avoid resource exhaustion.
- When persisting scheduled entries, include enough data (module id, effect id, executionCount, input/output snapshot or refs) to reconstruct the eventual `ExecutionContext` for the re-invocation.

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
    | ContainerExpressionType;

type RegisterEventArgs<Input, Output> = {
  name: string;
  description?: string;
  input?: Record<string, { type: EventArgType; description?: string }>;
  output?: Record<string, { type: EventArgType; description?: string }>;
  prepare?: (context: EventContext, input: Input /* structure declared in `this.input` */) => Output; /* returns structure declared in `this.output` */
  apply?: (context: EventContext, output: Output /* passed from result of `this.prepare` */) => void;

  // Optional repeat hooks for effects that reoccur
  /**
   * Called after prepare to determine whether the effect should be scheduled again.
   * Return a ConditionExpression (expression wrapper) evaluated at commit time. If true, updateInterval is invoked.
   */
  shouldRepeat?: (context: EventContext, executionCount: number, input: Input /* structure declared in `this.input` */, output: Output /* passed from result of `this.prepare` */) => ConditionExpression;

  /**
   * Called when shouldRepeat evaluates to true. Return a NumberExpression (delay in ms) evaluated at commit time.
   * The runtime computes nextScheduledTime = currentCommitTime + evaluatedDelay.
   */
  updateInterval?: (context: EventContext, executionCount: number, input: Input /* structure declared in `this.input` */, output: Output /* passed from result of `this.prepare` */) => NumberExpression;
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
  },

  // Example reoccurrence hooks
  shouldRepeat: (ctx, execCount, input, output) => execCount.isLessThan(host.number.of(3)),
  updateInterval: (ctx, execCount, input, output) => host.number.of(1000),
};

registerEvent(appendNumberEvent);


```