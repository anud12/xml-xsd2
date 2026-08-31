# Plan: Div-Composed, Real-Time JS UI Layer (`.ui`)

## Context

**Goal.** Make the UI a *real-time strategy interface* that is:
1. **Composed from a single container primitive** — like HTML's `<div>`: you build *anything* (HUDs, inventories, modals, minimap frames) by nesting one generic box, the way a web page is built from divs. No zoo of specialized components.
2. **Defines the game view** — the same div composition that builds the HUD also composes the **world view** (the RTS map: terrain, units, camera). The world is a `canvas`-like leaf node that divs position/size/overlay, exactly as a web page embeds a `<canvas>`.
3. **Modeled in JavaScript** — the UI is a JS expression DAG evaluated per tick, not a JSON snapshot.
4. **Runtime-agnostic** — one canonical JS UI layer; Godot/C# is just one renderer backend.
5. **Homed under `.ui`** — a dedicated runtime-agnostic directory.

**Non-goal (explicitly dropped):** an addon/hook extension model. Modules declare UI; no module can intercept, restyle, or replace another module's elements. There is no layering, priority, or hook chain. Cross-module interaction is by composition (sharing state values / containers), not interception.

**Current state (scouted).**
- UI is a **load-once JSON snapshot**: C# (Jint) runs each module, captures `registerPanel(PanelOptions)` JSON → flat `Dictionary<string, Panel>`; **later modules silently overwrite same-id panels**. No layering, no hooks.
- Rendering is **Godot-hardwired** (`Sources/UI/Panel.cs` extends `Godot.Panel`); live values are **per-frame FFI polling**. No deltas, no reconciliation.
- **Two divergent host APIs** (C#/Jint `HostApiSetup.cs` vs Rust/QuickJS `js_host_api/*.rs` where `registerPanel` is a no-op).
- **There is NO game-world rendering today.** No `Node2D`, `Sprite2D`, `Camera2D`, or spatial canvas anywhere. "The map" is only a sprite-map *texture* on a panel background (`SpriteMapCpu.cs`); entity positions (`getX`/`getY` per container) are data that nothing renders spatially. The world view is therefore a **net-new capability**, and it fits the div model as a fourth leaf (a `canvas` node the world renderer draws into).
- **The "div" model is already sketched, unimplemented:** `specification/types/todo/user-interface/` contains exactly the single-container idea:
  - `DivisionNode` — a **positionless flex layout container** (the "div"): `direction: Row|Column`, `alignment: start|center|end|space-between`, `gap`, `clip: clip|wrap|scroll`, `flex`, `maxWidth/maxHeight`, nested `divisions[]`. This is CSS-flexbox-in-a-box.
  - `PanelNode` — a **coordinate + clipping context** (a positioned window): `position`, `size`, `clip`, `scaleFactor`, 9 edge/corner `anchor`s, `divisions[]` children.
  - `TextNode` — a **leaf**: single-line `value`, `maxWidthPx`, `ellipsis`. (A `CanvasNode`/`ImageNode` leaf for the world is added by this plan.)
- A competing, richer 3-primitive model also exists in `specification/user-interface/` (`ui.panel`/`ui.box`/`ui.text` factories + grid layout + per-client `ui.state`). It's closer to a component library; the `todo/` `Division` model is closer to "everything is a div."

**Decision.** Adopt the **`todo/user-interface` Division model as the core composition primitive** — it *is* the "build everything from divs" idea — and build the `.ui` layer around it. Keep the `user-interface/` spec's **per-client `ui.state`** (actor + declared values) and **per-tick id-reconciliation/delta** model, since those are what make it real-time. **Layout is a single grid manager** (flex is shorthand over it), with **coordinate-escape** (`x`/`y` + `anchor`/`align`) for out-of-flow placement. **UI is evaluated and rendered only by the C# client (Jint)**; the Rust runtime's `hostApi.ui.*` are no-ops (matching today's split). The world is a **single map = one room graph** (rooms + portals), a new declaration type alongside entities/containers, extensible at runtime. The earlier `plans/simplify-ui-panel-api.md` is superseded.

---

## The UI model: everything is a division

The whole tree is two node kinds plus a small set of leaves — no more:

```
Window  (positioned, sized, clipped window — a top-level "screen region")
 └─ Division  (the "div": grid layout container, recursively nestable)
      └─ Division …
      └─ Text    (leaf: a string value — constant or expression)
      └─ Field   (leaf: a live binding to an entity map field)
      └─ Image   (leaf: a static texture)
      └─ Canvas  (leaf: the game WORLD — a hostable render surface, like <canvas>)
```

- **`ui.div(options, children)`** — the workhorse. Options: a `layout` (see below) + paint options (`background`, `border`, `padding`) + overflow (`clip`). Children = more divs / leaves. *This is the only layout node.* A HUD is a div; a stat row is a div; the label and value are divs containing text/field leaves. You never reach for a "special" component.

  **Paint options.**
  - **`background`** — uses the existing animation API (same as today's panel backgrounds, `PanelParser.cs` / `AnimationSequence`): either a static `SpriteResource` (a PNG path string, or a `SpriteMap` from `ui.spriteMapTIFF(...)`) or an animation reference `{ name, duration, loop }` resolved against a previously registered animation:
    ```js
    // register once (existing API):
    hostApi.runtime.registerAnimation("texture", { frames: [{ sprite: ui.getSpritePNG("frame_1.png") }, { sprite: ui.getSpritePNG("frame_2.png") }] })
    // use as a background:
    div({ background: { name: "texture", duration: n(5), loop: true } }, [...])
    div({ background: "panel-bg.png" }, [...])              // static
    ```
    A single static sprite may also be written as a 1-frame animation; the parser treats `background` uniformly (string → static; object with `frames`/`name` → animated, per the current `ExtractTexture`/`AnimationSequence` path). `hover` keeps its current shape (`{ texture, thickness }`) for v1.
  - **`border`** / **`padding`** — border texture + inner inset (logical units).

  **Layout: a single grid manager.** All child arrangement is one **grid** with `columns`/`rows` tracks (`min`/`max`/`scale`, per-track `align`), `gap`, and placement order. Flex is *shorthand* over the grid, not a separate manager:
  - `layout: "column"` → one column, children stack vertically (flex-column)
  - `layout: "row"` → N equal columns, children flow left→right and wrap (flex-row)
  - `layout: 4` → 4 equal columns (inventory grid)
  - `layout: { columns: [{min:80},{scale:1}], gap: 4 }` → label|value grid
  Under the hood it's always the same grid algorithm; the shorthand desugars to track definitions.
  - **`layout` is optional** — a bare `div({...}, children)` defaults to `"column"`.

  **Coordinate-escape (out-of-flow).** A child that specifies `x`/`y` is **not** positioned by the layout manager — it places itself relative to its parent's content box, and the manager skips it (doesn't count it, doesn't size the parent for it). No `position` flag needed; the *presence* of `x`/`y` is the signal:
  ```js
  div("overlay", { x, y, anchor: "center", align: "center" }, [...])
  ```
  - **`anchor`** — the reference point on the *parent* (9-point: `top-left`…`bottom-right`, default `top-left`). Named after WinForms `AnchorStyles`.
  - **`align`** — the point on the *child* that aligns to the parent's anchor (same 9-point set, default `top-left`). Named after GTK `halign/valign` / WPF `Alignment`.
  - **`x`/`y`** — displacement applied *after* alignment (e.g. inset from a corner).
  - Out-of-flow children don't consume parent size (may overflow, clipped by `clip`); z-order = declaration order. A child with `x`/`y` ignores its flow hints (`colSpan` etc.).
  - This replaces the earlier `free`/`stack` managers: a "stack" is several out-of-flow children at the same origin; "free placement" is out-of-flow children.

- **`ui.window(id, options, children)`** — a positioned window that hosts a division tree. Windows are top-level (never nested in a division). Options: `position`, `size`, `clip`, `scale`, `anchor`, `background`, `border` — `background`/`border` use the same animation-API shape as on a div (static `SpriteResource` or `{ name, duration, loop }` animation reference).
- **`ui.text(value)`** — a leaf rendering a string value (constant or `StringExpression`).
- **`ui.field(entity, map, name, { fallback })`** — a leaf that **binds to an entity map field** and renders it live. `entity` = an `EntityExpression`, entity id, or state reference (`state.actor`, `state.value("target")`); `map` = `"text" | "number"`; `name` = the field key; `fallback` = shown when the entity is absent or the field is missing (default `""`). No formatting — the raw value is rendered (numbers as-is). This is the declarative data-binding leaf: the runtime tracks it per (entity, map, name) so only affected leaves re-render when a field changes. `text(expr)` remains for complex/cross-entity expressions.
- **`ui.image(name)`** — a leaf rendering a static texture.
- **`ui.canvas(id, options, world)`** — the **game world view**, a leaf that divs host the way a page hosts a `<canvas>`. It is the *only* node the runtime's world renderer draws into; everything else (terrain, units, selection boxes, fog) is rendered by the world renderer *inside* this surface. Divs around it position/size/frame it and overlay HUD on top.

### The game view (`ui.canvas`)
This is what makes the model a *strategy* interface, not just HUD chrome. The world is a **single map** — one connected room graph — that modules and effects can *extend at runtime* by declaring new rooms and portals (see Data Model below). A `canvas` node carries:
- **`world` descriptor** (expressions, per-tick): `{ map, room }` — which (single) map to render and which **room** is the active one. The canvas draws that room as a textured polygon (local points + origin + rotation) + the units placed in it (units are entities; per-room membership + local position come from the room's container and the existing `getX`/`getY` maps, Rust `script_rest.rs` / `ContainerInterop`, mapped through the room transform). It also draws **portal edge markers** on the room's edges. Today the position data is computed but never rendered spatially — the canvas is the first consumer.
- **`camera`** (per-client, interactive): `{ room, x, y, zoom }` + follow behavior. Per-client state (like `ui.state`), so each player has their own view. When a unit (or the camera) crosses a portal's edge range, the active room transitions to the portal's target — the camera follows. A minimap is a **room graph** (nodes = rooms, edges = portals); clicking a room node re-centers the main canvas onto that room.
- **Renderer side:** the canvas maps to a Godot `SubViewport`/`Node2D` + `Camera2D` mounted as a child of the hosting division's `Control` — so the div layout system sizes/positions/clips it exactly like any other node. This is the one node kind that breaks the "Control-only" hierarchy; it introduces the spatial layer that doesn't exist today.

A full RTS screen is then pure composition: a full-rect `window` → a `div` (`layout: "column"`) → [ top resource-bar `div` (`layout: 3`), a `div` (`layout: { columns: [{scale:1},{min:240}] }`) holding the world `canvas` div + sidebar `div` ]. The minimap (room graph) is a small `div` in the sidebar.

### Data Model: the Map (single map, room graph, runtime-extensible)
The runtime's data model today is **entities** (textMap + numberMap) and **containers** (member entities + per-entity position/span). A strategy world needs a spatial layer on top. We add it as a **third declaration type**, modeled as a **single map = one room graph**:

```
Map  (single, the world)
 └─ Room   { id, terrain, origin {x, y}, rotation, points: [ {x, y} ×n ] }
 │            points are LOCAL to the room — (0,0) is the room's origin (center)
 │     └─ units: container of entities in this room (positions local to the room)
 └─ Portal { id,
             from: { room, edge, range {t0, t1} },   // edge = index into room.points; t = 0..1 along the edge
             to:   { room, edge, range {t0, t1} } }
```

- **A room is points only.** No `size`, no edges array, no separate vertex concept. `points` is an ordered, **convex** list in **room-local coordinates**: `(0,0)` is the room's `origin`, which is the **center** of the room. The room's placement in the world is its transform: `origin` (world position of the center) + `rotation` (angle, radians, about the origin). A local point maps to the world as `origin + R(rotation) · point`.
- **Edges are implicit.** Edge *i* is the segment `points[i] → points[(i+1) % n]`. A portal references an edge by that index, and `range {t0, t1}` is the 0..1 span along the edge where crossing triggers the transition. (Convexity is validated at declaration: all consecutive edge cross-products share a sign.)
- **Units are positioned in room-local coordinates** — the existing per-container `getX`/`getY` maps (Rust `script_rest.rs` / `ContainerInterop`) become local-to-room; the renderer applies the room transform (origin + rotation) to place them in world space. A unit's facing, if any, is local and rotates with the room.
- **Single map.** There is one world. "Extending the map" = adding rooms/portals to *that* graph, not loading a new map. (A future multi-map mode, if ever wanted, is a superset: a set of maps each being a room graph — the per-map structure is already isolated.)
- **Rooms and portals are declarations** — first-class, exactly like entities/containers. That is the load-bearing decision: because they flow through the **same declaration + per-tick evaluation + id-reconciliation pipeline**, they are automatically runtime-mutable and delta-synced.
  ```js
  hostApi.runtime.setRoom("cave-1", {
    terrain: "cave",
    origin: { x: 320, y: 180 },                       // world-space center
    rotation: Math.PI / 4,                             // 45° about the origin
    points: [ { x: -48, y: -32 }, { x: 48, y: -32 },
              { x: 48, y: 32 }, { x: -48, y: 32 } ],  // local, centered on origin
  })
  hostApi.runtime.setPortal("gate-1", {
    from: { room: "cave-1", edge: 1, range: { t0: 0.25, t1: 0.75 } },  // right edge
    to:   { room: "cave-2", edge: 3, range: { t0: 0.3,  t1: 0.7 } },
  })
  ```
- **Phased depth** (only the first is needed to shape the UI; the rest are staged):
  - **A — Data model (now, cheap, load-bearing):** `Room`/`Portal` declaration types in Rust state + `setRoom`/`setPortal` host API + manifest/declaration pipeline. Forces the canvas/minimap/delta model to be room-graph-aware from the start.
  - **B — Spatial semantics (staged):** portal **crossing** as a per-tick effect (a unit whose local position exits through a portal's edge `range` is moved to the target room at the mapped local range, in the target room's local coordinates); then **room adjacency** (portal-connected rooms are nav-adjacent); then pathfinding. Crossing alone already demonstrates a live-extending map.
  - **C — Build-engine mechanic (later, deferred):** a "build engine" unit that, on an action, calls `setRoom`/`setPortal`. Pure composition of A — no new machinery.
- **Spatial queries (the thing BSP is good at)** use a lightweight, runtime-mutable index (AABBs per room / uniform grid) for culling and coarse containment — **not** BSP. BSP is deferred to an *optional per-room visibility structure* only if fog-of-war/line-of-sight becomes a requirement.

This is the answer to the earlier "entities + containers only?" question: keep entities/containers as the unit/state layer, add **room/portal** as the spatial layer (rooms = convex point lists, local coords, origin = center, per-room rotation), and the single-map graph is what the canvas renders and the minimap summarizes.

Why this beats the current 5-content-type union: there's **one** layout concept (grid, with flex shorthand) and **one** way to compose. An "inventory grid" is `div({ layout: 4 }, ...)` — exactly how a web inventory is built. A "label|value" row is `div({ layout: { columns: [{min:80},{scale:1}] } }, ...)`. No `entityTextValue`/`containerListView` component types — values are `field` leaves or `text` expressions, not renderer primitives.

### Real-time binding (from the `user-interface` spec)
- Every node has a mandatory **`id`** (stable, declared at load) → per-tick **reconciliation**: runtime re-evaluates the DAG each iteration, diffs by id, ships an **id-keyed delta** (update/add/remove) to the renderer.
- **Values are expressions / bindings**: `ui.text(expr)` re-evaluates each tick; `ui.field(...)` tracks its (entity, map, name) so only affected leaves re-render. This *replaces* the current per-frame FFI polling.
- **Per-client `ui.state`**: `state.actor` (the client's entity), `state.declare(name)` / `state.value(name)`, narrowing (`asEntity`, `asContainer`, `isPresent`), and `ui.action.register(...)` for per-client state mutators. Interactivity (hover, click, selection) lives here, evaluated per-client.
- **Conditional rendering by exclusion**: a child is rendered iff present in its parent's children array; absence = not rendered.

### No addon / interception model
Modules declare UI into a shared registry; there is **no hook, no layer, no priority, no namespacing-for-interception**. A module cannot modify another module's elements. The "real-time" quality comes purely from per-tick evaluation + id-reconciliation (values and structure update live as state changes), not from cross-module mutation. If two modules declare the same node id, the last-loaded wins (the current behavior) — collisions are the module author's responsibility.

---

## Architecture

**Key constraint: UI is processed only by the C# client.** The Rust runtime's `hostApi.ui.*` functions are **no-ops** (as today: `script_panel_entity.rs` `registerPanel` no-op, `state_updates.rs` `append_panels_to_cache` no-op). Modules call the same `hostApi.ui.*` surface in both engines, but only the C# client (Jint) actually evaluates the UI DAG and renders it. Rust runs the simulation and exposes entity/container/room state via FFI; the C# client reads that state, evaluates the UI DAG per tick, and drives Godot.

```
            ┌──────────────────────────────────────────────────────┐
            │  .ui/  (UI layer JS, evaluated by the C# client)      │
            │  host.js  → builds hostApi.ui.* over a transport      │
            │  registry.js → node registry, id management           │
            │  dag.js   → Window/Division/Text/Field/Image/Canvas   │
            │             + per-tick evaluation + id diff → delta   │
            │  world.js → canvas/world descriptor + camera state    │
            │  state.js → per-client actor/declare/value/actions    │
            └───────────────────────┬──────────────────────────────┘
                                    │ per-tick: evaluate DAG, diff by id
                                    │ (reads entity/container/room state via FFI)
            ┌───────────────────────▼──────────────────────────────┐
            │  C# client (Jint) — UI engine of record               │
            │  runs modules, evaluates UI DAG, owns ui.state        │
            │  → UI delta (id-keyed add/update/del)                 │
            └───────────────────────┬──────────────────────────────┘
                                    │ delta
            ┌───────────────────────▼─────────────┐
            │  Godot renderer (C#)                 │
            │  reconcile nodes by id → live scene  │
            │  (incl. Canvas: SubViewport/Node2D/  │
            │   Camera2D for the world view)       │
            └──────────────────────────────────────┘

   Rust runtime (headless sim): entities/containers/rooms/portals + effects.
   hostApi.ui.* = no-op there. Exposes state via FFI to the C# client.
```

**`.ui` layout (new, `application/ui/`):**
```
application/ui/
  ui/        host.js, registry.js, dag.js, state.js, world.js
  types/     ui.d.ts, division.d.ts, window.d.ts, text.d.ts, field.d.ts, canvas.d.ts, state.d.ts
             (canonical; supersedes suite/types/ui + specification/types/todo/user-interface)
  godot/     C# renderer backend: consumes UI deltas → Godot nodes
             (incl. the spatial Canvas node: SubViewport/Node2D/Camera2D)
  test/      engine-agnostic UI tests
```
`.ui/ui/` depends on a **transport** seam (`readEntity`, `readContainer`, `readRooms`, `readClientState`, `resolveResource`) implemented by the C# client over FFI. The same `.ui` JS is what the C# client runs in Jint; in the Rust engine the `hostApi.ui.*` entry points are no-ops that drop the DAG (modules still load and their `runtime.*` declarations are extracted there as today).

---

## Phased implementation

### Phase 0 — Transport + spine (UI engine = C# client, settled)
- **UI engine of record is the C# client (Jint).** Rust's `hostApi.ui.*` are no-ops (as today); the C# client evaluates the UI DAG and renders. No engine decision needed.
- Define the **transport interface** the `.ui` JS uses to read sim state (`readEntity`, `readContainer`, `readRooms`, `readClientState`, `resolveResource`) — implemented by the C# client over the existing FFI (`RuntimeInterop`/`ContainerInterop`).
- **Spine:** one `ui.div` containing one `ui.text` evaluates in Jint → delta → Godot node, visible on screen. *Proves the spine before building up.*

### Phase 1 — The division model in `.ui` (DAG + reconciliation)
- `dag.js`: node types `Window`, `Division`, `Text`, `Field`, `Image`; factory functions `ui.window/ui.div/ui.text/ui.field/ui.image`; mandatory ids; **grid layout algorithm** (columns/rows tracks with min/max/scale, gap, placement order; flex shorthand `"row"`/`"column"`/number) + **coordinate-escape** (`x`/`y` + `anchor`/`align` out-of-flow placement).
- **Per-tick evaluation + id diff → delta** (per `user-interface/rendering.md`). The C# client re-evaluates the DAG each tick (driven by the existing ~40 ms iteration cadence), reading fresh entity/container/room state via FFI.
- `state.js`: per-client `ui.state` (actor, declare/value, narrowing, actions) per `ui-state.md`.
- `field` leaf: declarative entity-map binding with per-(entity, map, name) invalidation.
- **Deliverable:** a real-time `ui.field` bound to `state.actor` that updates within a tick when an effect mutates the entity — no C# FFI value polling.

### Phase 2 — Godot renderer backend (delta apply, kill polling)
- Rework `Sources/UI/` into a **dumb delta renderer**: reconcile Godot nodes by id (create/update/destroy). Replace the 5 `*ContentNode` classes + `ContentParser` with **one division renderer** (a Godot `Control` that lays out children via the grid manager, with out-of-flow children positioned by `anchor`/`align`/`x`/`y`) — the Godot side also collapses to "a div is a div."
- Keep `Panel.Update()`/`IContentNode.UpdateContent()` as the in-place path until the division node supersedes it.
- **Deliverable:** `MainModule` + `Test/Stage_*` UI modules migrated to `ui.div` composition; identical visuals; C# no longer calls `get_entity_*_value` per frame.

### Phase 2b — World view (the `canvas` node) + room data model (Layer A)
- **Data model (Layer A):** add `Room`/`Portal` declaration types — Rust state (`src/state`), declaration application (`src/module/declarations`), `setRoom`/`setPortal` host API (both engines' host scripts), and manifest/declaration plumbing. A room = `{ id, terrain, origin, rotation, points[] }` (convex, local coords, origin = center); a portal = edge-index + 0..1 range on each side. Single map = one room graph.
- `world.js` (in `.ui`): the `ui.canvas` node type — `world: { map, room }` descriptor + per-client `camera: { room, x, y, zoom }` state. Camera is `ui.state`-scoped so each client has its own view; a minimap (room graph) writes the main camera state.
- **Renderer (Godot):** a `Canvas` node kind that mounts a `SubViewport`/`Node2D` + `Camera2D` as a child of the hosting division's `Control`, and draws the active room as a textured convex polygon (terrain via the existing `SpriteMapCpu` composition, mapped onto the room's local points, positioned by `origin` + `rotation`), places units from the room container's local `getX`/`getY` maps (surfaced by `ContainerInterop`) through the same transform, and draws portal markers on the room's edges. This introduces the **spatial layer** that doesn't exist today (no `Node2D`/`Camera2D` currently).
- **Deliverable:** a full-rect world canvas shows the active room's terrain + units (correct under rotation); portal markers render on edges; drag pans, wheel zooms; a minimap (room graph) node click re-centers the main camera onto that room.
- **Phase 2c (staged, Layer B):** portal **crossing** as a per-tick effect (a unit whose local position exits through a portal's edge range is moved to the target room at the mapped local range); then room adjacency. Pathfinding deferred.

### Phase 3 — Packaging, lifecycle, cleanup
- `.ui`-aware packaging (module discovery, manifest `permissions`, enable/disable).
- Module **unload / hot-reload** (spec `runtime.md` "hot-swap under controlled quiescence"): deregister a module's nodes, reconcile out.
- **Retire:** duplicated Jint UI host (`HostApiSetup.cs` panel path), vestigial Rust `PanelFfi`/`LAST_PANELS`, `suite/types/ui/Panel.d.ts`, and the two `todo/` type drafts (promote them into `.ui/types`).
- Supersede `plans/simplify-ui-panel-api.md`.

---

## Scope decisions & trade-offs
- **One layout concept (grid, with flex shorthand), one composition rule (nest divs).** We deliberately do *not* keep the 5 content types from the current `Panel.d.ts` — values are `field`/`text` leaves, layout is the grid manager. Out-of-flow placement is by `x`/`y` + `anchor`/`align` (coordinate-escape), not by separate `free`/`stack` managers.
- **UI is processed only by the C# client; Rust's `hostApi.ui.*` are no-ops.** This matches the current split (Rust extracts `runtime.*` declarations; C#/Jint handles panels) and keeps the sim headless. The `.ui` JS is the single source the C# client runs; a future non-C# renderer would run the same `.ui` JS with its own transport.
- **`.ui` = `application/ui/`** top-level layer (sibling of `client`/`runtime`/`suite`).
- **Out of scope:** non-Godot renderers (pluggable in principle, but only the C#/Godot client built now); server-authoritative UI; resource bundling beyond named refs.

## Verification
1. **Spine (P0):** one div+text evaluates in the C# client (Jint) → delta → Godot node on screen.
2. **Real-time (P1):** a `field` leaf bound to an entity value updates within a tick when a Rust effect mutates it (observable via `MainModule`'s `repeat` effect incrementing `key`); the C# client picks up the change via FFI on its next evaluation tick.
3. **Renderer (P2):** migrate `MainModule` + `Test/Stage_*` UI modules to `ui.div`; confirm visuals, hover, z-order. (C# still reads values via FFI — but now *driven by the DAG evaluation*, not per-node `_Process` polling.)
4. **World view (P2b):** a full-rect `canvas` renders the active room as a textured polygon (correct under rotation) + units placed from the room container's local `getX`/`getY` data through the room transform; portal markers render on edges; drag pans, wheel zooms; a minimap (room graph) click re-centers the main camera. (P2c: a unit exiting through a portal's edge range emerges in the target room.)
5. **No regressions:** `application/suite` Maven build (incl. `tsc --noEmit`) + C# `dotnet build` pass; existing test modules migrated, not deleted.

## Open questions (answer before Phase 1)
1. **`.ui` = `application/ui/`** top-level, or a per-module `.ui/` folder convention? (UI engine is settled: C# client, Rust no-op.)
3. **`ui.image` leaf** — keep it, or is a div's `background` enough for static pictures initially? (`canvas` is required for the world view; `text`/`field` are settled.)
4. **`layout` shorthand coverage** — confirm `"row"`/`"column"`/number desugar correctly to the grid (especially `"row"` = equal columns + wrap), and that `layout` defaulting to `"column"` is right.
5. **World rendering depth (P2b):** confirm v1 scope — is a static sprite per unit placed from room-container data enough, or do we need unit animations/selection boxes/fog in the first cut? (Terrain via existing `SpriteMapCpu` is assumed available.)
6. **Portal crossing (P2c):** confirm crossing-movement (unit exits through the edge range → teleports to the mapped range in the target room) is the right v1 semantics, vs. a sliding/transition animation.
