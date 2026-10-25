# GameScene Test Index

Stages represent progressive feature gates. Each stage builds on the previous one.
Tests within a stage verify a specific subsystem or interaction.

## Ffi (low-level interface tests)

Direct FFI boundary tests that verify the runtime-to-host contract without full scene rendering.

| Folder | Scope |
|--------|-------|
| `Ffi/Actor/ActiveAction` | Actor action lifecycle via FFI (emit, complete, query active) |
| `Ffi/Container/Entities` | Container entity list FFI (add, remove, enumerate) |
| `Ffi/Container/Id` | Container id serialization and lookup |
| `Ffi/Container/Ids` | Multi-container id registration |
| `Ffi/Container/PositionSpan` | Container position and span (x, y, spanX, spanY) FFI |
| `Ffi/Container/Size` | Container size (sizeX, sizeY) and out-of-bounds FFI |
| `Ffi/Container/TextNumberMap` | Container text/number map get/set via FFI |
| `Ffi/Entity/Mutation` | Entity number_map/text_map put/get/remove via FFI |
| `Ffi/Module/cross-module` | Cross-module entity and container sharing |
| `Ffi/Module/HotReload` | Module hot-reload (swap, reoccur, no-effect variants) |
| `Ffi/Panel/Anchor` | Panel anchor FFI |
| `Ffi/Panel/Content` | Panel content (entity-bound, constant) FFI |
| `Ffi/Panel/Hover` | Panel hover state FFI |
| `Ffi/Panel/Offset` | Panel offset FFI |
| `Ffi/Panel/Size` | Panel size FFI |
| `Ffi/Sector/Transport` | Sector transport (portal traversal) FFI |
| `Ffi/Ui` | UI host state snapshot via FetchUiState |

## Stage_0 — Static UI rendering

Baseline panel rendering with no interactivity or data binding.

| Folder | Scope |
|--------|-------|
| `Background` | Panel background color/texture rendering |
| `Border` | Panel border (default and custom) rendering |
| `ChildPanels` | Nested child panel layout |
| `ConditionReadOnlyExpression/ValueCarryThrough` | Condition expression value propagation through ifTrue/ifFalse |
| `ContentText` | Static text content rendering with font |
| `DivLayout` | Div layout (row/column) child arrangement |
| `EsModule` | ES module entrypoint loading and panel creation |
| `Existence` | Panel existence (show/hide) verification |
| `ExpressionInto` | Expression-to-value resolution in panel properties |
| `Font/ColorChange` | Font color rendering |
| `Font/SizeChange` | Font size rendering |
| `LintInvalid` | TypeScript lint rejection of invalid module code |
| `OnClick` | Panel click event registration (no action, just cursor) |
| `OnClickCursor` | Cursor change on hover/click |
| `PaintMode` | Panel paint mode (opaque/transparent) |
| `PanelSurface` | Panel surface (title bar, content area) layout |
| `Positioning` | Absolute panel positioning (x, y) |
| `ScreenRegion` | Screen region clipping |
| `Typescript` | TypeScript module compilation and execution |
| `ViewportSize` | Viewport dimension reporting |

## Stage_1 — Panel layout properties

Layout modifiers that affect panel geometry without user interaction.

| Folder | Scope |
|--------|-------|
| `Anchor` | Panel anchor point (top-left, center, etc.) positioning |
| `Offset` | Panel offset from anchor |
| `Size` | Panel explicit size (width, height) |

## Stage_2 — Interactivity and data-bound content

User interaction (click, hover) and entity-bound content rendering.

| Folder | Scope |
|--------|-------|
| `ActionMutatesEntity` | Action emission that mutates entity state |
| `Animation/GetAnimation` | Animation handle retrieval |
| `Animation/Loop` | Looping animation playback |
| `Children` | Dynamic child panel enumeration |
| `Content/ConstantTextContent` | Constant text with alignment (9 positions) |
| `Content/ContainerViewContent` | Container view as panel content |
| `Content/EntityNumberValueContent` | Entity number map value displayed as text |
| `Content/EntityTextValueContent` | Entity text map value displayed as text |
| `EmitActionOnClick` | Click triggers action emission |
| `Hover/Propagate` | Hover state propagation to children (with stop) |
| `Progress/Animation` | Progress bar with animation |
| `Progress/ConstantRange` | Static progress bar |
| `Progress/MinRange` | Progress bar minimum range clamping |
| `Progress/RecurringDeplete` | Recurring depleting progress |
| `Progress/ValueChange` | Progress value change animation |
| `SpriteMap/*` | Sprite map rendering (single/multi-layer, rotated, transparency, validation) |

## Stage_3 — Dynamic panel manipulation

Runtime panel tree modification (add/remove/resize/drag).

| Folder | Scope |
|--------|-------|
| `AddChild` | Runtime child panel addition |
| `EmitActionAndTriggerOnClick` | Click emits action AND triggers panel response |
| `Resizable` | Panel resize interaction |
| `ResizableAspect` | Aspect-ratio-locked resize |
| `TitleDrag` | Panel drag via title bar |

## Stage_4 — Effect-driven entity updates

Scheduled/concurrent effects that update entity values visible in the UI.

| Folder | Scope |
|--------|-------|
| `ConcurrentEffects` | Multiple effects executing concurrently in one iteration |
| `EntityNumberValueUpdate` | Effect updates entity number map; UI reflects new value |
| `EntityTextValueUpdate` | Effect updates entity text map; UI reflects new value |

## Stage_5 — Time-based and recurring updates

Elapsed-time units and recurring (periodic) entity updates.

| Folder | Scope |
|--------|-------|
| `ElapsedTimeUnits` | Elapsed time unit reporting (ms, frames) |
| `EntityTextValueUpdate/reoccurAfterMsTo*` | Recurring text updates: to dynamic value, to none, to static value |

## Stage_6 — Event propagation

Child-to-parent and parent-to-child event emission.

| Folder | Scope |
|--------|-------|
| `EmitChildEvents` | Emitting events from child panels, received by parent |

## Stage_7 — Container views and entity lists

Container-to-panel binding: entity lists, grid views, alignment, and container mutation.

| Folder | Scope |
|--------|-------|
| `ContainerListView` | entityList panel renders one child panel per container entity |
| `ContainerViewAlignment` | Container grid alignment (top-left vs center) on sector grid |
| `DynamicEntityList` | entityList panel dynamically reflects entities added via effects |
| `MutableAddEntity` | getMutableContainersBy + addEntity adds entity to container |

## Stage_8 — Behavior and action planning

Structured action sequences: plans, waits, conditional action dispatch.

| Folder | Scope |
|--------|-------|
| `ActionPlan/BusyReject` | Action plan rejection when actor is busy |
| `Behavior/ActionExists` | Behavior checks if an action is registered |
| `Behavior/CallAction` | Behavior calls a named action |
| `Behavior/Wait` | Behavior wait (delay) step |

## Stage_9 — Movement and teleportation

Container/panel movement across the screen and between sectors.

| Folder | Scope |
|--------|-------|
| `ContainerTeleport` | Instant container position change |
| `ContainerViewClickMoveTo` | Click on container view triggers move |
| `ContainerViewResizableClick` | Click on resizable container view |
| `Interrupt` | Movement interrupt (cancel in-progress move) |
| `MoveTo` | Animated container move to target position |
| `PanelClickMoveTo` | Panel click triggers entity move |
| `PanelClickTeleport` | Panel click triggers entity teleport |
| `RenderMovement` | Visual rendering of in-progress movement |

## Stage_10 — Sector grid system

Multi-sector world: grid layout, portals, links, and sector views.

| Folder | Scope |
|--------|-------|
| `AddContainer` | Add container to a sector |
| `GridCreate` | Sector grid creation |
| `MultiFootprint` | Containers spanning multiple grid cells |
| `PortalLinks` | Portal link validation (basic, mismatch, two-cycle, walled) |
| `SectorGrid/*` | Grid geometry: box corner, cell gap, cell size, diagonal link, dynamic, grid create, link opening, one-way link, unlinked, visual |
| `SectorGrid*` (legacy) | Earlier single-file sector grid tests |
| `SectorView/*` | Sector viewport: openings, containers, L-shape, mixed size, portal overlap, symmetry |

## Stage_11 — Area system

Named areas within sectors: entity containment queries and visual outlines.

| Folder | Scope |
|--------|-------|
| `Area/FindEntitiesInside` | Query entities inside a named area |
| `Area/NoPresence` | Area with no entities (empty result) |
| `AreaVisual/BlendWithTexture` | Area outline blended with background texture |
| `AreaVisual/MultipleAreas` | Multiple overlapping area outlines |
| `AreaVisual/RenderedOutline` | Area boundary outline rendering |
| `AreaVisual/StampedOutline` | Stamped (repeated) area outline |

## Stage_12 — Screen

Full-screen UI elements and screen-level layout.

| Folder | Scope |
|--------|-------|
| `Screen` | Screen-level panel (fullscreen overlay) |

## Stage_13 — String generation

Runtime string generation utilities.

| Folder | Scope |
|--------|-------|
| `StringGenerator` | Procedural string generation (ids, names) |

## Stage_14 — Entity filter system

Structured entity filtering: predicates, composition, edge cases.

| Folder | Scope |
|--------|-------|
| `AndOrNot` | Boolean composition (AND, OR, NOT) of filter predicates |
| `ByIdMatch` | Exact and substring id matching (no first-entity fallback) |
| `EmptyArrays` | Empty filter arrays (no-op, no crash) |
| `FiltersLeaf` | Single-leaf filter predicate evaluation |
| `Malformed` | Malformed filter structures (graceful rejection) |
| `NoMatch` | Filters that match no entities (empty result, no fallback) |
