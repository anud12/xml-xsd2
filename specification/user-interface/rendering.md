# Rendering Model

This document describes how the UI system translates from specification to pixels on screen.

---

## Overview

The rendering model divides responsibility between **runtime** (logic) and **client** (display):

| Component | Role |
|-----------|------|
| **Runtime** | Evaluates UI DAG each tick. Generates delta (changed components). Sends delta to client. Does not track interaction state. |
| **Client** | Receives UI tree from runtime. Renders panels and components. Tracks focus state locally. Handles user input (clicks, focus). |

This separation ensures the runtime remains stateless and platform-agnostic, while the client adapts rendering to its target (canvas, retained-mode UI, web, etc.).

---

## Coordinate System

All UI sizes and offsets are expressed in **logical units**. The client translates logical units to device pixels via a **global UI scale factor**.

```
Device pixels = Logical units × Global UI scale
```

### Global UI Scale

- Set by the client at startup (e.g., 32 pixels = 1 logical unit).
- Can be adjusted at runtime (e.g., zoom in/out).
- Applies to all components uniformly.

### Per-Panel Scale Override

Panels can declare an optional `scale` multiplier that compounds the global scale:

```
Device pixels = Logical units × Global UI scale × Panel scale
```

Example: With global scale = 32, a panel with `scale: 1.25` renders at 40 device pixels per logical unit.

### Coordinate Origin

- **(0, 0)** = top-left of screen
- **X** increases rightward
- **Y** increases downward

---

## Viewport

The viewport is the visible screen area. It has:

- **Fixed or resizable bounds** (implementation-specific; e.g., 1920×1080 or dynamic)
- **Safe area** (logical bounds after applying global scale; e.g., `[0, 0, 60, 34]` in logical units with global scale = 32)

Panels position using normalized screen coordinates (0–1 range) that recompute when the viewport resizes.

---

## Resource Resolution

### Textures and Fonts

Components reference textures and fonts by **name**. The client maintains a **resource registry** that maps names to assets.

```ts
{
  "ui-bg-texture": TextureAsset { ... },
  "body-font": FontAsset { family: "Arial", size: 14, color: 0xFFFFFF, ... },
  ...
}
```

### Resolution Process

1. Component specifies resource name (e.g., `font: { name: "body-font" }`)
2. Client looks up name in registry
3. If found → use asset
4. If not found → use platform default font/texture; log warning

This allows modules to declare UI without knowing implementation details. Renderers provide their own font/texture libraries.

### Dynamic Resource Names

Resource names can be **expressions**:

```ts
font: { name: entity.fontStyle() } // Evaluates each tick
```

When the expression changes, the client re-resolves the resource. Smooth switching; no errors if the new resource doesn't exist (fallback applies).

---

## Texture Stretching

When a texture is applied to a component, its size may not match. Stretching mode controls how the texture fills the component bounds.

| Mode | Behavior |
|------|----------|
| **`fill`** | Stretch/squash texture to exactly fit component bounds. Ignores aspect ratio. |
| **`fit`** | Scale texture to fit within bounds, preserving aspect ratio. Add padding to fill space. |
| **`tile`** | Repeat texture across component bounds. |
| **`center`** | Place texture at component center without scaling. Clip if texture exceeds bounds. |

Default: `fill` (stretch to fit).

---

## Z-Ordering (Layering)

Panels render in order of **declaration**, then **focus state**:

### Initial Order

Panels declared first render at the back; panels declared last render at the front.

### Focus-Based Layering

When a user **clicks** a panel, the client raises it to the front of the rendering stack. This persists until another panel is clicked.

**Note**: The runtime doesn't track focus. Focus state is purely a **client-side rendering concern**. The runtime never observes clicks.

### Implementation Detail

Z-order is maintained as a **client-side stack**. When a new panel is added (at runtime), it starts at the back (lowest z-index). Clicking any panel moves it to the front.

---

## Hit Detection

Hit detection determines whether a click (or other input) affects a component.

### Visible Bounds

Hit detection is **scoped to visible content only**:

- **Panel/Box overflow**: Content is clipped to component bounds. Clicking clipped (off-screen) content does **not** register.
- **TextValue overflow**: Text that exceeds `width.max` is truncated silently. Clicking truncated (invisible) text does **not** register.

### Z-Order Priority

When clicks overlap multiple panels, the panel with the **highest z-index** (most recently focused, or declared last if no focus) captures the click.

### Interaction Scope

Click events are observed **client-side only** and result in focus-state changes. The runtime does not receive click notifications.

---

## Overflow Semantics

### Panel

**Behavior**: Clips all content to panel bounds.

- Content exceeding panel width or height is invisible.
- Hit detection respects clipping (invisible content is not clickable).
- No scrolling; no overflow indicators.

### Box

**Behavior**: Clips all children to grid bounds.

- Children exceeding the grid's width or height are invisible.
- Hit detection respects clipping.
- No scrolling; no overflow indicators.

### TextValue

**Behavior**: Truncates text silently (no ellipsis).

- If text exceeds `width.max`, the rightmost characters are cut off.
- If no `width.max` is set, text uses its intrinsic width.
- Hit detection respects truncation (invisible text is not clickable).
- Vertical text is centered within the component's `height`.

---

## Evaluation Tick

### Frequency

The runtime evaluates the UI DAG **each tick** (typically per frame, or per update cycle). The exact frequency is implementation-specific.

### Process

1. **Evaluate expressions**: All expression nodes (NumberExpression, StringExpression, ConditionExpression) in the DAG are re-evaluated.
2. **Reconcile tree**: The new tree is compared with the previous tree using component `id` for identity matching.
3. **Generate delta**: Only changed components are included in the output.
4. **Send to client**: Delta is transmitted to the client (WebSocket, IPC, etc.).

### Identity Stability

Components are identified by their `id` field. The runtime uses `id` to match components across ticks:

- Same `id` → component re-evaluated; updates sent if changed
- Missing `id` → component is new; full state sent
- Lost `id` → component was removed; removed from client

This enables efficient delta compression and client-side reconciliation.

---

## Rendering Output

The client receives an updated UI tree each tick and renders it immediately. The rendering process is **implementation-specific**:

- **Canvas renderer**: Draw shapes, text, and textures directly to a 2D canvas.
- **Retained-mode UI**: Update a scene graph or DOM tree.
- **Game engine**: Compose UI using engine primitives (meshes, materials, text rendering).

All implementations respect the same coordinate system, overflow semantics, and layering model described above.

---

## Summary

| Aspect | Responsibility | Behavior |
|--------|-----------------|----------|
| **Evaluation** | Runtime | Each tick; expression DAG re-evaluated; delta diffed by `id` |
| **Rendering** | Client | Each tick; render delta; apply coordinate system + scale factor |
| **Resource lookup** | Client | Resolve font/texture by name; fallback to platform default if missing |
| **Focus tracking** | Client | Track clicked panels; raise to front; client-side only |
| **Hit detection** | Client | Visible bounds only; respects clipping and z-order |
| **Overflow** | Client | Panel/Box clip silently; TextValue truncates silently |
| **Coordinate system** | Both | Logical units → device pixels via global scale + per-panel multiplier |
