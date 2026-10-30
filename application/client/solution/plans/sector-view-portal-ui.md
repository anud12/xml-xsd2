# `ui.sectorView` — continuous portal view

## Goal

A JS UI component that renders a **focus container** at full view size and
renders every other container reachable through portals **offset so the shared
openings line up** — the rooms read as one continuous space to the user. The
focus container fills the window; neighbours overflow the view rect and clip
(camera/viewport framing, not minimap framing).

## API

```js
hostApi.ui.sectorView("view-1", {
  grid: "cave",
  width: 700,
  height: 400,
  x: 200,
  y: 200,
  background: hostApi.ui.getAnimation(string.of("viewbg")),
  focus: "room-a",
}, (container) => hostApi.ui.window(container.id, {
  width: container.width,
  height: container.height,
  background: hostApi.ui.getAnimation(string.of("cell")),
}));
```

- `grid`: the sector grid id the focus + reachable neighbours live in (required).
- `focus`: the container id the camera is "in". It fills the whole view.
- `width`/`height`: the view window size in logical units.
- The render lambda is re-invoked once per reachable container each tick. It
  receives a descriptor `{ id, width, height }` and returns a single node id
  (the `containerView` for that container). `width`/`height` are the focus
  container's footprint extent in view units — the lambda uses them as the
  `containerView` size.
- The host stamps each materialized `containerView` with `x`, `y`, `viewWidth`,
  `viewHeight`. The existing per-tick container-view position pass then places
  entities inside each view.

## Placement (build-engine layout)

Positioning is driven entirely by the **sector build grid** (see
`runtime/src/state/sector.rs`), not by the portal walk. That module places each
sector at `at`, discovers neighbours by landing against already-taken squares,
and links two sectors with a portal only where they share facing openings of
at least one cell. The grid cells therefore already carry absolute build-engine
coordinates: `globalThis.__uiSectorGrids[grid].cells` = `{ x, y, container }`
in grid squares. `expandSectorViews` reuses those coordinates directly (it does
not re-derive layout from portals) so the view and the `sectorGrid` build render
stay in lockstep.

### Sizing (own spanX / spanY)

Each container is sized by its **own** footprint bounding box, in grid cells:

```
spanX(B) = maxX(B) − minX(B) + 1     // footprint squares of B, min/max over x
spanY(B) = maxY(B) − minY(B) + 1     // footprint squares of B, min/max over y
```

### View scale

One grid cell in view units is fixed by the focus container filling the window:

```
cellW = width  / spanX(focus)
cellH = height / spanY(focus)
```

### Arrangement (portal-arranged via grid delta)

For every container B reachable from `focus` over the portal graph:

```
viewX(B) = (at_B.x − at_focus.x) * cellW
viewY(B) = (at_B.y − at_focus.y) * cellH
viewW(B) = spanX(B) * cellW
viewH(B) = spanY(B) * cellH
```

where `at_X = { minX(X), minY(X) }` is the container's footprint origin on the
shared grid. The offset is the **grid-space delta of the two origins** — which
is exactly how the build engine placed the two sectors relative to each other
when it joined them with a portal. Because that delta lines up the shared portal
opening, the same opening lands on the same view coordinate from both rooms →
continuous. The portal BFS is used only to *select* the reachable set; it does
not compute any position.

## Walk scope

All containers transitively reachable from `focus` over the portal graph
(BFS, cycle-guarded, deduped). No depth limit.

## Offset source

Computed in the Rust engine (mirrors how `sectorGrid` is engine-driven). The
JS host only declares the view + render lambda; the engine bakes offsets into
the node stream.

## Files

1. `application/ui/ui/host.js`
   - `sectorView(name, args, render)` factory: stores `sectorViewRenders[name]`,
     registers a `window` node with options `{ grid, focus, width, height, x,
     y, background, ... }` and children `[$$sectorView:<name>]`.
   - `sectorViewMarker(name)` / `isSectorViewMarker(c)` helpers.
    - `expandSectorViews()`: for each `$$sectorView:` marker, read the grid state
      from `globalThis.__uiSectorGrids[grid]`. Group cells by container to get
      each footprint's origin + own spanX/spanY. Compute the view scale from the
      focus span. BFS the portal graph only to select the reachable container
      set, then place each by its build-grid origin delta
      (`(at_B − at_focus) * cell`) and size it by its own spanX/spanY — the same
      layout the build engine used to join the sectors. For each reachable
      container invoke `render({ id, width, height, text_map, number_map,
      getText, getNumber })` and stamp the returned node with `x`, `y`,
      `viewWidth`, `viewHeight`. Materialized ids are spliced in in place of the
      marker.
   - `resetContainers()`: drops the materialized `containerView` subtrees and
     restores the `$$sectorView:` marker.
   - `clear()` / `loadSnapshot()`: reset the new registries.
2. `application/runtime/src/ui/mod.rs`
   - The offset math lives in JS (host.js) for Option A framing, because the
     focus container's footprint span and the per-container origins are already
     available in `globalThis.__uiSectorGrids` (cells carry `x`, `y`,
     `container`). No Rust pass is required if the JS expansion does the BFS.
     (Revisit: keep the Rust pass only if the grid state is not fully present
     in `__uiSectorGrids`.)
3. `application/runtime/src/js_host_api/ui_host_api.rs` — expose `sectorView`
   on the live `host.ui` surface (mirror `sectorGrid`).
4. `application/runtime/src/js_executor/{sim_ctx,sim_entry,sim_invoke,
   extract_invoke}.rs` — wire `sectorView` into the sim host API + no-op
   fallbacks so modules that don't use it still boot.
5. `application/suite/types/ui/Ui.d.ts` — `UiSectorViewOptions`,
   `UiSectorViewDescriptor`, and `sectorView` on `UiApi`.

## Data available in JS at expansion time

`globalThis.__uiSectorGrids[gridId]` =
```
{ id, cells: [{ x, y, container, boundary_edges, openings }],
  portals: [{ id, a: { cell:[x,y], side, span, length },
              b: { cell:[x,y], side, span, length } }] }
```
- `cells` give per-container footprint squares (x, y are grid coords) →
  group by `container` to get each container's footprint set, its origin
  (min x, min y) and span (max−min+1).
- `portals` give the adjacency graph (container A ↔ container B via
  `a.cell`/`b.cell` ownership looked up in `cells`).

## Verification

- Rust: `cargo build` (runtime) — sim host boots with the new no-op `sectorView`;
  sector + ui suites stay green.
- TS: `npx tsc --noEmit` in `application/suite` (type-only change) — passes.
- C#: `dotnet build` of `New Game Project.csproj` — compiles 0 errors.
- Godot test (needs Godot runtime, not runnable headless here):
  `Tests/GameScene/Stage_10/Sector/SectorViewContinuous/` declares a two-room
  linked grid + a `sectorView` focused on `room-a` (400x400 window at 200,200).
  Asserts `room-a` fills the view (local 0,0 / 400x400) and `room-b` (grid
  (1,0)) overflows east at local (400,0) 400x400 — the continuous offset.

## Status: complete

All wiring landed: `host.js` (factory + `expandSectorViews` + reset),
`runtime/src/ui/mod.rs` (tick calls `expandSectorViews`), `script_ui.rs`,
`sim_ctx.rs`, `sim_entry.rs`, `extract_invoke.rs` (no-op), `Ui.d.ts` (types),
and the Godot test. (`sim_invoke.rs` untouched — its minimal `ui` stub never
exposed `sectorGrid`, so `sectorView` isn't needed there.)
