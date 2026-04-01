# UI Concepts

Shared concepts used across all UI components.

---

## Component Identity

Every UI component (`Panel`, `Box`, `TextValue`) requires a unique `id` string:

```ts
hostApi.ui.panel("stats-panel", { /* ... */ }, ...)
hostApi.ui.box("content", { /* ... */ }, ...)
hostApi.ui.text("label", { value: ... })
```

**Why:** The runtime reconciliation algorithm uses ids to diff successive evaluations — matching same-id nodes across ticks to determine what changed.

**Scope:** Ids must be unique within their parent's `children` array. Not globally unique.

**Stability:** Declared at load time (inside callbacks); always constants — never dynamic.

---

## Size Constraints

All components accept a `size` option with per-axis constraints:

```ts
type SizeConstraint = {
  min?: NumberExpression;    // Never sized below this (logical units)
  max?: NumberExpression;    // Never sized above this (logical units)
  scale?: NumberExpression;  // Flex weight; claims remaining space after min
};

type ChildSize = {
  width?: SizeConstraint;
  height?: SizeConstraint;
  anchor?: { x?: NumberExpression; y?: NumberExpression };
};
```

**Scale behavior:** Like CSS `flex-grow`. A component with `scale: 2` claims twice as much remaining space as one with `scale: 1`. Defaults to 0 (sized to min, or content if no min).

---

## Anchor Positioning

Components can anchor within their cell to control positioning and growth direction:

- **0 (edge)**: Anchors to start/top; content grows rightward/downward
- **0.5 (center)**: Anchors to center; content grows symmetrically
- **1 (edge)**: Anchors to end/bottom; content grows leftward/upward

Values outside [0, 1] clamp. When content exceeds min/max, it clamps and anchor repositions to maintain intent.

**Per-axis independent:** `x` and `y` work separately. Asymmetric anchors allowed.

---

## Conditional Rendering

There is no `visible` flag. To conditionally render a child, **exclude it from the `children` array**.

Presence = rendered; absence = not rendered.

Since `children` is called once at load time with proxy objects, conditional logic uses expression combinators:

```ts
hostApi.ui.box("content", { /* ... */ }, (state, data) => [
  hostApi.ui.text("label", { value: string.of("Always visible") }),
  ...state.value("target").asEntity
    .map(e => [hostApi.ui.text("conditional", { value: e.textMap.get("name") })])
    .orElse([]),
])
```

---

## State Binding

UI components bind to per-client state values for interactivity:

- **`actor`** (built-in): The entity owned by the authenticated client. Always present.
- **Declared values**: Named slots holding `EntityExpression`, `ContainerExpression`, or absent.

Binding example:

```ts
const selection = hostApi.ui.state.declare("selection")
hostApi.ui.text("name", {
  value: selection.asEntity
    .map(e => e.textMap.get("name"))
    .orElse(hostApi.string.of("—"))
})
```

Per-client evaluation ensures each client maintains independent UI state.

---

## Expressions

All dynamic UI properties are expressions — not runtime booleans:

- **NumberExpression**: Sizes, positions, numeric values
- **StringExpression**: Text content, prefixes, suffixes
- **ConditionExpression**: Visibility, state narrowing (but we use exclusion instead)
- **EntityExpression / ContainerExpression**: Typed state values

This enables runtime-driven, fully composable UI updates.
