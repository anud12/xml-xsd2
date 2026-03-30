# Actions — Concepts

This document describes the core `Actions` concept.

## Summary

An `Action` is the **sole external entrypoint** into the runtime. Clients send Actions over WebSocket to initiate state changes. The runtime validates, guards, and dispatches each Action into a declarative pipeline of Effects. No module code or Effect can originate an Action — only connected clients can.

---

## Purpose

Actions provide a typed, authenticated, rate-limited boundary between clients and the Effect pipeline. Their goals are:

- **Single entrypoint**: all client-initiated state changes flow through Actions.
- **Declarative guard**: eligibility is evaluated as a `ConditionExpression` before any Effect runs.
- **Cooldown control**: per-actor rate-limiting via a [`TemporalExpression`](./temporalExpression.md) *(spec pending)*.
- **Effect pipeline**: fan-out to one or more Effects with an explicit, statically-validated dependency DAG.
- **Client-server parity**: the client runs the same runtime and modules; only state synchronization is needed over the wire.

---

## Wire Message

The client sends a JSON message over WebSocket:

```ts
type ActionMessage = {
  actionName: string;
  actorEntityId: UniqueGlobalEntityId;
  target: ActionTarget;
}

type ActionTarget =
  | { type: "entity";    entityId: UniqueGlobalEntityId }
  | { type: "container"; containerId: UniqueGlobalContainerId }
  | { type: "point";     containerId: UniqueGlobalContainerId; position: ContainerPoint }

// 1D or 2D, matching the target container's declared dimensions
type ContainerPoint =
  | { dimension1: NumberExpression }
  | { dimension1: NumberExpression; dimension2: NumberExpression }
```

---

## Action Registration (Host API)

Modules register Actions using `registerAction`. The runtime validates the declaration at module load time.

```ts
export type HostApi = {
  /* ... rest of declarations ... */
  registerAction: (args: RegisterActionArgs) => void;
}

type RegisterActionArgs = {
  /** Unique name; clients identify this action by name on the wire */
  name: string;
  description?: string;

  /** What kind of target this action accepts */
  targetType: "entity" | "container" | "point";

  /**
   * Evaluated against the read-buffer before the pipeline runs.
   * If false (or throws), the action is rejected and the client receives an error response.
   * Both actor and target are accessible here — use this for eligibility and target validation.
   */
  guard?: (context: ActionContext) => ConditionExpression;

  /**
   * Minimum time between invocations of this cooldownGroup for the same actor.
   * See temporalExpression.md for TemporalExpression semantics.
   */
  cooldown?: (context: ActionContext) => TemporalExpression;

  /**
   * Actions sharing the same cooldownGroup share a per-actor timer.
   * If omitted, the action name is used as its own group (independent cooldown).
   * Example: "attack" and "heavy_attack" both declaring cooldownGroup: "melee"
   * will share a single per-actor cooldown timer.
   */
  cooldownGroup?: string;

  /**
   * DAG of Effects to execute. Nodes with no `after` dependencies run concurrently.
   * All prepare() calls complete before any apply() is invoked.
   * The entire pipeline commits atomically in a single commit.
   */
  pipeline: PipelineNode[];
}

type ActionContext = {
  /** The entity performing the action (resolved from actorEntityId) */
  actor: EntityExpression;
  /** The resolved target from the wire message */
  target: ActionTarget;
}

type PipelineNode = {
  /** Name of the registered Effect to invoke */
  effect: string;

  /**
   * Effect names this node must wait for before its own prepare() begins.
   * Those effects' prepare() outputs are passed to the `input` mapper below.
   * Omit or leave empty for root nodes (no dependencies).
   */
  after?: string[];

  /**
   * Maps ActionContext and upstream prepare() outputs to this effect's declared input.
   * Only needed if this node depends on upstream outputs or needs to inject action context.
   * Omit if the effect takes no input or constructs its own input independently.
   */
  input?: (context: ActionContext, upstream: Record<string, any>) => any;
}
```

---

## ActionContext

Available in `guard`, `cooldown`, and pipeline `input` mappers:

```ts
type ActionContext = {
  actor: EntityExpression;   // entity identified by actorEntityId
  target: ActionTarget;      // the resolved wire target
}
```

---

## Runtime Processing Flow

```
[1] Receive ActionMessage over WebSocket
    │  Validate wire shape (actionName, actorEntityId, target type)
    │  → Malformed: reject with parse error, discard

[2] Resolve actorEntityId
    │  → Not found: reject with auth error
    │  → Entity not owned by this session: reject with auth error

[3] Look up registered Action definition by actionName
    │  → Unknown action: reject with error to client

[4] Validate target type matches Action.targetType
    │  → Mismatch: reject with error to client
    │  For "point" targets: validate dimension count matches container's declared dimensions

[5] Evaluate guard ConditionExpression (read-only, current read-buffer)
    │  → False or throws: reject with error + corrective state delta to client

[6] Check cooldown for (actorEntityId, cooldownGroup ?? actionName)
    │  → Not elapsed: reject with error + corrective state delta to client

[7] Create ExecutionContext (seed, tick, source=actorEntityId, actionId=actionName)

[8] Execute pipeline (DAG):
    │  a. Identify root nodes (no `after` dependencies) → prepare() in parallel
    │  b. As each node's prepare() completes, unblock dependent nodes
    │  c. Repeat until all nodes have completed prepare()
    │  d. Run all apply() calls in topological order
    │  e. Single atomic commit

[9] On successful commit:
    │  Record new cooldown expiry for (actorEntityId, cooldownGroup ?? actionName)
    │  Stream state deltas to relevant clients
```

---

## Pipeline Execution Semantics

- **Parallelism**: Root nodes (no `after` deps) run `prepare()` concurrently. A node begins `prepare()` only once all nodes it depends on have completed their `prepare()`.
- **Output passing**: The `input` mapper receives `upstream` as a map of `{ [effectName]: prepareOutput }` containing only the effects declared in `after`.
- **Single commit**: All `apply()` calls and the commit happen after all `prepare()` calls complete. There are no intermediate commits within a pipeline.
- **Failure isolation**: If a node's `prepare()` throws, all nodes that depend on it (transitively) are skipped. Independent branches of the DAG continue. The final commit includes only the mutations from non-failed branches.
- **Static validation**: DAG cycles are detected at module load time. A module with a cyclic pipeline is rejected with `E_PIPELINE_CYCLE`.
- **ExecutionContext**: All nodes in a pipeline share the same `ExecutionContext` (same seed, tick, actor, action). Each node's random draws increment the shared Call Index deterministically in topological order.

---

## Security

- `actorEntityId` in the wire message must correspond to an entity owned by the sending session. The runtime maintains a session → owned entity set.
- The guard `ConditionExpression` is the module's mechanism for actor and target eligibility checks (role, classification, proximity, etc.).
- All module-provided callbacks (`guard`, `cooldown`, `input` mappers) execute in the sandboxed module environment; they cannot directly mutate state.

---

## Client-Server Parity

The client runs the same runtime and loads the same modules as the server. The WebSocket channel carries only **state synchronization deltas**.

- The client evaluates guards and cooldowns locally against its replica to drive UI (disable unavailable actions, display cooldown timers).
- The server is the authority and re-validates all conditions independently.
- When the server rejects an action, it sends back an error response **and** a corrective state delta to re-align the client's replica.
- On module hot-reload, the runtime pauses, publishes a **full resync** message to all clients, then resumes.

---

## Failure Modes & Edge Cases

| Scenario | Mitigation |
|---|---|
| Malformed wire message | Parse error returned; no state touched |
| Unknown `actorEntityId` | Auth error; no state touched |
| `actorEntityId` not owned by session | Auth error; no state touched |
| Unknown `actionName` | Error returned; module observability log entry |
| Target entity/container deleted between send and processing | Guard or first Effect-level filter returns absent; pipeline short-circuits cleanly |
| Guard throws | Treated as `false`; error returned + corrective delta |
| Cooldown check race (two concurrent messages, same actor) | Cooldown reset is atomic with commit; only one wins; second receives rejection + corrective delta |
| Pipeline DAG cycle | Rejected at module load time with `E_PIPELINE_CYCLE` |
| Upstream Effect `prepare()` throws | Dependent nodes skipped; independent branches continue; commit includes only successful branches |
| `input` mapper throws | Treated as `prepare()` failure for that node |
| `ContainerPoint` dimension count mismatches container arity | Validated at step [4] before any module code runs |
| Module hot-reload mid-session | Runtime pauses, sends full resync to all clients, resumes with new module |
| Client sends duplicate ActionMessage (retry) | Cooldown rejection on second message + corrective delta; idempotency is the cooldown's responsibility |

---

## Examples

### Simple action — single effect

```ts
hostApi.registerAction({
  name: "pickUp",
  targetType: "entity",
  guard: (ctx) => ctx.actor.hasClassification(hostApi.string.of("player"))
    .and(ctx.target.entity.hasClassification(hostApi.string.of("item"))),
  cooldown: (ctx) => hostApi.temporal.seconds(hostApi.number.of(1)), // TODO: TemporalExpression
  pipeline: [
    { effect: "transferEntityToActorInventory" }
  ]
});
```

### Grouped cooldown — two actions share one timer

```ts
hostApi.registerAction({
  name: "attack",
  targetType: "entity",
  cooldownGroup: "melee",
  cooldown: (_ctx) => hostApi.temporal.seconds(hostApi.number.of(1)),
  pipeline: [{ effect: "meleeAttack" }]
});

hostApi.registerAction({
  name: "heavyAttack",
  targetType: "entity",
  cooldownGroup: "melee",
  cooldown: (_ctx) => hostApi.temporal.seconds(hostApi.number.of(1)),
  pipeline: [{ effect: "heavyMeleeAttack" }]
});
```

### Pipeline with dependency — Effect B uses Effect A's output

```ts
hostApi.registerAction({
  name: "loot",
  targetType: "container",
  pipeline: [
    {
      effect: "resolveLootTable",     // root node — runs first
    },
    {
      effect: "spawnLootItems",       // depends on resolveLootTable's output
      after: ["resolveLootTable"],
      input: (ctx, upstream) => ({
        items: upstream["resolveLootTable"].resolvedItems,
        targetContainer: ctx.target,
      }),
    },
  ]
});
```

---

## Cross-References

- [`effects.md`](./effects.md) — Effect registration, prepare/apply/commit semantics
- [`conditionExpression.md`](./conditionExpression.md) — guard expression primitives
- [`temporalExpression.md`](./temporalExpression.md) — cooldown duration expression *(spec pending)*
- [`entities.md`](./entities.md) — EntityExpression used in ActionContext
- [`containers.md`](./containers.md) — ContainerExpression and dimension model
- [`runtime.md`](./runtime.md) — ExecutionContext, double-buffer commit, module sandboxing
- [`randomness.md`](./randomness.md) — deterministic PRNG keyed by ExecutionContext

---

## Implementation Checklist

- [ ] Validate wire message shape and reject malformed inputs
- [ ] Enforce session → owned-entity authorization at step [2]
- [ ] Evaluate guard `ConditionExpression` against read-buffer before pipeline
- [ ] Implement per-actor-per-cooldownGroup cooldown tracking (runtime decides storage)
- [ ] Execute pipeline DAG: parallel root nodes, topological unblocking, single commit
- [ ] Detect and reject cyclic pipeline DAGs at module load time (`E_PIPELINE_CYCLE`)
- [ ] On rejection: return structured error + corrective state delta to client
- [ ] On module hot-reload: pause, full resync to all clients, resume
- [ ] Replace `TemporalExpression` placeholder once spec is finalized