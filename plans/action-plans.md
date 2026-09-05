# Plan: Action Plans — actions that span time via `ctx.wait()`

## Status (v1 implemented, 2026-09-03)

Core Rust-side pipeline is done and green:

- **Types**: `ActionContext.wait` (`common.d.ts`), `ActionPlanStep` (`plan.d.ts`, exported from `index.d.ts`). TSC check passes.
- **Recorder**: `sim_tpl_p3.rs` — `apply` runs in recorder mode; `emitEffect`/`wait` append steps; the plan is walked to its first `wait` at dispatch and the remainder (`{ wait, steps }`) is returned in the sim result JSON.
- **State**: `ActivePlan { action_name, steps, resume_at }` table (`state/mod.rs` + accessors), cleared by `clear_state()`.
- **Walker**: `js_executor/active_plans.rs::process_active_plans(now)` — due plans emit (names → `pending_effects`, same as today's pipeline), re-park on `wait` at `now + wait`, remove on exhaustion. 6 unit tests.
- **Tick pass**: `runtime_run_iteration` walks plans before pending/scheduled effects.
- **Occupancy (per-actor)**: the active-plan table is keyed by (actor, action name). Dispatch with an actor id (`runtime_emit_action_for` / `emitAction(action, actor)` / debug-loop `action [actor]` token) is rejected for *any* action while that actor has a parked plan — never interrupting, never queued; unscoped dispatch keeps the name-keyed check (`state::actor_is_busy`, `ffi_mod/debug/debug_simulate_action/mod.rs`, `debug_loop/action_handler.rs`).
- **Shim**: `hostApi.runtime.temporal.ofTicks(n)` added (`js_host_api/ui_host_api.rs`).
- **Tests**: `cargo test` — 19 lib tests + 6 in other targets pass (shared `state::test_lock()` now serializes all state-touching tests across modules); C# suite 86/86.

**v1 deviations / deferred** (open items updated below):

- Occupancy is keyed by **action name**, not per-actor (spec's per-actor lock needs actor id in the key).
- Emitted steps reuse the name-only `pending_effects` pipeline (no per-step payload replay through a fresh event pipeline yet — payloads are recorded but the walker emits names, consistent with today's pending-effect storage).
- C# Jint host parity (recorder + `temporal`) not started — Rust runtime is the authority for actions per the walker-placement decision.
- Spec doc `specification/interaction/action-plan.md` + `actions.md` amendments not yet written.
- Cucumber stage fixture (shoot/rest modules, frame-stepped scenarios) not yet written.

## Context

Actions are currently instant: `guard → cooldown → apply → emitEvent → effects`, all executed and committed within one tick (spec: `specification/interaction/actions.md`). We need actions that span a length of time (aim → wait → fire; rest for N rounds; walk).

Cross-tick suspension already exists in the codebase as a precedent:

- Behaviors: `do` builds `BehaviorStep[]` (`{ action } | { wait }`); a linear step machine walks it across ticks (`application/suite/types/behavior.d.ts`, C# `BehaviorStore`).
- Autonomy: `ScriptBuilder.wait(duration)` (`application/suite/types/autonomy.d.ts`).
- Effect reoccurrence: scheduled effects processed per tick in `runtime_run_iteration` (`application/runtime/src/ffi_mod/misc/run_iteration/mod.rs`).

**Decision (from discussion): `apply` is a declaration phase that records a *plan*; the runtime walks the plan across ticks.** No `async`/`await`, no generator, no JS continuation kept alive — `ctx.wait()` merely appends a step, and the walker is plain data. This unifies the "suspend across ticks" primitive with behaviors/autonomy instead of inventing parallel lifecycle callbacks (`step`/`finish` were considered and rejected).

## Confirmed semantics

1. **`ctx.emitEvent` inside `apply` records, it does not execute** — and returns nothing at plan time. The existing "use event A's result as input to event B" chaining pattern (spec `channelPower` example) no longer works inside one plan; such patterns are rewritten as a single event that does both. Instant actions (no `wait`) behave exactly as today.
2. **Lazy payloads**: expressions in a step's payload are evaluated when the walker *reaches* that step, against that tick's state — not when the plan was built.
3. **v1 step vocabulary**: `emitEvent` + `wait` only. The actor is locked (per-actor serialization) until the plan completes; no mid-plan cancellation in v1. Future: a `ctx.canInterrupt()`-style marker to designate interruptible points in the plan.

## JS API

### `ActionContext.wait` (the only new surface)

Added to `ActionContext` in `application/suite/types/action/common.d.ts`; all four registration APIs (`registerAction`, `registerEntityAction`, `registerContainerAction`, `registerPointAction`) get it for free:

```ts
export type ActionContext = {
  actor: EntityExpression;
  emitEffect: (eventName: string, input: Record<string, any>) => any;
  /**
   * Records a suspension step: the plan parks until `duration` of game time
   * has elapsed, then continues with the next recorded step.
   * Calling this (or emitEffect) executes nothing — apply is a declaration
   * phase; the runtime walks the recorded plan across ticks.
   */
  wait: (duration: TemporalExpression) => void;
};
```

Duration is typed `TemporalExpression` (consistent with `cooldown`). **v1 implementation note**: neither host implements the full temporal unit system yet; `wait` accepts `hostApi.temporal.ofTicks(n)` (raw GTU) resolved against the elapsed-units counter the runtime already advances in `runtime_run_iteration(elapsedUnits)`. Named units (`of(n, "round")`) work once `defineUnit`/`tickAdvancesBy` land; the API shape does not change.

### Examples

```ts
// spans 2 rounds between aim and fire
hostApi.registerAction({
  name: "shoot",
  apply: (ctx) => {
    ctx.emitEffect("aim");
    ctx.wait(hostApi.temporal.ofTicks(2));
    ctx.emitEffect("fire");
  }
});

// rest: heal a bit each round, 10 rounds total
hostApi.registerAction({
  name: "rest",
  apply: (ctx) => {
    for (let i = 0; i < 10; i++) {
      ctx.emitEffect("healTick", { actorId: ctx.actor.id });
      ctx.wait(hostApi.temporal.ofTicks(1));
    }
  }
});
```

### Plan step representation (internal, exposed in types for parity with `BehaviorStep`)

New `application/suite/types/action/plan.d.ts`:

```ts
export type ActionPlanStep =
  | { emit: { eventName: string; payload: Record<string, any> } }
  | { wait: TemporalExpression };
```

## Runtime walk semantics

### Dispatch (unchanged prefix)

Wire message → auth → guard → cooldown, exactly as today (`actions.md` steps 1–7). Then:

1. Acquire per-actor action lock.
2. Run `apply` in **recorder mode**: `ctx.emitEffect` / `ctx.wait` append to the plan; nothing executes.
3. Start walking the plan from step 0 in the current tick.

### Walking

- **`emit` step**: emit through the existing event pipeline (prepare runs now, apply phases enqueued, committed with the tick) — identical to today's emit.
- **`wait` step**: resolve duration to GTU (lazy evaluation, at parking time), set `resumeAt = currentGTU + duration`, park the walker.
- **Plan exhausted**: release the actor lock.

### Per-tick pass

In `runtime_run_iteration`, after the elapsed-units counter advances:

1. Walk every parked plan whose `resumeAt` has passed — deterministic order (actor id ascending). Each resumed plan walks until its next `wait` or completion.
2. Process pending/scheduled effects (existing behavior).
3. Single commit for the tick (existing double-buffer).

### Determinism

- One `ExecutionContext` per plan (seed, actor, actionId from dispatch; dispatch tick as the context tick). Step order is fixed by the plan, so call-indexed PRNG draws are reproducible across the plan's lifetime.
- Plan steps are data (event names + payload expression ASTs — the same representation event payloads already use), so walking requires no JS engine round-trip.

### Locking & failure

- While a plan is active, further actions from the same actor are rejected (`E_ACTOR_BUSY` + corrective delta), per the existing per-actor serialization.
- Actor entity deleted mid-plan: plan aborted, lock released (v1; see open items).

## Implementation map

| Layer | Files | Change |
|---|---|---|
| Spec | `specification/interaction/action-plan.md` (new), `actions.md` | New doc: plan model, walk semantics, failure modes. Amend `actions.md`: `apply` is a declaration phase; `emitEffect` records; per-actor lock held across waits; `E_ACTOR_BUSY`. |
| Types | `application/suite/types/action/common.d.ts`, `plan.d.ts` (new), `index.d.ts` | `ActionContext.wait`; `ActionPlanStep` export. |
| Rust runtime | `js_host_api` (recorder context for `apply`), `state` (per-actor plan table: steps, next index, resumeAt), `ffi_mod/misc/run_iteration` (walk pass), `ffi_mod/misc/emit_action` + `debug_simulate_action` (occupancy check, plan build, start walk), `export_helpers` (in-flight plan rows) | Plan build at dispatch; walk per tick; export for inspection. |
| C# client | `Sources/Module/HostApiSetup.cs` (`registerAction` recorder — currently stores name only), `Sources/Runtime/RuntimeInterop.cs` (`RunIteration` already the tick driver) | Jint host records plans so module code is host-portable; see open item on walker placement. |
| Tests | `application/suite` Cucumber fixtures (new stage: shoot/rest modules, frame-stepped scenarios per `todo.md` control work), Rust unit tests (park/resume/commit, determinism) | |

## Open items

1. **Cooldown anchor**: recorded at dispatch (recommended — matches "cooldown still fires" semantics and keeps the existing rule) or at plan completion? *v1: dispatch (unchanged — the guard/cooldown prefix runs before plan recording).*
2. **Walker placement**: **resolved — Rust runtime walks** (actions are the Rust host's authority per spec). C# Jint host parity (recorder + `temporal`) is deferred; note `RuntimeInterop.emitAction` tries C# handlers first and falls back to Rust — whichever host runs the action must walk it, so C# parity is required before Jint-hosted modules may use `ctx.wait`.
3. **Actor deleted mid-plan**: abort (recommended) vs fail-soft step-skipping. *v1: not implemented — a parked plan outlives actor deletion until it completes (emit of a dead actor's effects is inert via the existing pipeline). Needs a spec sentence.*
4. **Progress reporting**: for UI progress bars, export `{ actorId, actionName, currentStep, resumeAt, totalWaitGTU }` in state deltas — confirm shape when UI work starts. *v1: not exported; `ACTIVE_PLANS` state exists and is the natural source.*
5. **Temporal units**: **resolved for v1** — `ofTicks` raw GTU against the existing elapsed-units counter, shimmed as `hostApi.runtime.temporal.ofTicks`; `defineUnit`/`tickAdvancesBy` follow separately.
6. **Per-actor occupancy** (new, from v1 deviation): **resolved** — the active-plan table is keyed by (actor, action name); the actor id travels on the dispatch wire (`runtime_emit_action_for` / C# `emitAction(action, actor)` / debug-loop second token). While an actor has a parked plan, *every* further action for that actor is rejected at dispatch (never interrupting, never queued) via `state::actor_is_busy`. Unscoped dispatch (no actor on the wire) keeps the name-keyed check. E2E coverage: `Test/Stage_8/ActionPlan/BusyReject` (rest parks → dash for same actor rejected and not queued → plan completes on schedule → dash runs after).

## Verification

1. **Rust unit tests**: plan builds → parks → resumes on correct GTU; emit waves commit with the right ticks; two runs of the same plan produce identical draws; busy rejection while parked.
2. **TSC check**: `mvn -DskipTests=true process-test-resources` in `application/suite` (runs `tsc --noEmit` over `src/test/resources/**/*.js` against `types/**`).
3. **Cucumber/Java**: new stage fixture with a `shoot` module (aim, wait 2, fire) + `rest` module; frame-step the runtime (per `todo.md` step-wise control) and assert state after each frame.
4. **C# client**: `dotnet build` in `application/client/solution`; existing test modules still pass (instant actions unchanged); a test module using `ctx.wait` end-to-end.

## Out of scope

- Interruption/cancellation (`ctx.canInterrupt()` marker, `cancelAction` wire message) — future, noted in spec as reserved.
- New step vocabulary beyond `emitEvent` + `wait` (e.g. `setEntity` in plans).
- Implementing the full `TemporalExpression` unit system (`defineUnit`, `tickAdvancesBy`).
- Client-side progress rendering (UI work, separate plan).
