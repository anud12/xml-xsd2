# User Initialization Action

## Summary

`userInitialization` is a **custom action type** that serves as the **sole entry point for new user onboarding** in the runtime. It is externally triggered (by an authenticated service layer), runs once per user, and atomically establishes all initial user state: the user entity, inventory container, starting equipment, and other bootstrapped resources.

The action is **idempotent by design**: a second initialization attempt for the same user name will be rejected by the guard. This ensures users cannot be accidentally re-initialized.

---

## Purpose

`userInitialization` provides:

- **Single trusted entry point**: only the service layer can dispatch user initialization; guard prevents duplicates
- **Atomic bootstrap**: all user state (entity, containers, child events) commits together
- **Event chaining**: emits `UserInitialized` event, which cascades child events (e.g., `EquipStartingGear`, `JournalInit`)
- **Authorization separation**: authorization is enforced upstream by the service layer; runtime guard only checks "already initialized"
- **Client error transparency**: clients receive explicit error messages and can retry intelligently

---

## Wire Message

```ts
type UserInitializationMessage = {
  actionName: "userInitialization";
  actorEntityId: UniqueGlobalEntityId;  // caller (pre-authorized by service layer)
  user: {
    name: string;
  }
}
```

**Preconditions** (enforced by service layer upstream):
- `actorEntityId` belongs to the authenticated session
- Caller has permission to initialize users (e.g., "admin" role)
- `user.name` is valid (non-empty, within acceptable length, etc.)

**Note**: By the time this message reaches the runtime, the caller is already authenticated. The runtime performs an additional ownership check at step [2] of processing, but assumes the caller is authorized by the service layer.

---

## Registration API

```ts
hostApi.action.registerCustomAction({
  name: "userInitialization",
  paramType: { name: string },
  
  guard: (context, userMetadata) => {
    // Acquire lock to prevent concurrent initialization of same user
    const lock = context.acquireLock(`user-init:${userMetadata.name}`);
    
    // Check: does a user with this name already exist?
    const existingUser = context.queryEntities({
      filter: (e) => e.getText("name") === userMetadata.name
    });
    
    return existingUser.length === 0;
  },
  
  apply: (context, userMetadata) => {
    // Create user entity with default attributes
    const userEntity = hostApi.entity.create()
      .withText("name", userMetadata.name)
      .withNumber("level", 1)
      .withNumber("experience", 0)
      .withText("status", "active");
    
    const userId = context.registerEntity(userEntity);
    
    // Create user's inventory container
    const inventory = hostApi.container.create()
      .withGetX((entity) => entity.number_map.get("slotIndex")
        .orElse(hostApi.number.of(0)))
      .withGetY((entity) => hostApi.number.of(0))
      .withGetSpanX((entity) => entity.number_map.get("slotSpan").orElse(hostApi.number.of(1)))
      .withGetSpanY((entity) => hostApi.number.of(1))
      .withSize(hostApi.number.of(20), "clamp")
      .withEntity(/* default starting items */);
    
    context.registerContainer(userId, "inventory", inventory);
    
    // Emit UserInitialized event → triggers cascading initialization
    context.emitEvent("UserInitialized", {
      userId,
      userName: userMetadata.name,
      timestamp: context.tick()
    });
  },
  
  cooldown: null,  // One-time action; no cooldown
})
```

---

## Event Chaining: UserInitialized

The `UserInitialized` event triggers cascading sub-system initialization:

```ts
hostApi.event.register({
  name: "UserInitialized",
  apply: (context, payload) => {
    const { userId, userName, timestamp } = payload;
    
    // Emit child events for parallel initialization
    context.emitEvent("EquipStartingGear", { userId });
    context.emitEvent("JournalInit", { userId });
    context.emitEvent("StatsInit", { userId });
    context.emitEvent("NotificationInit", { userId, userName, timestamp });
  }
})
```

**Semantics**:
- All child events' `prepare` phases run synchronously (read-only)
- All child events' `apply` phases are enqueued
- If a child event's `prepare` throws, dependent emissions are skipped; independent ones continue
- **All state changes** (parent + children) are committed atomically in a single wave

---

## Runtime Processing Flow

```
[1] Receive UserInitializationMessage over WebSocket
    │  Validate wire shape (actionName, actorEntityId, user.name)
    │  → Malformed: reject with parse error, discard

[2] Resolve actorEntityId (caller)
    │  → Not found: reject with auth error
    │  → Entity not owned by session: reject with auth error

[3] Look up registered action by actionName
    │  → Unknown: reject with error

[4] Evaluate guard ConditionExpression (read-only, current read-buffer)
    │  a. Acquire lock: `user-init:{userName}`
    │  b. Query for existing user with same name
    │  → True (user exists): guard fails; reject with "AlreadyInitialized" error
    │  → Exception or throws: log exception, treat as False, reject
    │  (Lock held through step [10]; released after commit)

[5] No cooldown check (cooldown = null; one-time action only)

[6] Acquire per-actor action lock (serialize all actions from caller)

[7] Create ExecutionContext (seed, tick, source=actorEntityId, actionId="userInitialization")

[8] Invoke apply function:
    │  a. Call apply(actionContext, user metadata) synchronously
    │  b. Create user entity → write-buffer
    │  c. Create inventory container → write-buffer
    │  d. Emit UserInitialized event via context.emitEvent()
    │  e. UserInitialized.prepare() runs (read-only)
    │  f. Child events emitted: EquipStartingGear, JournalInit, StatsInit, etc.
    │  g. Each child event's prepare() runs synchronously (read-only)
    │  h. All apply phases enqueued
    │  i. Apply queue executes all apply callbacks
    │  j. Single atomic commit:
    │     - User entity
    │     - Inventory container
    │     - All event mutations (equipment, journal entries, stats, etc.)
    │  k. If apply() throws: exception logged; no state written; error returned

[9] Release per-actor action lock
    (User-name lock released after commit)

[10] On successful commit:
     │  Record new user entity in state.db
     │  Stream state deltas to all connected clients
     │  (Nothing special recorded; action is terminal)

[11] On error:
     │  Return structured error to client:
     │  {
     │    error: "AlreadyInitialized" | "Unauthorized" | "InternalError",
     │    message: human-readable reason,
     │    retryable: boolean
     │  }
     │  Client displays error and decides whether to retry
```

---

## Error Responses

| Error | Cause | Retryable | Client Action |
|---|---|---|---|
| `AlreadyInitialized` | User with this name already exists | No | Show error; user must choose different name |
| `Unauthorized` | Caller not authorized to initialize users | No | Show auth error; user logs in differently |
| `InvalidUserMetadata` | User name empty, too long, or contains invalid chars | No | Show validation error; user corrects input |
| `InternalError` | Exception during apply or commit | Yes | Retry with backoff after delay |
| `DatabaseUnavailable` | SQLite commit failed | Yes | Retry with backoff; may indicate service degradation |

---

## Failure Modes & Edge Cases

| Scenario | Mitigation |
|---|---|
| **User already initialized** | Guard with name lock prevents duplicate; rejects with `AlreadyInitialized` error |
| **Two concurrent initializations, same name** | Lock acquired in guard step [4]; only one proceeds; second rejects |
| **Two concurrent initializations, different names, same caller** | Per-actor lock serializes; both execute sequentially; both succeed if guards pass |
| **Apply() throws during user creation** | Exception logged; transaction rolled back; error returned; client retries or gives up |
| **Event emission fails (e.g., EquipStartingGear.prepare() throws)** | Dependent child events skipped; independent ones continue; only successful mutations committed; error logged |
| **Child event failure should fail entire initialization** | Configure child events with `failFast: true` if this is desired; document cascading failure semantics |
| **User entity creation succeeds, container creation fails** | Atomic commit rolls back; neither user nor container created; error returned |
| **Database unavailable during commit** | Entire action rolls back; error returned; client retries later |
| **Client sends duplicate UserInitializationMessage (retry)** | Guard rejects on second attempt (user exists); no duplicate user created; `AlreadyInitialized` error |
| **Service layer sends invalid user metadata** | Runtime guard validates guard logic; recommend service layer pre-validates; reject early if name is empty |
| **Lock timeout (initialization takes too long)** | Lock should not timeout during execution; released only after commit; configure per deployment SLAs |
| **NameLock deadlock (two initializations of same user, alternating actors)** | Per-actor lock + name lock together prevent deadlock; only one actor can hold name lock at a time |

---

## Concurrency & Idempotency

### Idempotency

`userInitialization` is **idempotent by guard**:

- First call: user doesn't exist → guard passes → user created
- Second call: user exists → guard fails → rejected with `AlreadyInitialized`
- **Result**: calling twice with the same user name always produces the same outcome (either success once, or failure on retry)

### Race Condition Prevention

**Scenario**: Two services both try to initialize user "Alice" simultaneously (from different actors).

**Without lock**:
```
Service A guard: "Alice" not found → pass (stale read-buffer)
Service B guard: "Alice" not found → pass (same stale read-buffer)
Both apply → commit conflict → undefined behavior
```

**With name lock** (acquired in guard):
```
Service A guard: acquire lock user-init:Alice → "Alice" not found → pass (lock held)
Service B guard: acquire lock user-init:Alice → WAIT (blocked on lock)
Service A apply + commit → release lock
Service B guard: acquire lock user-init:Alice → "Alice" now found → fail with AlreadyInitialized
```

**Guarantee**: Exactly one user entity created; second request receives explicit error.

---

## Authorization Model

Authorization is **not enforced by the runtime**; it is the responsibility of the **service layer**:

1. **Service layer**:
   - Authenticate the caller (who is requesting user initialization?)
   - Authorize the caller (do they have permission to initialize users?)
   - Validate user metadata (is the name valid?)
   - Send `UserInitializationMessage` to runtime

2. **Runtime**:
   - Re-check caller owns `actorEntityId` (step [2])
   - Guard checks only: does user already exist?
   - No role/permission logic in runtime

**Rationale**: 
- Simplifies runtime guard logic
- Allows service layer to implement complex auth policies (time-based, quota-based, etc.)
- Reduces runtime complexity and improves performance

---

## Typical Use-Case Flow

```
1. User signs up via web/mobile UI
   └─ Service validates credentials & metadata

2. Service layer calls initialize endpoint
   └─ POST /api/users/initialize { name: "Alice" }

3. Service authorizes caller (session token valid?)
   └─ If unauthorized: reject client, don't contact runtime

4. Service sends UserInitializationMessage to runtime
   └─ { actionName: "userInitialization", actorEntityId: SERVER_ACTOR, user: { name: "Alice" } }

5. Runtime processes:
   ├─ Verify SERVER_ACTOR owns session
   ├─ Guard: user "Alice" not found? → Yes, proceed
   ├─ Apply: create user, inventory, equipment, journal
   ├─ Emit UserInitialized → cascading events
   └─ Atomic commit

6. Runtime returns success to service

7. Service returns { userId, created: true } to client

8. Client fetches updated state via resync
   └─ Sees new user entity, containers, items
```

---

## Implementation Checklist

- [ ] Define `registerCustomAction()` in action registry (handles non-standard action types)
- [ ] Implement `context.acquireLock()` method for guard phase lock acquisition
- [ ] Implement `context.registerEntity()` to add entity to write-buffer during apply
- [ ] Implement `context.registerContainer()` to add container to write-buffer during apply
- [ ] Define guard callback signature: `(context, paramType) => boolean`
- [ ] Define apply callback signature: `(context, paramType) => void`
- [ ] Guard acquires name lock: `context.acquireLock('user-init:' + name)`
- [ ] Guard queries for existing user entity with same name
- [ ] Apply creates user entity with default attributes (level, experience, status)
- [ ] Apply creates inventory container (1D slot-based)
- [ ] Apply emits `UserInitialized` event
- [ ] Register child events (EquipStartingGear, JournalInit, StatsInit, etc.)
- [ ] Verify event chaining cascades correctly (child events' prepare runs sync)
- [ ] Atomic commit: all user + container + event mutations in single transaction
- [ ] On error: return structured error with `retryable` flag
- [ ] Test idempotency: second initialization of same user rejected
- [ ] Test concurrency: two simultaneous initializations of same user (only one succeeds)
- [ ] Test event failure isolation: child event failure doesn't crash parent
- [ ] Document lock timeout behavior and configuration
- [ ] Verify per-actor serialization enforces sequential execution for same caller
- [ ] Add logging/observability for initialization success and failures

---

## Cross-References

- [`actions.md`](./actions.md) — Core action processing flow, authorization model
- [`effects.md`](./effects.md) — Event registration, prepare/apply semantics, event chaining
- [`entity-action.md`](./entity-action.md) — Entity action registration (contrast with custom actions)
- [`container-action.md`](./container-action.md) — Container action registration
- [`no-input-action.md`](./no-input-action.md) — No-input action registration
- [`entities.md`](../data-model/entities.md) — EntityExpression, default attributes
- [`containers.md`](../data-model/containers.md) — ContainerExpression, inventory model
- [`runtime.md`](../runtime/runtime.md) — ExecutionContext, double-buffer commit, locking semantics

---

## Notes & Future Enhancements

1. **Bulk Initialization**: If 1000s of users need initialization simultaneously, consider batching via separate bulk action or async job queue (outside runtime scope).

2. **Initialization Templates**: Consider allowing service layer to specify which child events to emit (e.g., some users skip equipment, some skip journal). Could be added to `user` metadata: `{ name, template: "default" | "minimal" }`.

3. **Rollback & Compensation**: If initialization fails midway, current behavior is to discard all state. Consider adding a `UserInitializationRolledBack` event for cleanup (e.g., logging, notifications).

4. **Audit Logging**: Track all user initializations (timestamp, caller, user name) in a separate audit log for compliance.

5. **Soft Delete**: Consider whether users can be "uninitialized" (soft delete) and re-initialized; impacts guard logic if names are reusable.
