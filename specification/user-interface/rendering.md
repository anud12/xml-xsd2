# Rendering Model

The runtime evaluates the UI DAG each tick and sends a delta to the client. The client renders and manages interaction state. The runtime is platform-agnostic.

| Responsibility | Owner |
|----------------|-------|
| Expression evaluation, delta generation | Runtime |
| Rendering, resource loading, focus, hit-detection | Client |

---

## Coordinate System

All sizes and offsets are in **logical units**. The client translates to device pixels:

```
Device pixels = Logical units × Global UI scale [× Panel scale]
```

- **(0, 0)** = screen top-left; X increases right, Y increases down.
- **Global scale**: set at startup; applies uniformly. Adjustable at runtime (e.g., zoom).
- **Panel scale**: optional per-panel multiplier compounded on top of global scale.
- **Viewport**: fixed or resizable; panel positions (0–1 normalised) recompute on resize.

---

## Resources

Textures and fonts are **named references** resolved by the client at render time. Missing resources fall back to platform default; a warning is logged. Names are `StringExpression` — re-evaluated each tick, allowing dynamic switching.

### Texture Stretching

| Mode | Behavior |
|------|----------|
| `fill` | Stretch to fit bounds. Ignores aspect ratio. |
| `fit` | Scale to fit, preserve aspect ratio; pad remainder. |
| `tile` | Repeat across bounds. |
| `center` | Center without scaling; clip if overflows. |

Default: `fill`.

---

## Z-Ordering

Initial order: **declaration order** — first declared = back, last = front.

When a user clicks a panel, the client raises it to the front. Focus is client-side only; the runtime never observes clicks or focus state.

---

## Overflow & Hit Detection

| Component | Overflow | Hit Detection |
|-----------|----------|---------------|
| **Panel** | Clips all content to panel bounds | Visible bounds only |
| **Box** | Clips all children to grid bounds | Visible bounds only |
| **TextValue** | Truncates text silently; no ellipsis; vertically centered | Visible bounds only |

Clicks on clipped or truncated content do **not** register. When panels overlap, the highest z-index captures the click.

---

## Evaluation Tick

Each tick:

1. All expressions in the DAG are re-evaluated.
2. New tree is compared to previous using component `id`.
3. Changed components generate a delta sent to the client.

Identity rules by `id`:
- **Same** → update if changed
- **New** → full state sent
- **Missing** → removed from client

---

## Summary

| Aspect | Owner | Behavior |
|--------|-------|----------|
| Evaluation | Runtime | Each tick; delta diffed by `id` |
| Rendering | Client | Render delta; apply coordinate system |
| Resources | Client | Name → asset; fallback to platform default |
| Focus | Client | Clicked panel raises to front; client-side only |
| Hit detection | Client | Visible bounds + z-order priority |
| Overflow | Client | Panel/Box clip; TextValue truncates |
| Coordinates | Both | Logical units × global scale [× panel scale] |
