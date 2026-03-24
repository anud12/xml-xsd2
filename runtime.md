# Runtime — Implementation & Operational Specification

## Overview

This document specifies the runtime: the in-memory execution environment that loads module ASTs, evaluates event/effect logic deterministically, and exposes a snapshot-consistent repository to clients and modules. The runtime's primary goals are:

- Deterministic execution and reproducible randomness per `ExecutionContext`.
- High-throughput, memory-efficient evaluation (SIMD-friendly flat data layout where practical).
- Safe sandboxed module loading (modules declare ASTs; the runtime executes only the cached AST).
- Atomic, transactional state updates via a double-buffer commit pipeline.
- Horizontally-scalable action processing with thread-safe, snapshot-consistent reads.

---

## Key properties

- Ticked execution: frame-driven loop (30 fps) processes queued actions and commits staged writes once per tick. Clients receive delta updates.
- One WebSocket per connected user; authenticated sessions map socket → entity/actor id.
- Modules are ZIP archives containing sandboxed ESM entrypoints; they are validated, sandboxed, and converted into ASTs at load time.
- Double-buffering ensures commits are atomic and the read buffer never observes partially applied mutations.
- Deterministic randomness is derived from a stateless PRNG (see randomness.md) keyed by the `ExecutionContext`.

---

## Module lifecycle

1. Upload and validate ZIP: check `manifest.json`, entrypoint existence, limits (size, file types), and requested permissions.
2. Unpack into an isolated in-memory filesystem and execute the entry ESM inside a restricted JS sandbox. No direct access to process, filesystem, network, or host globals unless explicitly proxied via HostApi.
3. The module's entry receives `HostApi` and uses it to declare rules, events, constants and AST nodes. The runtime extracts and validates the declared AST.
4. On successful load, the AST is cached in-memory; the sandbox is torn down.
5. Module reload: validate new ZIP, compute AST diff, apply index rebuild or hot-swap semantics under controlled quiescence.

Security notes:
- Sanitize all module-provided data before using it in core systems.
- Enforce resource quotas per module (CPU time during load, memory usage, permitted host API calls).

---

## Execution context & determinism

Each actionable entrypoint (Action, Event emission, or Entity update) is assigned a freshly constructed `ExecutionContext` that is propagated through the execution chain. The `ExecutionContext` fields: 

- World Seed (session global)
- Tick Identifier (frame number)
- Source Identifier (actor entity id)
- Action / Event Identifier
- Call Index (local counter incremented with each random draw)

Random draws are computed from these coordinates using a 64-bit SplitMix64-based PRNG. Because the `ExecutionContext` is fully derivable from the entrypoint and local call ordering, the runtime and client can reproduce identical choices when provided the same entry parameters.

---

## Event / Effect pipeline (prepare → apply → commit)

The runtime enforces a strict four-stage processing model for events/effects to achieve parallelism and transactional integrity.

1. Emit / Receive Action
   - An incoming action (from client or internal source) is validated, authenticated, and turned into an event entry in the runtime queue.
   - A new `ExecutionContext` is created and attached.

2. Prepare (synchronous, read-only)
   - `prepare` handlers execute in the context of the current read-buffer snapshot. They may inspect state, resolve targets, and emit further events (emitted events enter the same synchronous prepare wave).
   - `prepare` returns a serializable `output` payload. It must not mutate runtime state.
   - `prepare` may call `emitEvent` — the runtime must ensure recursion is finite (recursion guard).

3. Apply (record mutations)
   - `apply` handlers receive `output` and record intended mutations as expression wrappers/intent objects. These records are appended to the current commit record list (they do not mutate the read-buffer).
   - `apply` is allowed to perform side-effect recording only; errors must cause the runtime to drop the recorded changes from this `apply` invocation.

4. Commit (evaluate & write)
   - After prepare/apply for the current synchronous wave complete, the runtime evaluates all recorded expression wrappers against the read-buffer and writes concrete mutations into the write-buffer in the recorded order.
   - If any evaluation throws or validation fails, the commit is aborted and the write-buffer is discarded for that tick; the read-buffer remains unchanged.

5. Swap
   - On successful commit, atomically swap the write-buffer into the read-buffer; the new state becomes visible to subsequent reads.
   - Notify subscribers and stream deltas to clients.

Important semantics:
- Expression wrappers returned by `prepare` or recorded in `apply` are evaluated exactly once at commit time.
- The commit order is the order of recording; modules should not rely on interleaving guarantees other than the recorded order.
- The runtime must provide deterministic conflict resolution semantics for concurrent writes targeted at the same field (prefer recording-time ordering or per-entity write queues).

---

## Recursion guard & validation

- The runtime must validate at module-load time (or first use) that synchronous `prepare → emitEvent` chains are provably finite or impose a safe depth limit (e.g., 256 frames). If this cannot be proven, the runtime rejects the module or operates in strict/validated mode.
- At runtime, a per-chain recursion depth counter prevents runaway synchronous emission. When exceeded, further emits are either rejected or queued asynchronously according to configuration.

---

## Concurrency & threading model

- Independent Actions with distinct `ExecutionContexts` may be processed concurrently on multiple worker threads because the runtime provides **snapshot reads** (immutable read-buffer) and records writes into per-action append-only logs.
- No global locks: design aims to avoid coarse-grained locks and instead use fine-grained synchronization (per-entity or per-region) where required.
- Commit phase must coordinate writes. Two models:
  1. Centralised commit thread: workers record changes; single commit thread evaluates and writes atomically.
  2. Sharded commit: partition world state by shard (zones, regions) and commit per-shard in parallel with ordered per-shard write lists.
- Repository reads are snapshot-consistent: builders and module logic operate on immutable snapshots for the tick.

---

## Repositories & indexing

- At boot and after module loads, the runtime builds index maps (id → Entry) and additional composite indexes for common predicates (zone→region→entity, classification indexes).
- Index rebuilds are atomic and swap-based: build a new index structure, then atomically replace the old one to maintain consistent reads.
- The runtime SHOULD analyze module ASTs to detect commonly-used query patterns and pre-build composite indexes to avoid scan-heavy filters.
- Implement safeguards: max index arity, maximum indexed cardinality, and overall index memory budget.

---

## Memory & performance considerations

- Aim for flat, SIMD-friendly in-memory representations for hot arrays (entity lists, portal lists), keeping per-tick allocations minimal.
- Avoid per-tick GC pressure: reuse buffers, object pools, and pre-allocated arenas where appropriate.
- Enforce per-module and per-world memory quotas to limit noisy neighbors.
- Provide configurable module size limits and synchronous initialization timeouts.

---

## Persistence & durability

- The runtime is primarily in-memory. Persistence of canonical world state should be handled by a dedicated persistence service (snapshot and/or event log) that subscribes to committed deltas.
- Design patterns:
  - Periodic snapshotting of read-buffer state to persistent storage.
  - Append-only change-log (event sourcing) where writes are persisted in order; replay produces identical state when starting from the same seed.
- The runtime must expose checkpoints and consistent snapshot APIs for safe persistence.

---

## Networking & client delta streaming

- Maintain one websocket/socket per connected client; authenticate and map to actor entity id.
- After each successful commit, compute minimal diffs (deltas) between the old and new read-buffer snapshots and stream only relevant changes to clients.
- Provide subscription filters so clients receive only relevant deltas (zone/region/entities they observe).
- Ensure snapshot-consistent delivery: client-visible updates correspond to fully swapped commits only.

---

## Observability & operational practices

- Emit structured logs for module loads, commit failures, validation errors, and resource quota violations.
- Provide metrics: tick latency, commit fail rate, prepare/apply durations, index rebuild duration, memory usage, websocket counts.
- Health checks: liveness (tick loop), readiness (index built, modules loaded), and module-level health.
- Graceful module reload: quiesce new incoming actions for the affected modules, validate AST diffs, rebuild indexes, then swap in new indexes and ASTs atomically.

---

## Security

- Module sandboxing: no Node/OS APIs; interactions only via `HostApi` endpoints which enforce validation and sanitization.
- Permissions model: manifest-declared runtime permissions limit the host API capabilities a module may call.
- Resource limits per module during load (CPU/time/memory) and during runtime (host API quotas).

---

## Testing & validation

- Provide deterministic test harness that constructs `ExecutionContext` values and asserts outcomes (random draws, commit results, deterministic ordering).
- Unit test bodies of `prepare` and `apply` with mocked snapshots.
- Integration tests: module upload → AST extraction → index build → run action → verify commit/deltas.
- Property tests for recursion guard, deterministic `randomFrom` selection, and index rebuild atomicity.

---

## Example action lifecycle

1. Client sends `MoveAction` for entity `E1`.
2. Runtime authenticates socket → action enqueued; create `ExecutionContext` (seed, tick, source=E1, actionId).
3. `prepare(MoveAction)` resolves target region and returns payload.
4. `apply` records movement mutation wrapper (entity position = expression referencing target coords).
5. Commit evaluates expression wrapper (reads from read-buffer to resolve refs), writes concrete mutation into write-buffer.
6. Swap: write-buffer becomes read-buffer. Notify subscribers; stream deltas to relevant clients.

Random choices inside `prepare` or `apply` use the `ExecutionContext`'s call-indexed PRNG, guaranteeing reproducible results.

---

Note: Scheduler functionality has been removed from this runtime specification.

The runtime no longer exposes a scheduler HostApi. Time-driven callbacks and periodic registrations are considered out of scope for this spec and should be implemented by external scheduler services or by module-level patterns that use the runtime's existing tick loop and event emission APIs.

Remove any manifest permissions, quotas, or HostApi references tied to a scheduler when implementing modules against this spec.

## Implementation checklist

- [ ] Implement double-buffer read/write swap with atomic commit.
- [ ] Provide module sandboxing and AST extraction pipeline with manifest validation.
- [ ] Implement deterministic PRNG keyed by `ExecutionContext` and ensure inclusive index semantics for `randomFrom`.
- [ ] Implement recursion guard for synchronous emits and static validation checks at module load.
- [ ] Provide snapshot-consistent repository reads and atomic index swapping.
- [ ] Design commit coordination (centralised commit thread or sharded commit) and document tradeoffs.
- [ ] Add observability: logs, metrics, health endpoints.
- [ ] Add tests for prepare/apply/commit semantics, recursion guard, index rebuilds, and persistence checkpoints.

---

This expanded runtime spec is intended to be the single source of truth for implementers, operators, and test authors. Adjust quotas, timeouts and shard strategies according to deployment needs (hardware, scale, persistence approach).
