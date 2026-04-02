# User Interface — Overview

## Purpose

The UI system is a **shell layered on top of the runtime**. Modules declare the entire UI structure at load time via `HostApi.ui`. The client is a dumb renderer: it receives an evaluated UI tree each tick and renders it. There is no client-side UI logic.

---

## Design principles

- **Runtime-owned**: modules declare UI at load time; the client only renders what the runtime sends.
- **Expression-bound**: visibility, size, and content are driven by the same expression primitives used across the spec (`NumberExpression`, `StringExpression`, `ConditionExpression`).
- **Resolution-independent**: all sizes are in logical units scaled by a global UI scale factor, with optional per-panel overrides.
- **State-bound**: components bind to per-client UI state values (actor, and module-declared values) for interactive state.
- **Minimal primitives**: `Panel`, `Box`, `TextValue` are the initial building blocks.
- **Conditional by exclusion**: children are rendered by being present in the `children` array; absent means not rendered. There are no visibility flags on components.
- **Identity-stable**: every component (`Box`, `TextValue`) carries a mandatory `id`. The runtime reconciliation algorithm uses these ids to diff successive evaluations of the expression DAG — matching same-id nodes across ticks so that only changed values are sent to the client. Ids must be unique within their parent `children` array.

---

## Documents

| Document | Description |
|---|---|
| [`concepts.md`](./concepts.md) | Shared concepts: component identity, size constraints, anchor positioning, conditional rendering, state binding. |
| [`panel.md`](./panel.md) | Top-level positioned UI window. Defines anchor/pivot/offset positioning, default state, child callback. |
| [`box.md`](./box.md) | Layout block using grid model. Defines columns, auto-placement, track alignment, common patterns. |
| [`text-value.md`](./text-value.md) | Leaf component. Displays a `StringExpression` — typically a `TextMap` value. |
| [`ui-state.md`](./ui-state.md) | Per-client UI state. `actor` (built-in) and module-declared values. Drives interactive bindings. |
| [`rendering.md`](./rendering.md) | Rendering model: coordinate system, z-ordering, resource resolution, overflow semantics, evaluation tick. |
