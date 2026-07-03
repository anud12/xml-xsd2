# Runtime Specification

This document is the **entrypoint** for the runtime specification. It describes the purpose of the project, how the documents are organised, and the recommended reading order for different audiences.

---

## What is this project?

This specification describes a **deterministic, module-driven game runtime**. The runtime:

- Loads sandboxed **Modules** (ZIP archives containing ESM JavaScript) that declare rules, events, and effects as lazy expression ASTs.
- Maintains a **world state** of Entities and Containers, updated atomically via a double-buffer commit pipeline.
- Accepts **Actions** from connected clients over WebSocket as the sole external entrypoint.
- Executes **Effects** (prepare → apply → commit) deterministically using a stateless PRNG keyed by an `ExecutionContext`.
- Synchronises state to clients as minimal deltas; the **client runs the same runtime**, so only synchronisation — not capability negotiation — is needed.

The specification is **implementation-agnostic**: it describes behaviour and contracts, not the language or platform used to implement them.

---

## Document Map

### 🏗 Runtime & Architecture

| Document | Description |
|---|---|
| [`runtime.md`](./runtime/runtime.md) | Core runtime: module lifecycle, double-buffer commit pipeline, ExecutionContext, concurrency model, persistence, networking. **Start here for implementers.** |
| [`modules.md`](./runtime/modules.md) | Module system: ZIP layout, manifest, sandboxed ESM execution, HostApi surface. |
| [`randomness.md`](./runtime/randomness.md) | Deterministic, stateless PRNG (SplitMix64) keyed by ExecutionContext. Guarantees runtime-client parity. |

---

### ⚡ Interaction

| Document | Description |
|---|---|
| [`actions.md`](./interaction/actions.md) | **Actions** — sole external entrypoint. Client sends a named action over WebSocket. Modules register actions with a guard, cooldown, and Effect pipeline (DAG). |
| [`effects.md`](./interaction/effects.md) | **Effects** — declarative state transitions. `prepare` (read-only) → `apply` (record mutations) → `commit` (atomic write). Supports reoccurrence via `TemporalExpression`. |

---

### 📦 Data Model

| Document | Description |
|---|---|
| [`entities.md`](./data-model/entities.md) | **Entity** — atomic world object. Has `id`, `textMap`, `numberMap`, and container memberships. Includes `EntityExpression` builder API. |
| [`containers.md`](./data-model/containers.md) | **Container** — holds Entities. Supports position, span, and size (slot/grid). Includes `ContainerExpression` builder API. |
| [`textMap.md`](./data-model/textMap.md) | Keyed string map (`TextMap`) expression builder. `StringExpression` values attached to Entities and Containers. |
| [`numberMap.md`](./data-model/numberMap.md) | Keyed number map (`NumberMap`) expression builder. `NumberExpression` values attached to Entities and Containers. |

---

### 🔍 Queries & Filters

| Document | Description |
|---|---|
| [`entityFilter.md`](./queries/entityFilter.md) | Declarative, composable filter for selecting Entities by id, text/number map values, container membership, or classification. |
| [`containerFilter.md`](./queries/containerFilter.md) | Declarative, composable filter for selecting Containers by id, type, classification, metadata, or entity membership. |
| [`repository.md`](./queries/repository.md) | Read-only query facade (`getEntities`, `getContainers`). Index-backed, snapshot-consistent per tick. |

---

### 🧮 Expression Primitives

All expressions are **immutable** and **lazily evaluated**. The runtime evaluates them at commit time.

| Document | Description |
|---|---|
| [`numberExpression.md`](./expressions/numberExpression.md) | 64-bit signed integer arithmetic. `of`, `sum`, `subtract`, `multiply`, `divide`, `random`, comparisons. |
| [`stringExpression.md`](./expressions/stringExpression.md) | String construction. `of`, `concat`, `join`, `oneOf` (deterministic choice), `ref` (rule lookup). |
| [`conditionExpression.md`](./expressions/conditionExpression.md) | Boolean logic. `of`, `and`, `or`, `negate`, `ifTrue`, `ifFalse`. Short-circuit evaluation. |
| [`listExpression.md`](./expressions/listExpression.md) | Ordered sequences. `of`, `concat`, `append`, `get`, `length`, `map`, `forEach`, `randomElement`, `oneOf`. |
| [`maybeExpression.md`](./expressions/maybeExpression.md) | Optional values (Some / None). `map`, `flatMap`, `filter`, `orElse`, `ifPresent`. Fail-soft by default. |
| [`temporalExpression.md`](./expressions/temporalExpression.md) | In-game time durations. Module-defined named units (e.g. `"round"`, `"day"`) mapped to an internal GTU clock advancing per tick. Used for cooldowns and effect reoccurrence. |
| [`textMap.md`](./data-model/textMap.md) | `TextMapExpression` builder — keyed `StringExpression` map. |
| [`numberMap.md`](./data-model/numberMap.md) | `NumberMapExpression` builder — keyed `NumberExpression` map. |

> **Type declarations** for the expression primitives live in [`types/`](./types/).

---

### 🖥 User Interface

| Document | Description |
|---|---|
| [`user-interface.md`](./user-interface/overview.md) | Layout-first UI system. Declarative component tree; runtime handles layout, rendering and event delivery. Links to sub-documents for Panel, Layout, Division and Text primitives. |

---

### 🗂 Legacy & Internal

| Document | Description |
|---|---|
| [`zones.md`](./legacy/zones.md) | *(Legacy)* Extracted zones/regions behaviour from the sibling implementation. Superseded by Containers. Retained for reference during migration. |
| [`srpites.md`](./legacy/srpites.md) | *(Draft)* Sprite format using OpenEXR with channel-encoded destination mapping. |
| [`todo.md`](./todo.md) | *(Internal)* Backlog of pending design and migration tasks. Not normative. |

---

## Recommended Reading Order

### For a new **implementer**
1. `runtime.md` — understand the execution model
2. `randomness.md` — understand determinism
3. `modules.md` — understand how behaviour enters the system
4. `entities.md` + `containers.md` — understand the world model
5. `effects.md` — understand state transitions
6. `actions.md` — understand the client boundary
7. Expression primitives as needed

### For a **module author**
1. `modules.md` — entry script, HostApi, sandbox constraints
2. `actions.md` — how to register actions
3. `effects.md` — how to declare state changes
4. Expression primitives (`number`, `string`, `condition`, `list`, `maybe`, `temporal`)
5. `entityFilter.md` + `containerFilter.md` — how to query the world
6. `repository.md` — how to read world state

### For a **client developer**
1. `actions.md` — wire message format
2. `runtime.md` — synchronisation model (delta streaming, hot-reload resync)
3. `entities.md` + `containers.md` — world model shape
4. `user-interface.md` — UI primitives

---

## Cross-Cutting Concerns

| Concern | Where it lives |
|---|---|
| Determinism | `randomness.md`, `runtime.md` (ExecutionContext) |
| Atomicity | `effects.md` (double-buffer commit), `runtime.md` |
| Sandboxing | `modules.md`, `runtime.md` (security section) |
| In-game time | `temporalExpression.md` |
| Client-server parity | `actions.md` (client-server parity section), `runtime.md` |
| Error codes | `effects.md` (`E_RECURSION_UNPROVEN_VALIDATION_FAIL`), `actions.md` (`E_PIPELINE_CYCLE`), `temporalExpression.md` (`E_TEMPORAL_*`) |
