# Actions — Overview

This document describes the core `Actions` concept and wire protocol.

## Summary

An `Action` is the **sole external entrypoint** into the runtime. Clients send Actions over WebSocket to initiate state changes. The runtime validates, guards, and dispatches each Action into a sequence of Effects. No module code or Effect can originate an Action — only connected clients can.

---

## Purpose

Actions provide a typed, authenticated, rate-limited boundary between clients and the Effect pipeline. Their goals are:

- **Single entrypoint**: all client-initiated state changes flow through Actions.
- **Declarative guard**: eligibility is evaluated as a `ConditionExpression` before any Effect runs.
- **Cooldown control**: per-actor rate-limiting via a [`TemporalExpression`](../expressions/temporalExpression.md) *(spec pending)*.
- **Effect pipeline**: emit one or more Effects with explicit side effects.
- **Client-server parity**: the client runs the same runtime and modules; only state synchronization is needed over the wire.

---

## Wire Message

Most actions are targeted. The client sends a JSON message over WebSocket:

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

### No-Input Actions

For no-input actions (registered via `registerAction`), the `target` field is omitted:

```ts
type NoInputActionMessage = {
  actionName: string;
  actorEntityId: UniqueGlobalEntityId;
  // target is omitted
}
```

**Guidance**: 
- Each targeted action targets a **single** entity, container, or point. Bulk operations are handled by:
  - The client sending multiple `ActionMessage` instances (one per target), or
  - The action's `apply` function emitting multiple events in a chain.
- No-input actions are for simple, actor-only operations (rest, meditate, idle, etc.) with no target interaction.

---

## ActionContext

Available in `guard`, `cooldown`, and `apply` callbacks:

```ts
type ActionContext = {
  actor: EntityExpression;                // entity identified by actorEntityId
  emitEvent: (eventName: string, input: any) => any;  // emit events for side effects
}
```

The target is passed as the second parameter to each callback, allowing TypeScript to properly narrow the type based on which registration function was used.

---

## Security

- `actorEntityId` in the wire message must correspond to an entity owned by the sending session. The runtime maintains a session → owned entity set.
- The guard `ConditionExpression` is the module's mechanism for actor and target eligibility checks (role, classification, proximity, etc.).
- All module-provided callbacks (`guard`, `cooldown`, `apply`) execute in the sandboxed module environment; they cannot directly mutate state.

---

## Client-Server Parity

The client runs the same runtime and loads the same modules as the server. The WebSocket channel carries only **state synchronization deltas**.

- The client evaluates guards and cooldowns locally against its replica to drive UI (disable unavailable actions, display cooldown timers).
- The server is the authority and re-validates all conditions independently.
- When the server rejects an action, it sends back an error response **and** a corrective state delta to re-align the client's replica.
- On module hot-reload, the runtime pauses, publishes a **full resync** message to all clients, then resumes.

---

## Action Types

Actions are organized by target type, plus a no-input variant. See specialized documentation for each:

- **[Entity Actions](./entity-action.md)** — Actions targeting a single entity
- **[Container Actions](./container-action.md)** — Actions targeting a single container
- **[Point Actions](./point-action.md)** — Actions targeting a coordinate within a container
- **[No-Input Actions](#no-input-actions)** — Simple actor-only actions with no target

---

## No-Input Actions (Simplified)

No-input actions skip target validation and follow a simpler processing flow:

```
[1] Receive ActionMessage over WebSocket (target field omitted)
    │  Validate wire shape (actionName, actorEntityId)
    │  → Malformed: reject with parse error, discard

[2] Resolve actorEntityId
    │  → Not found: reject with auth error
    │  → Entity not owned by this session: reject with auth error

[3] Look up registered Action definition by actionName
    │  → Unknown action: reject with error to client

[4] Evaluate guard ConditionExpression (read-only, current read-buffer)
    │  → False or throws: reject with error + corrective state delta to client

[5] Check cooldown for (actorEntityId, cooldownGroup ?? actionName)
    │  → Not elapsed: reject with error + corrective state delta to client

[6] Acquire per-actor action lock (serialize all actions from this actor)

[7] Create ExecutionContext (seed, tick, source=actorEntityId, actionId=actionName)

[8] Invoke apply function:
    │  a. Call apply(actionContext) synchronously (no target parameter)
    │  b. Events emitted via context.emitEvent() run synchronously (prepare phase)
    │  c. Runtime enqueues apply phases for emitted events
    │  d. All apply() calls for current wave complete
    │  e. Single atomic commit
    │  f. If apply() throws: cooldown still fires; exception propagated to client;
    │     partial events already emitted stand

[9] Release per-actor action lock

[10] On successful commit:
    │  Record new cooldown expiry for (actorEntityId, cooldownGroup ?? actionName)
    │  Stream state deltas to relevant clients
```

**Key Difference**: No-input actions skip the target validation step entirely, making them faster for simple actor-only operations.

---

## Common Semantics

### Runtime Processing Flow (Targeted Actions)

```
[1] Receive ActionMessage over WebSocket
    │  Validate wire shape (actionName, actorEntityId, target type)
    │  → Malformed: reject with parse error, discard

[2] Resolve actorEntityId
    │  → Not found: reject with auth error
    │  → Entity not owned by this session: reject with auth error

[3] Look up registered Action definition by actionName
    │  → Unknown action: reject with error to client

[4] Validate target type matches action's registered type
    │  → Mismatch: reject with error to client
    │  For "point" targets: validate dimension count matches container's declared dimensions

[5] Evaluate guard ConditionExpression (read-only, current read-buffer)
    │  → False or throws: reject with error + corrective state delta to client

[6] Check cooldown for (actorEntityId, cooldownGroup ?? actionName)
    │  → Not elapsed: reject with error + corrective state delta to client

[7] Acquire per-actor action lock (serialize all actions from this actor)

[8] Create ExecutionContext (seed, tick, source=actorEntityId, actionId=actionName)

[9] Invoke apply function:
    │  a. Call apply(actionContext, target) synchronously
    │  b. Events emitted via context.emitEvent() run synchronously (prepare phase)
    │  c. Runtime enqueues apply phases for emitted events
    │  d. All apply() calls for current wave complete
    │  e. Single atomic commit
    │  f. If apply() throws: cooldown still fires; exception propagated to client;
    │     partial events already emitted stand

[10] Release per-actor action lock

[11] On successful commit:
    │  Record new cooldown expiry for (actorEntityId, cooldownGroup ?? actionName)
    │  Stream state deltas to relevant clients
```

### Apply Function & Event Emission

- **Synchronous execution**: The `apply` function runs synchronously and may emit events via `context.emitEvent()`.
- **Event chaining**: Events emitted from `apply` have their `prepare` phase run immediately (read-only). The event's `apply` phase is enqueued.
- **Recursion guard**: Cross-event emission is guarded against infinite recursion via depth limits, stack detection, or cycle detection as per the Events specification.
- **Single commit**: All mutations from the action and its emitted events are committed atomically after all `apply` calls complete.
- **Failure isolation**: If an event's `prepare` throws, dependent emissions are skipped but independent branches continue. The commit includes only successful mutations.
- **ExecutionContext**: All events emitted from an action share the same `ExecutionContext`. Random draws increment the shared Call Index deterministically.

---

## Failure Modes & Edge Cases

| Scenario | Mitigation |
|---|---|
| Malformed wire message | Parse error returned; no state touched |
| Unknown `actorEntityId` | Auth error; no state touched |
| `actorEntityId` not owned by session | Auth error; no state touched |
| Unknown `actionName` | Error returned; module observability log entry |
| Target entity/container deleted between send and processing | Guard returns `false`; action rejected |
| Guard throws (exception, not false) | Exception logged; treated as `false`; error returned + corrective delta |
| `apply()` throws (exception, not false) | Exception logged and propagated to client; cooldown still fires; partial events already emitted stand |
| Event emission fails | Event logged; independent events continue; committed state includes successful event emissions |
| Cooldown check race (two concurrent actions, same actor) | Per-actor lock serializes; only one executes; second queued and rejected when checked again (cooldown active) |
| Two actions from same actor arrive simultaneously | Per-actor lock enforces sequential execution; second action waits for first to complete |
| `ContainerPoint` dimension count mismatches container arity | Validated at step [4] before any module code runs |
| Module hot-reload mid-session | Runtime pauses, sends full resync to all clients, resumes with new module |
| Client sends duplicate ActionMessage (retry) | Cooldown rejection on second message + corrective delta; idempotency guaranteed by cooldown |

---

## Cross-References

- [`entity-action.md`](./entity-action.md) — Entity action registration and examples
- [`container-action.md`](./container-action.md) — Container action registration and examples
- [`point-action.md`](./point-action.md) — Point action registration and examples
- [`effects.md`](./effects.md) — Effect registration, prepare/apply/commit semantics
- [`conditionExpression.md`](../expressions/conditionExpression.md) — guard expression primitives
- [`temporalExpression.md`](../expressions/temporalExpression.md) — cooldown duration expression *(spec pending)*
- [`entities.md`](../data-model/entities.md) — EntityExpression used in ActionContext
- [`containers.md`](../data-model/containers.md) — ContainerExpression and dimension model
- [`runtime.md`](../runtime/runtime.md) — ExecutionContext, double-buffer commit, module sandboxing

---

## Implementation Checklist

- [ ] Validate wire message shape and reject malformed inputs
- [ ] Enforce session → owned-entity authorization at step [2]
- [ ] Implement unified action registry (stores entity/container/point registrations, dispatches by name)
- [ ] Implement per-actor action queue with mutex (serialize all actions from one actor)
- [ ] Implement per-actor-per-cooldownGroup cooldown tracking with defaults to action name
- [ ] Evaluate guard `ConditionExpression` against read-buffer before apply
- [ ] Execute apply function synchronously; emitted events run prepare phase immediately
- [ ] Catch apply() exceptions: log, propagate to client, fire cooldown, stand partial events
- [ ] Independent event emission: catch event failures, log, continue with remaining events
- [ ] Implement recursion guard for cross-event emission (per effects.md)
- [ ] Single atomic commit: all mutations from action and events committed together
- [ ] On rejection: return structured error + corrective state delta to client
- [ ] Per-actor serialization: queue pending actions when one is in-flight
- [ ] On module hot-reload: pause, full resync to all clients, resume
- [ ] Replace `TemporalExpression` placeholder once spec is finalized
