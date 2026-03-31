# User Interface — Overview

## Purpose

The UI system is a **shell layered on top of the runtime**. Modules declare the entire UI structure at load time via `HostApi.ui`. The client is a dumb renderer: it receives an evaluated UI tree each tick and renders it. There is no client-side UI logic.

---

## Design principles

- **Runtime-owned**: modules declare UI at load time; the client only renders what the runtime sends.
- **Expression-bound**: visibility, size, and content are driven by the same expression primitives used across the spec (`NumberExpression`, `StringExpression`, `ConditionExpression`).
- **Resolution-independent**: all sizes are in logical units scaled by a global UI scale factor, with optional per-panel overrides.
- **State-bound**: components bind to per-client UI state values (actor, and module-declared values) for interactive state.
- **Minimal primitives**: `Panel`, `Box`, `TextValue`, and `NumberValue` are the initial building blocks.
- **Conditional by exclusion**: children are rendered by being present in the `children` array; absent means not rendered. There are no visibility flags on components.

---

## Documents

| Document | Description |
|---|---|
| [`panel.md`](./panel.md) | A positioned, sized UI window anchored to the screen. Top-level; never nested. Declares default positioning and a `render` function. |
| [`box.md`](./box.md) | A layout block inside a panel. Grid-based; forms the content tree. Defines `Child`, `SizeConstraint`, `TrackDefinition`. |
| [`text-value.md`](./text-value.md) | Leaf component. Displays a `StringExpression` — typically a `TextMap` value. |
| [`number-value.md`](./number-value.md) | Leaf component. Displays a `NumberExpression` with optional formatting — typically a `NumberMap` value. |
| [`ui-state.md`](./ui-state.md) | Per-client UI state. `actor` (built-in) and module-declared values (`declare`/`value`). Drives interactive bindings. |
